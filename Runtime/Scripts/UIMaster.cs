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

    public bool HideUIAtStart = true;
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

    // The panel and the popups are built in Awake, so UIMaster carries no serialized wiring references.
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

        ResolveLinks();

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

    // Resolve the panel container and build the popups without any serialized reference: the panel
    // is the scroll view's content, and the popups build themselves.
    void ResolveLinks()
    {
        _scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (_scrollRect != null)
            MainPanel = _scrollRect.content;
        else
            Debug.LogError("[GenUI] No ScrollRect found under UIMaster; the panel container is missing.");

        //Last, so both popups draw over every panel.
        rightClickMenu = RightClickMenu.Build(_rootCanvas.transform);
        rightClickMenu.transform.SetAsLastSibling();

        colorPicker = ColorPicker.Build(_rootCanvas.transform);
        colorPicker.transform.SetAsLastSibling();
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
        //Panel appearance is GenUI's own, so it comes from an optional sibling component rather than
        //from the Controllable; see GenUIPanelSettings.
        var usePanel = GenUIPanelSettings.UsePanel(newControllable);

        if(showDebug)
            Debug.Log("Adding " + newControllable.controllableId + ", use panel : " + usePanel);

        if (!usePanel) return;

        if (_panels.ContainsKey(newControllable.controllableId))
        {
            if (showDebug)
                Debug.LogWarning("[GenUI] A panel for '" + newControllable.controllableId + "' already exists; skipping.");
            return;
        }

        var barColor = GenUIPanelSettings.BarColorFor(newControllable);

        //First we create a panel for the controllable
        var panel = PanelUI.Build(MainPanel, newControllable.controllableId, barColor);
        var newPanel = panel.gameObject;

        _panels.Add(newControllable.controllableId, panel.Root);

        //Read all properties and add associated UI
        foreach (var property in newControllable.controllableFields)
        {
            var propertyType = property.Value.FieldType;
            OCFProperty attribute = Attribute.GetCustomAttribute(property.Value, typeof(OCFProperty)) as OCFProperty;

            //Check if needs to be in UI
            if (!attribute.showInUI) continue;

            if (showDebug)
                Debug.Log("[UI] Adding control for (" + newControllable.GetType() + ") : " + property.Value.Name + " of type : " + propertyType.ToString());

            //Which widget the member gets is MemberDescriptor's decision, not this loop's; here we
            //only build what it names.
            var descriptor = MemberDescriptor.Describe(newControllable, property.Value, attribute);

            if (descriptor.Header != null)
                CreateHeaderText(newPanel.transform, newControllable, descriptor.Header);

            var isInteractible = !descriptor.ReadOnly;

            switch (descriptor.Kind)
            {
                case WidgetKind.Dropdown:
                    CreateDropDown(newPanel.transform, newControllable, property.Value, isInteractible,
                        targetListName: descriptor.TargetList, enumType: descriptor.EnumType);
                    break;
                case WidgetKind.Slider:
                    CreateSlider(newPanel.transform, newControllable, property.Value,
                        new RangeAttribute(descriptor.Min, descriptor.Max), isInteractible, descriptor.IsFloat);
                    break;
                case WidgetKind.Input:
                    CreateInput(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                case WidgetKind.Toggle:
                    CreateCheckbox(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                case WidgetKind.Color:
                    CreateColor(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                case WidgetKind.Vector2:
                    CreateVector2(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                case WidgetKind.Vector2Int:
                    CreateVector2Int(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                case WidgetKind.Vector3:
                    CreateVector3(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                case WidgetKind.Vector3Int:
                    CreateVector3Int(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                case WidgetKind.Vector4:
                    CreateVector4(newPanel.transform, newControllable, property.Value, isInteractible);
                    break;
                default:
                    Debug.LogWarning(descriptor.SkipReason);
                    break;
            }

            if (descriptor.Tooltip != null)
                CreateTooltipText(newPanel.transform, newControllable, descriptor.Tooltip);
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
        var panel = _panels[controllableId].GetComponentInChildren<PanelUI>();
        var lastPanel = panel.transform;
        var presetHolder = panel.PresetHolder;
        var isGlobalPresetPanel = controllable is ControllableMasterControllable;

        //The global buttons get sections of their own, at the top of the panel.
        RectTransform globalPresetSection = null;
        RectTransform globalPresetHolder = null;
        RectTransform globalActionSection = null;
        RectTransform globalActionHolder = null;
        if (isGlobalPresetPanel)
        {
            globalPresetSection = panel.CreatePresetSection("AllPresetSection", out globalPresetHolder);
            globalPresetSection.SetSiblingIndex(panel.FirstRowIndex); //Set first

            //Own row, directly under the preset row: these buttons have long labels and do not fit
            //alongside Save All / Save As All.
            globalActionSection = panel.CreatePresetSection("GlobalActionSection", out globalActionHolder);
            globalActionSection.SetSiblingIndex(panel.FirstRowIndex + 1);
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

        //The preset dropdown belongs to the same block as the buttons, so it joins them in the section.
        if (usePreset)
        {
            foreach (var dropdown in lastPanel.GetComponentsInChildren<DropdownUI>())
            {
                if (dropdown.Property == null
                    || dropdown.Property.Name != nameof(Controllable.controllableCurrentPreset)) continue;

                dropdown.transform.SetParent(panel.PresetSection);
                dropdown.transform.SetAsLastSibling();
                break;
            }

            panel.PresetSection.SetAsLastSibling();
            panel.LayoutSection(panel.PresetSection);
        }
        else
            panel.HideSection(panel.PresetSection);

        if (isGlobalPresetPanel)
        {
            //Nothing landed in it, so it would otherwise render as an empty block.
            if (globalActionHolder.childCount == 0)
                panel.HideSection(globalActionSection);
            else
                panel.LayoutSection(globalActionSection);

            panel.LayoutSection(globalPresetSection);
        }

        //After the preset buttons have left the body, so the gap lands above the first button that stays.
        panel.AddMethodGap();

        //After the sections have been ordered, since one of them can be the panel's first row.
        panel.TrimTitleGap();

        panel.Init(controllable);

        //Close panel if needed
        if (GenUIPanelSettings.ClosePanelAtStart(controllable))
            panel.Close();
        else
            panel.Open();

        //Order the panels. The rule is GenUIPanelSettings', so the browser mirror orders identically.
        //Sibling order is all there is to set: the scroll content lays its children out top to bottom.
        var panelIds = _panels.Keys.ToArray();
        Array.Sort(panelIds, (idA, idB) =>
            GenUIPanelSettings.ComparePanels(idA, PanelPriority(idA), idB, PanelPriority(idB)));

        for(int i = 0; i < panelIds.Length; i++)
        {
            _panels[panelIds[i]].transform.SetAsLastSibling();
        }
    }

    //A panel outliving its controllable would be a bug, since RemoveUI destroys it on unregistration,
    //but ordering is not where that should throw.
    private static int PanelPriority(string controllableId)
    {
        return ControllableMaster.RegisteredControllables.TryGetValue(controllableId, out var controllable)
               && controllable != null
            ? GenUIPanelSettings.PanelPriority(controllable)
            : 0;
    }

    private void CreateHeaderText(Transform parent, Controllable target, string text)
    {
        ControllableUI.Create<HeaderUI>(parent).CreateUI(target, text);
    }

    private void CreateTooltipText(Transform parent, Controllable target, string text)
    {
        ControllableUI.Create<TooltipUI>(parent).CreateUI(target, text);
    }

    //One widget, two sources for its entries: the entries of a named List<string>, or the members of
    //the field's own enum type. Exactly one of the two is set by the caller.
    private void CreateDropDown(Transform parent, Controllable target, FieldInfo activeElement, bool isInteractible, string targetListName = null, Type enumType = null)
    {
        var newDropdown = ControllableUI.Create<DropdownUI>(parent);
        if (enumType != null)
            newDropdown.CreateUI(target, activeElement, enumType, isInteractible);
        else
            newDropdown.CreateUI(target, targetListName, activeElement, isInteractible);
    }

    private void CreateSlider(Transform parent, Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible, bool isFloat = true)
    {
        ControllableUI.Create<SliderUI>(parent).CreateUI(target, property, rangeAttribut, isInteractible, isFloat);
    }

    private void CreateInput(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        ControllableUI.Create<InputFieldUI>(parent).CreateUI(target, property, isInteractible);
    }

    private void CreateCheckbox(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        ControllableUI.Create<ToggleUI>(parent).CreateUI(target, property, isInteractible);
    }

    private void CreateButton(Transform parent, Controllable target, ClassMethodInfo method)
    {
        //Methods marked [OCFMethod(showInUI = false)] stay OSC-callable but get no button. The
        //options come from ClassMethodInfo, which knows which method they were declared on.
        var ocfMethod = method.Options;
        if (ocfMethod != null && !ocfMethod.showInUI)
            return;

        //As we can't expose parameter in UI, ignore methods with arguments
        if (method.methodInfo.GetParameters().Length == 0)
        {
            //Appended after the member rows, wherever they end. CleanGeneratedUI is what moves the
            //preset buttons out of the body afterwards and sets the gap above the first one left.
            var newButton = ControllableUI.Create<ButtonUI>(parent);
            newButton.CreateUI(target, method);
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
        ControllableUI.Create<ColorUI>(parent).CreateUI(target, property, isInteractible);
    }

    private void CreateVector3(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        ControllableUI.Create<Vector3UI>(parent).CreateUI(target, property, isInteractible);
    }

    private void CreateVector4(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        ControllableUI.Create<Vector4UI>(parent).CreateUI(target, property, isInteractible);
    }

    private void CreateVector3Int(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        ControllableUI.Create<Vector3IntUI>(parent).CreateUI(target, property, isInteractible);
    }

    private void CreateVector2(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        ControllableUI.Create<Vector2UI>(parent).CreateUI(target, property, isInteractible);
    }

    private void CreateVector2Int(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        ControllableUI.Create<Vector2IntUI>(parent).CreateUI(target, property, isInteractible);
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
        //Only the menu moves: the root covers the screen so that clicking anywhere else closes it.
        UIFactory.PlacePopup(rightClickMenu.Content, Mouse.current.position.value);
        rightClickMenu.linkedUI = controllableUI;
    }

    void CloseRightClickMenu()
    {
        rightClickMenu.gameObject.SetActive(false);
    }

    void OnCopyAddressClick()
    {
        if (rightClickMenu.linkedUI != null)
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

        colorPicker.linkedUI = controllableUI as ColorUI;
        colorPicker.gameObject.SetActive(true);

        //Placed after activation: a disabled rect reports no size, so it could not be kept on screen.
        UIFactory.PlacePopup(colorPicker.Content, Mouse.current.position.value);

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