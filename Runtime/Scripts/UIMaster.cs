using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class UIMaster : MonoBehaviour
{
    public static UIMaster Instance;

    /// <summary>The values edited in this panel, so Ctrl+Z can put the last one back.</summary>
    public UndoStack Undo { get; private set; }

    public bool AutoHideCursor
    {
        get => _autoHideCursor;
        set { _autoHideCursor = value; UpdateUI(); }
    }

    [Header("Global settings")]
    [SerializeField] private bool _autoHideCursor = true;

    public bool HideUIAtStart;
    public bool enableUIMovement = true;

    [Header("Shortcuts")]
    public Key toggleUIKey = Key.F1;
    public Key resetUIKey = Key.F2;

    public float UIScale
    {
        get => _uiScale;
        set { 
            _uiScale = value;
            _canvasScaler.scaleFactor = _uiScale;
        }
    }

    [Header("Debug")]
    public bool showDebug = false;

    // All prefabs come from one Resources asset; the panel and popups are resolved/instantiated in
    // Awake, so UIMaster carries no serialized wiring references.
    private UIPrefabs _prefabs;
    private Transform MainPanel;
    private RightClickMenu rightClickMenu;
    private ColorPicker colorPicker;

    private bool displayUI;
    private GameObject _rootCanvas;
    private Dictionary<string, GameObject> _panels;
    private CanvasScaler _canvasScaler;
    private RectTransform _scrollViewTransform;
    private ScrollRect _scrollRect;

    //Shortcut keys resolved from what they print on the active layout rather than bound to a physical
    //position; see KeyPrinting().
    private readonly Dictionary<string, KeyControl> _keysByCharacter = new Dictionary<string, KeyControl>();
    private Keyboard _resolvedKeyboard;
    private string _resolvedLayout;

    //See SuppressNavigationWhileCtrlHeld.
    private bool _navigationSuppressed;
    private bool _navigationWasEnabled;

    private float _uiScale = 1;
    private const float _uiScaleSpeed = 2;
    private const float _uiMovementSpeed = 500;

    #region MonoBehaviour

    // Use this for initialization
    void Awake()
    {
        //Enable canvas that is disabled by default in prefab to not be visible in scene view.
        transform.GetChild(0).gameObject.SetActive(true);

        Undo = new UndoStack();

        _rootCanvas = transform.GetChild(0).gameObject;
        _canvasScaler = _rootCanvas.GetComponent<CanvasScaler>();
        _scrollViewTransform = (RectTransform)_rootCanvas.transform.GetChild(0);

        ResolvePrefabsAndLinks();

        InitializeRightClickMenu();
        InitializeColorPicker();

        Instance = this;
        _panels = new Dictionary<string, GameObject>();

        ControllableMaster.controllableAdded += CreateUI;
        ControllableMaster.controllableRemoved += RemoveUI;

        displayUI = true;

        ResetUITransform();

        if (HideUIAtStart)
            ToggleUI();
    }

    void Start()
    {
        // Checked in Start rather than Awake: by now every Awake/OnEnable in the scene has run, so
        // an EventSystem another script created is seen and no false warning fires.
        EventSystemCheck.WarnIfMissing();
    }

    void Update()
    {
        SuppressNavigationWhileCtrlHeld();

        if (Keyboard.current != null && Keyboard.current[toggleUIKey].wasPressedThisFrame)
        {
            //Avoid toggling the UI if currently writing in an input field
            if (FocusedInputField() != null)
                return;

            ToggleUI();
        }

        if (displayUI && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            MoveFocus(backwards: Keyboard.current.shiftKey.isPressed);

        //Deliberately not guarded by FocusedInputField(), unlike every other shortcut here: the other
        //ones step out of the way while a value is being typed, whereas undo is precisely what the
        //user wants after typing one.
        if (displayUI && Keyboard.current != null && Keyboard.current.ctrlKey.isPressed)
        {
            var undoKey = UndoKey();
            if (undoKey != null && undoKey.wasPressedThisFrame)
                UndoLastEdit();
        }

        if(displayUI)
            UpdateUITransform();

    }

    void OnDestroy()
    {
        // Static event: must unsubscribe or destroyed instances keep receiving
        // callbacks when Domain Reload is disabled
        ControllableMaster.controllableAdded -= CreateUI;
        ControllableMaster.controllableRemoved -= RemoveUI;

        //The EventSystem outlives this panel, so navigation must not be left switched off on it.
        if (_navigationSuppressed && EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = _navigationWasEnabled;

        if (Instance == this)
            Instance = null;
    }

    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    #region Setup

    // Load the prefab set and resolve the panel + popup links without any serialized reference:
    // the panel is the scroll view's content, and the popups are instantiated from the prefab set.
    void ResolvePrefabsAndLinks()
    {
        _prefabs = Resources.Load<UIPrefabs>("GenUIPrefabs");
        if (_prefabs == null)
        {
            Debug.LogError("[GenUI] Could not load the GenUIPrefabs asset from Resources. The UI cannot be built.");
            return;
        }

        _scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (_scrollRect != null)
            MainPanel = _scrollRect.content;
        else
            Debug.LogError("[GenUI] No ScrollRect found under UIMaster; the panel container is missing.");

        var rightClickMenuObject = Instantiate(_prefabs.RightClickMenuPrefab, _rootCanvas.transform, false);
        rightClickMenuObject.transform.SetAsLastSibling();
        rightClickMenu = rightClickMenuObject.GetComponentInChildren<RightClickMenu>(true);

        var colorPickerObject = Instantiate(_prefabs.ColorPickerPrefab, _rootCanvas.transform, false);
        colorPickerObject.transform.SetAsLastSibling();
        colorPicker = colorPickerObject.GetComponentInChildren<ColorPicker>(true);
    }

    #endregion

    #region Visibility

    public void ToggleUI()
    {
        displayUI = !displayUI;

        UpdateUI();
    }

    public void ShowUI() {
        if (!displayUI)
            ToggleUI();
	}

    public void HideUI() {
        if (displayUI)
            ToggleUI();
    }

    public void UpdateUI()
    {
        if (AutoHideCursor && !Application.isEditor && !displayUI)
        {
            Cursor.visible = false;
        } else
        {
            Cursor.visible = true;
        }

        transform.GetChild(0).gameObject.SetActive(displayUI);
    }

    public bool IsUIVisible()
    {
        return displayUI;
    }

    #endregion

    #region Keyboard shortcuts

    //Puts the last value edited in the UI back to what it held before that edit. The widget restores
    //it through SetFieldProp, the same path an edit takes, so the target script and OSC follow.
    void UndoLastEdit()
    {
        UndoStack.Entry entry;
        if (!Undo.TryPop(out entry))
            return;

        entry.Widget.ApplyUndo(entry.Value);
    }

    /// <summary>
    /// Turns the EventSystem's keyboard navigation off for as long as Ctrl is held.
    /// </summary>
    /// <remarks>
    /// Every Ctrl shortcut here collides with the Navigate action on some layout: Ctrl+arrows is
    /// Navigate outright, and Ctrl+Z is the physical W key on AZERTY, which Navigate binds to "up".
    /// Either way the selection walks off the field being edited. Ctrl is never a navigation
    /// modifier, so the moves are stopped at the source rather than corrected after the fact.
    /// </remarks>
    void SuppressNavigationWhileCtrlHeld()
    {
        var suppress = displayUI && Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;

        if (suppress == _navigationSuppressed || EventSystem.current == null)
            return;

        //Restores what the host project had it set to, not an assumed true.
        if (suppress)
            _navigationWasEnabled = EventSystem.current.sendNavigationEvents;

        EventSystem.current.sendNavigationEvents = !suppress && _navigationWasEnabled;
        _navigationSuppressed = suppress;
    }

    KeyControl UndoKey()
    {
        return KeyPrinting("z", Key.Z);
    }

    /// <summary>
    /// The key that prints <paramref name="character"/> on the keyboard as it is currently laid out.
    /// </summary>
    /// <remarks>
    /// Key values are physical positions named after US QWERTY, so binding one directly picks the
    /// wrong key on any layout that moves the character: Z sits where QWERTY has W on AZERTY, and '-'
    /// sits on the 6. displayName reports what a key prints under the active layout, so shortcuts
    /// resolve the key they mean instead of listing the positions it might occupy. The lookup is
    /// cached until the keyboard or its layout changes.
    /// </remarks>
    /// <param name="fallback">Physical key to use on a layout that prints the character nowhere.</param>
    KeyControl KeyPrinting(string character, Key fallback)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return null;

        if (keyboard != _resolvedKeyboard || keyboard.keyboardLayout != _resolvedLayout)
        {
            _keysByCharacter.Clear();
            _resolvedKeyboard = keyboard;
            _resolvedLayout = keyboard.keyboardLayout;
        }

        KeyControl resolved;
        if (_keysByCharacter.TryGetValue(character, out resolved))
            return resolved;

        resolved = keyboard[fallback];

        foreach (var key in keyboard.allKeys)
        {
            if (string.Equals(key.displayName, character, StringComparison.OrdinalIgnoreCase))
            {
                resolved = key;
                break;
            }
        }

        _keysByCharacter[character] = resolved;
        return resolved;
    }

    static bool IsPressed(KeyControl key)
    {
        return key != null && key.isPressed;
    }

    /// <summary>The input field the user is currently typing in, or null.</summary>
    /// <remarks>
    /// The shortcuts use this to stay out of the way while a value is being typed; Tab uses it, the
    /// other way round, to work out where it currently is.
    /// </remarks>
    static InputField FocusedInputField()
    {
        if (EventSystem.current == null)
            return null;

        var selected = EventSystem.current.currentSelectedGameObject;
        return selected != null ? selected.GetComponent<InputField>() : null;
    }

    #endregion

    #region Tab navigation

    //Moves focus to the next editable field in the panel, wrapping at either end. Selecting the new
    //field is all that is needed to commit the old one: InputField.OnDeselect deactivates it, which
    //raises onEndEdit, and OnSelect activates the new one and selects its text.
    void MoveFocus(bool backwards)
    {
        if (MainPanel == null || EventSystem.current == null)
            return;

        //Rebuilt per keypress rather than cached: panels are created and destroyed at runtime, and
        //Tab is not a hot path.
        var fields = CollectInputFields();
        if (fields.Count == 0)
            return;

        var index = NextIndex(fields.IndexOf(FocusedInputField()), fields.Count, backwards);
        var next = fields[index];

        EventSystem.current.SetSelectedGameObject(next.gameObject);
        ScrollIntoView((RectTransform)next.transform);
    }

    //Hierarchy order matches visual order, since the panels are laid out top to bottom. Widgets
    //return their own fields so multi-field ones come out in x, y, z, w order.
    List<InputField> CollectInputFields()
    {
        var fields = new List<InputField>();

        //Inactive widgets are skipped, which is what keeps collapsed panels out of the sequence.
        foreach (var widget in MainPanel.GetComponentsInChildren<ControllableUI>())
        {
            foreach (var field in widget.GetInputFields())
            {
                //Read-only members render as non-interactable fields; Tab should pass over them.
                if (field != null && field.interactable && field.gameObject.activeInHierarchy)
                    fields.Add(field);
            }
        }

        return fields;
    }

    /// <summary>
    /// The field to focus next. <paramref name="current"/> is -1 when nothing is focused, which
    /// starts the sequence at either end depending on direction.
    /// </summary>
    public static int NextIndex(int current, int count, bool backwards)
    {
        if (count <= 0)
            return -1;

        if (current < 0)
            return backwards ? count - 1 : 0;

        return ((current + (backwards ? -1 : 1)) % count + count) % count;
    }

    //Without this, tabbing past the bottom of the view looks like Tab stopped working. Note this
    //moves the scroll view's content, not _scrollViewTransform, which is what Ctrl+arrows move.
    void ScrollIntoView(RectTransform field)
    {
        if (_scrollRect == null || _scrollRect.viewport == null || MainPanel == null)
            return;

        Canvas.ForceUpdateCanvases();

        var viewport = _scrollRect.viewport;
        var content = (RectTransform)MainPanel;

        var fieldTop = viewport.InverseTransformPoint(field.TransformPoint(new Vector2(0, field.rect.yMax))).y;
        var fieldBottom = viewport.InverseTransformPoint(field.TransformPoint(new Vector2(0, field.rect.yMin))).y;

        var viewTop = viewport.rect.yMax;
        var viewBottom = viewport.rect.yMin;

        var delta = 0f;
        if (fieldTop > viewTop)
            delta = fieldTop - viewTop;
        else if (fieldBottom < viewBottom)
            delta = fieldBottom - viewBottom;

        if (delta != 0f)
            content.anchoredPosition -= new Vector2(0, delta);
    }

    #endregion

    #region Panel building

    //Every numeric widget gets its label wired for drag-to-scrub here rather than in each widget's
    //CreateUI, so the seven of them share one call site.
    static void AttachValueDragging(Transform panel)
    {
        foreach (var widget in panel.GetComponentsInChildren<ControllableUI>(true))
        {
            foreach (var target in widget.GetScrubTargets())
                DragValueUI.Attach(widget, target);
        }
    }

    public void RemoveUI(Controllable dyingControllable)
    {
        if (!dyingControllable.controllableUsePanel) return;
        
        if (showDebug)
            Debug.Log("Removing UI for " + dyingControllable.controllableId);

        if (!_panels.ContainsKey(dyingControllable.controllableId))
            return;

        if (_panels[dyingControllable.controllableId] != null)
            _panels[dyingControllable.controllableId].GetComponentInChildren<PanelUI>().RemoveUI();

        Destroy(_panels[dyingControllable.controllableId]);
        _panels.Remove(dyingControllable.controllableId);
    }

    public void CreateUI(Controllable newControllable)
    {
        if(showDebug)
            Debug.Log("Adding " + newControllable.controllableId + ", use panel : " + newControllable.controllableUsePanel);

        if (!newControllable.controllableUsePanel) return;

        if (_panels.ContainsKey(newControllable.controllableId))
        {
            if (showDebug)
                Debug.LogWarning("[GenUI] A panel for '" + newControllable.controllableId + "' already exists; skipping.");
            return;
        }

        //First we create a panel for the controllable
        var newControllableHolder = Instantiate(_prefabs.PanelPrefab);
        newControllableHolder.transform.GetChild(0).GetComponent<Image>().color = newControllable.controllableBarColor;
        newControllableHolder.transform.SetParent(MainPanel.transform);

        var newPanel = newControllableHolder.transform.GetChild(1).gameObject;
        newPanel.GetComponentInChildren<Text>().text = newControllable.controllableId;
        newPanel.transform.GetChild(0).GetChild(0).GetComponentInChildren<Image>().color = newControllable.controllableBarColor;

        _panels.Add(newControllable.controllableId, newControllableHolder);

        //Read all properties and add associated UI
        foreach (var property in newControllable.controllableFields)
        {
            var propertyType = property.Value.FieldType;
            OCFProperty attribute = Attribute.GetCustomAttribute(property.Value, typeof(OCFProperty)) as OCFProperty;

            //Check if needs to be in UI
            if (!attribute.showInUI) continue;

            if (showDebug)
                Debug.Log("[UI] Adding control for (" + newControllable.GetType() + ") : " + property.Value.Name + " of type : " + propertyType.ToString());

			bool propertyDrawn = false;

            //Add header if it exists
            var headerAttribut = (HeaderAttribute[])property.Value.GetCustomAttributes(typeof(HeaderAttribute), false);
            if (headerAttribut.Length != 0)
            {
                CreateHeaderText(newPanel.transform, newControllable, headerAttribut[0].header);
            }

			//Create list
			if (!string.IsNullOrEmpty(attribute.targetList) && !propertyDrawn)
            {
                //The name is passed on rather than a resolved FieldInfo: the list may live on the
                //mirror or on the target script, and its entries are read live on every refresh.
                if (newControllable.GetTargetList(attribute.targetList) == null)
                    Debug.LogWarning("[GenUI] No widget created for '" + property.Value.Name + "' on "
                        + newControllable.controllableId + " : targetList '" + attribute.targetList
                        + "' names no List<string> on the controllable or its target script.");
                else
                    CreateDropDown(newPanel.transform, newControllable, property.Value, targetListName: attribute.targetList);

				propertyDrawn = true;
                //continue;
            }

            if (propertyType.IsEnum && !propertyDrawn)
            {
                //A [Flags] enum holds a combination of its members, which one dropdown cannot show.
                //Drawing a single-select control over it would silently discard every flag it leaves
                //out, so the member is left to OSC and presets instead.
                if (propertyType.IsDefined(typeof(FlagsAttribute), false))
                    Debug.LogWarning("[GenUI] No widget created for '" + property.Value.Name + "' on "
                        + newControllable.controllableId + " : " + propertyType.Name
                        + " is a [Flags] enum. It stays controllable over OSC.");
                else
                    CreateDropDown(newPanel.transform, newControllable, property.Value, enumType: propertyType);

                propertyDrawn = true;
                //continue;
            }
            //property.Value.Attributes O
            if ((propertyType.ToString() == "System.Single" || propertyType.ToString() == "System.Int32") && !propertyDrawn)
            {
                var rangeAttribut = (RangeAttribute[]) property.Value.GetCustomAttributes(typeof(RangeAttribute), false);

                bool isFloat = propertyType.ToString() != "System.Int32";

                if (rangeAttribut.Length == 0)
                    CreateInput(newPanel.transform, newControllable, property.Value, !attribute.readOnly);
                else
                    CreateSlider(newPanel.transform, newControllable, property.Value, rangeAttribut[0], !attribute.readOnly, isFloat);

				propertyDrawn = true;
				//continue;
			}
            if (propertyType.ToString() == "System.Boolean" && !propertyDrawn)
            {
                CreateCheckbox(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

				propertyDrawn = true;
				//continue;
			}
            if (propertyType.ToString() == "System.String" && !propertyDrawn)
            {
                CreateInput(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

				propertyDrawn = true;
				//continue;
			}

            if (propertyType.ToString() == "UnityEngine.Color" && !propertyDrawn)
            {
                CreateColor(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

				propertyDrawn = true;
				//continue;
			}

            if(propertyType.ToString() == "UnityEngine.Vector3" && !propertyDrawn)
            {
                CreateVector3(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

				propertyDrawn = true;
				//continue;
			}

            if (propertyType.ToString() == "UnityEngine.Vector4" && !propertyDrawn)
            {
                CreateVector4(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

                propertyDrawn = true;
                //continue;
            }

            if (propertyType.ToString() == "UnityEngine.Vector3Int" && !propertyDrawn) {
                CreateVector3Int(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

                propertyDrawn = true;
                //continue;
            }

            if (propertyType.ToString() == "UnityEngine.Vector2" && !propertyDrawn) {
                CreateVector2(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

                propertyDrawn = true;
                //continue;
            }

            if (propertyType.ToString() == "UnityEngine.Vector2Int" && !propertyDrawn) {
                CreateVector2Int(newPanel.transform, newControllable, property.Value, !attribute.readOnly);

                propertyDrawn = true;
                //continue;
            }

            if (!propertyDrawn)
                Debug.LogWarning("[GenUI] No widget created for '" + property.Value.Name + "' on " + newControllable.controllableId + " : unsupported type " + propertyType + ".");

            //Add tooltip if it exists
            var tooltipAttribut = (TooltipAttribute[])property.Value.GetCustomAttributes(typeof(TooltipAttribute), false);
			if (tooltipAttribut.Length != 0) {
				CreateTooltipText(newPanel.transform, newControllable, tooltipAttribut[0].tooltip);
			}
		}

        //Read all methods and add button
        foreach (var method in newControllable.controllableMethods)
        {
            if (showDebug)
                Debug.Log("[UI] Adding button for (" + newControllable.GetType() + ") : " + method.Value.methodInfo.Name);

            CreateButton(newPanel.transform, newControllable, method.Value);
        }

        AttachValueDragging(newPanel.transform);

        CleanGeneratedUI(newControllable.controllableId, newControllable);       
    }

    public void CleanGeneratedUI(string controllableId, Controllable controllable)
    {
        //Order Save and Load preset buttons. Buttons are identified by the name of the method they
        //invoke, not by their label: the label is derived from the method name by ParseNameString,
        //and a panel's title Text also lives in this subtree.
        var lastPanel = _panels[controllableId].transform.GetChild(1);
        var presetHolder = lastPanel.Find("PresetHolder");
        var isGlobalPresetPanel = controllable is ControllableMasterControllable;

        //Create the global holders up front, while PresetHolder is still empty. Cloning them
        //lazily once the first SaveAll button appears would copy the Save/Load buttons already
        //reparented into PresetHolder, stacking them into these top rows too.
        Transform globalPresetHolder = null;
        Transform globalActionHolder = null;
        if (isGlobalPresetPanel)
        {
            globalPresetHolder = Instantiate(presetHolder);
            globalPresetHolder.name = "AllPresetHolder";
            globalPresetHolder.SetParent(lastPanel);
            globalPresetHolder.SetSiblingIndex(1); //Set first

            //Own row, directly under the preset row: these buttons have long labels and do not fit
            //alongside Save All / Save As All.
            globalActionHolder = Instantiate(presetHolder);
            globalActionHolder.name = "GlobalActionHolder";
            globalActionHolder.SetParent(lastPanel);
            globalActionHolder.SetSiblingIndex(2);
        }

        var allButtons = lastPanel.GetComponentsInChildren<ButtonUI>();
        var usePreset = false;
        foreach (var button in allButtons)
        {
            if (button.Method == null) continue;

            if (Array.IndexOf(Controllable.PresetMethodNames, button.Method.Name) >= 0)
            {
                button.transform.SetParent(presetHolder);
                usePreset = true;
            }

            //Only this panel owns the global preset buttons; a target script may expose its own SaveAll.
            if (isGlobalPresetPanel &&
                Array.IndexOf(ControllableMasterControllable.AllPresetMethodNames, button.Method.Name) >= 0)
            {
                button.transform.SetParent(globalPresetHolder);
            }

            if (isGlobalPresetPanel &&
                Array.IndexOf(ControllableMasterControllable.GlobalActionMethodNames, button.Method.Name) >= 0)
            {
                button.transform.SetParent(globalActionHolder);
            }
        }

        //Nothing landed in it, so it would otherwise render as an empty strip.
        if (globalActionHolder != null && globalActionHolder.childCount == 0)
            globalActionHolder.gameObject.SetActive(false);

        if (usePreset)
        {
            presetHolder.SetSiblingIndex(lastPanel.transform.childCount - 2); //last index being the preset list
        }
        else
            presetHolder.gameObject.SetActive(false);

        lastPanel.GetComponent<PanelUI>().Init(controllable);

        //Close panel if needed
        if (controllable.controllableClosePanelAtStart)
            _panels[controllableId].GetComponentInChildren<PanelUI>().Close();
        else
            _panels[controllableId].GetComponentInChildren<PanelUI>().Open();

        //Order panels by alphabetical order
        var panelIds = _panels.Keys.ToArray();
        Array.Sort(panelIds);

        for(int i = 0; i < panelIds.Length; i++)
        {
            _panels[panelIds[i]].transform.SetAsLastSibling();
        }

        //Set GenUI on top
        if (_panels.ContainsKey("GenUI"))
        {
            _panels["GenUI"].transform.SetAsFirstSibling();
        }
    }

    private void CreateHeaderText(Transform parent, Controllable target, string text)
    {
        var headerText = Instantiate(_prefabs.HeaderTextPrefab);
        headerText.transform.SetParent(parent);
        headerText.GetComponent<HeaderUI>().CreateUI(target, text);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(headerText.GetComponent<HeaderUI>());
    }

	private void CreateTooltipText(Transform parent, Controllable target, string text) {
		var tooltipText = Instantiate(_prefabs.TooltipTextPrefab);
		tooltipText.transform.SetParent(parent);
		tooltipText.GetComponent<TooltipUI>().CreateUI(target, text);
		parent.gameObject.GetComponent<PanelUI>().AddUIElement(tooltipText.GetComponent<TooltipUI>());
	}

    //One prefab, two sources for its entries: the entries of a named List<string>, or the members of
    //the field's own enum type. Exactly one of the two is set by the caller.
    private void CreateDropDown(Transform parent, Controllable target, FieldInfo activeElement, string targetListName = null, Type enumType = null)
    {
        var newDropdown = Instantiate(_prefabs.DropdownPrefab);
        newDropdown.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newDropdown.GetComponent<DropdownUI>());
        if (enumType != null)
            newDropdown.GetComponent<DropdownUI>().CreateUI(target, activeElement, enumType);
        else
            newDropdown.GetComponent<DropdownUI>().CreateUI(target, targetListName, activeElement);
    }

    private void CreateSlider(Transform parent, Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible, bool isFloat = true)
    {
        var newSlider = Instantiate(_prefabs.SliderPrefab);
        newSlider.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newSlider.GetComponent<SliderUI>());
        newSlider.GetComponent<SliderUI>().CreateUI(target, property, rangeAttribut, isInteractible,  isFloat);
    }

    private void CreateInput(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newInput = Instantiate(_prefabs.InputPrefab);
        newInput.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newInput.GetComponent<InputFieldUI>());
        newInput.GetComponent<InputFieldUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateCheckbox(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newCheckbox = Instantiate(_prefabs.CheckboxPrefab);
        newCheckbox.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newCheckbox.GetComponent<ToggleUI>());
        newCheckbox.GetComponent<ToggleUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateButton(Transform parent, Controllable target, ClassMethodInfo method)
    {
        //Methods marked [OCFMethod(showInUI = false)] stay OSC-callable but get no button.
        var ocfMethod = Attribute.GetCustomAttribute(method.methodInfo, typeof(OCFMethod)) as OCFMethod;
        if (ocfMethod != null && !ocfMethod.showInUI)
            return;

        //As we can't expose parameter in UI, ignore methods with arguments
        if (method.methodInfo.GetParameters().Length == 0)
        {
            var newButton = Instantiate(_prefabs.MethodButtonPrefab);
            newButton.transform.SetParent(parent);
            newButton.transform.SetSiblingIndex(parent.childCount-2);
            parent.gameObject.GetComponent<PanelUI>().AddUIElement(newButton.GetComponent<ButtonUI>());
            newButton.GetComponent<ButtonUI>().CreateUI(target, method);
        }
        else
        {
            foreach (var parameter in method.methodInfo.GetParameters())
            {
                //Will do cool stuff in the future
            }
        }
    }

    private void CreateColor(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newColor = Instantiate(_prefabs.ColorPrefab);
        newColor.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newColor.GetComponent<ColorUI>());
        newColor.GetComponent<ColorUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector3(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newVector3 = Instantiate(_prefabs.Vector3Prefab);
        newVector3.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector3.GetComponent<Vector3UI>());
        newVector3.GetComponent<Vector3UI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector4(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newVector4 = Instantiate(_prefabs.Vector4Prefab);
        newVector4.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector4.GetComponent<Vector4UI>());
        newVector4.GetComponent<Vector4UI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector3Int(Transform parent, Controllable target, FieldInfo property, bool isInteractible) {
        var newVector3Int = Instantiate(_prefabs.Vector3IntPrefab);
        newVector3Int.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector3Int.GetComponent<Vector3IntUI>());
        newVector3Int.GetComponent<Vector3IntUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector2(Transform parent, Controllable target, FieldInfo property, bool isInteractible) {
        var newVector2 = Instantiate(_prefabs.Vector2Prefab);
        newVector2.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector2.GetComponent<Vector2UI>());
        newVector2.GetComponent<Vector2UI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector2Int(Transform parent, Controllable target, FieldInfo property, bool isInteractible) {
        var newVector2Int = Instantiate(_prefabs.Vector2IntPrefab);
        newVector2Int.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector2Int.GetComponent<Vector2IntUI>());
        newVector2Int.GetComponent<Vector2IntUI>().CreateUI(target, property, isInteractible);
    }

    public void ClickOnDropdown()
    {
        ControllableMaster.RefreshAllPresets();
    }

    #endregion

    #region Right Click Menu

    void InitializeRightClickMenu()
    {
        rightClickMenu.gameObject.SetActive(false);
        rightClickMenu.closeButton.onClick.AddListener(CloseRightClickMenu);
        rightClickMenu.copyAddressButton.onClick.AddListener(OnCopyAddressClick);
    }

    public void CreateRightClickMenu(ControllableUI controllableUI)
    {
        rightClickMenu.gameObject.SetActive(true);
        rightClickMenu.transform.position = Mouse.current.position.value;
        rightClickMenu.linkedUI = controllableUI;
    }

    void CloseRightClickMenu()
    {
        rightClickMenu.gameObject.SetActive(false);
    }

    void OnCopyAddressClick()
    {
        rightClickMenu.linkedUI.CopyAddressToClipboard();
        CloseRightClickMenu();
    }

    #endregion

    #region Color Picker

    void InitializeColorPicker()
    {
        colorPicker.gameObject.SetActive(false);
        colorPicker.closeButton.onClick.AddListener(CloseColorPicker);
    }

    public void CreateColorPicker(ControllableUI controllableUI)
    {
        //Opening the picker on another member ends the session on the previous one, so a pick that is
        //never explicitly closed still records its single undo.
        EndColorPickerEdit();

        colorPicker.transform.position = Mouse.current.position.value;
        colorPicker.linkedUI = controllableUI as ColorUI;
        colorPicker.gameObject.SetActive(true);

        if (colorPicker.linkedUI != null)
            colorPicker.linkedUI.BeginPickerEdit();
    }

    void CloseColorPicker()
    {
        EndColorPickerEdit();

        colorPicker.gameObject.SetActive(false);
    }

    //A colour pick is one edit for as long as the picker is open, however many colours it travels
    //through, so the undo entry is recorded here rather than on every push from the picker.
    void EndColorPickerEdit()
    {
        if (colorPicker.linkedUI != null)
            colorPicker.linkedUI.EndPickerEdit();
    }

    #endregion

    #region UI Transform

    void ResetUITransform()
    {
        //Set scale from screen size to always get a visible UI
        UIScale = Screen.width * 1.5f / 1920;

        _scrollViewTransform.anchoredPosition = Vector2.zero;
    }

    void UpdateUITransform()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[resetUIKey].wasPressedThisFrame)
                ResetUITransform();

        UpdateUIScale();

        if(enableUIMovement)
            UpdateUIPosition();
    }

    void UpdateUIScale()
    {
        //The '=' and '-' keys are found by what they print on the current layout, so Ctrl +/- lands on
        //the same characters everywhere. The numpad needs no such lookup: it prints the same on every
        //layout.
        if (Keyboard.current.pageUpKey.isPressed ||
            (Keyboard.current.ctrlKey.isPressed && (IsPressed(KeyPrinting("=", Key.Equals)) || Keyboard.current.numpadPlusKey.isPressed)))
        {
            //Avoid scaling the UI if currently writing in an input field
            if (FocusedInputField() != null)
                return;

            UIScale += _uiScaleSpeed * Time.deltaTime;
        }

        if (Keyboard.current.pageDownKey.isPressed ||
            (Keyboard.current.ctrlKey.isPressed && (IsPressed(KeyPrinting("-", Key.Minus)) || Keyboard.current.numpadMinusKey.isPressed)))
        {
            //Avoid scaling the UI if currently writing in an input field
            if (FocusedInputField() != null)
                return;

            UIScale -= _uiScaleSpeed * Time.deltaTime;
        }
    }

    void UpdateUIPosition()
    {
        if (Keyboard.current.ctrlKey.isPressed)
        {
            //Avoid scaling the UI if currently writing in an input field
            if (FocusedInputField() != null)
                return;

            if (Keyboard.current.leftArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.left * _uiMovementSpeed * Time.deltaTime;

            if (Keyboard.current.rightArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.right * _uiMovementSpeed * Time.deltaTime;

            if (Keyboard.current.upArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.up * _uiMovementSpeed * Time.deltaTime;

            if (Keyboard.current.downArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.down * _uiMovementSpeed * Time.deltaTime;

        }
    }

    #endregion
}