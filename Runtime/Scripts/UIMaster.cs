using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.InputSystem;

public class UIMaster : MonoBehaviour
{
    public static UIMaster Instance;
    
    public bool AutoHideCursor
    {
        get => _AutoHideCursor;
        set { _AutoHideCursor = value; UpdateUI(); }
    }

    [Header("Global settings")]
    [SerializeField] private bool _AutoHideCursor = true;

    public bool HideUIAtStart;
    public bool enableUIMovement = true;

    [Header("Shortcuts")]
    public Key toggleUIKey = Key.F1;
    public Key resetUIKey = Key.F2;

    public float UIScale
    {
        get => _UIScale;
        set { 
            _UIScale = value;
            _canvasScaler.scaleFactor = _UIScale;
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

    private float _UIScale = 1;
    private const float _UIScaleSpeed = 2;
    private const float _UIMovementSpeed = 500;

    // Use this for initialization
    void Awake()
    {
        //Enable canvas that is disabled by default in prefab to not be visible in scene view.
        transform.GetChild(0).gameObject.SetActive(true);

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

        var scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null)
            MainPanel = scrollRect.content;
        else
            Debug.LogError("[GenUI] No ScrollRect found under UIMaster; the panel container is missing.");

        var rightClickMenuObject = Instantiate(_prefabs.RightClickMenuPrefab, _rootCanvas.transform, false);
        rightClickMenuObject.transform.SetAsLastSibling();
        rightClickMenu = rightClickMenuObject.GetComponentInChildren<RightClickMenu>(true);

        var colorPickerObject = Instantiate(_prefabs.ColorPickerPrefab, _rootCanvas.transform, false);
        colorPickerObject.transform.SetAsLastSibling();
        colorPicker = colorPickerObject.GetComponentInChildren<ColorPicker>(true);
    }

    void OnDestroy()
    {
        // Static event: must unsubscribe or destroyed instances keep receiving
        // callbacks when Domain Reload is disabled
        ControllableMaster.controllableAdded -= CreateUI;
        ControllableMaster.controllableRemoved -= RemoveUI;

        if (Instance == this)
            Instance = null;
    }

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

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleUIKey].wasPressedThisFrame)
        {
			//Avoid toggling the UI if currently writing in an input field
			if (EventSystem.current.currentSelectedGameObject) {
				if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()) {
					return;
				}
			}
            ToggleUI();
        }

        if(displayUI)
            UpdateUITransform();

    }

    public void RemoveUI(Controllable dyingControllable)
    {
        if (!dyingControllable.usePanel) return;
        
        if (showDebug)
            Debug.Log("Removing UI for " + dyingControllable.id);

        if (!_panels.ContainsKey(dyingControllable.id))
            return;

        if (_panels[dyingControllable.id] != null)
            _panels[dyingControllable.id].GetComponentInChildren<PanelUI>().RemoveUI();

        Destroy(_panels[dyingControllable.id]);
        _panels.Remove(dyingControllable.id);
    }

    public void CreateUI(Controllable newControllable)
    {
        if(showDebug)
            Debug.Log("Adding " + newControllable.id + ", use panel : " + newControllable.usePanel);

        if (!newControllable.usePanel) return;

        if (_panels.ContainsKey(newControllable.id))
        {
            if (showDebug)
                Debug.LogWarning("[GenUI] A panel for '" + newControllable.id + "' already exists; skipping.");
            return;
        }

        //First we create a panel for the controllable
        var newControllableHolder = Instantiate(_prefabs.PanelPrefab);
        newControllableHolder.transform.GetChild(0).GetComponent<Image>().color = newControllable.BarColor;
        newControllableHolder.transform.SetParent(MainPanel.transform);

        var newPanel = newControllableHolder.transform.GetChild(1).gameObject;
        newPanel.GetComponentInChildren<Text>().text = newControllable.id;
        newPanel.transform.GetChild(0).GetChild(0).GetComponentInChildren<Image>().color = newControllable.BarColor;

        _panels.Add(newControllable.id, newControllableHolder);

        //Read all properties and add associated UI
        foreach (var property in newControllable.Fields)
        {
            var propertyType = property.Value.FieldType;
            OSCProperty attribute = Attribute.GetCustomAttribute(property.Value, typeof(OSCProperty)) as OSCProperty;

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
                var associatedListFieldInfo = newControllable.getFieldInfoByName(attribute.targetList);
                CreateDropDown(newPanel.transform, newControllable, associatedListFieldInfo, property.Value);

				propertyDrawn = true;
                //continue;
            }

            if(!string.IsNullOrEmpty(attribute.enumName) && !propertyDrawn)
            {
                var associatedListFieldInfo = newControllable.getFieldInfoByName(attribute.targetList);
                CreateDropDown(newPanel.transform, newControllable, associatedListFieldInfo, property.Value, attribute.enumName);

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
                Debug.LogWarning("[GenUI] No widget created for '" + property.Value.Name + "' on " + newControllable.id + " : unsupported type " + propertyType + ".");

            //Add tooltip if it exists
            var tooltipAttribut = (TooltipAttribute[])property.Value.GetCustomAttributes(typeof(TooltipAttribute), false);
			if (tooltipAttribut.Length != 0) {
				CreateTooltipText(newPanel.transform, newControllable, tooltipAttribut[0].tooltip);
			}
		}

        //Read all methods and add button
        foreach (var method in newControllable.Methods)
        {
            if (showDebug)
                Debug.Log("[UI] Adding button for (" + newControllable.GetType() + ") : " + method.Value.methodInfo.Name);

            CreateButton(newPanel.transform, newControllable, method.Value);
        }

        CleanGeneratedUI(newControllable.id, newControllable);       
    }

    public void CleanGeneratedUI(string controllableId, Controllable controllable)
    {
        //Order Save and Load preset buttons. Buttons are identified by the name of the method they
        //invoke, not by their label: the label is derived from the method name by ParseNameString,
        //and a panel's title Text also lives in this subtree.
        var lastPanel = _panels[controllableId].transform.GetChild(1);
        var presetHolder = lastPanel.Find("PresetHolder");
        var isGlobalPresetPanel = controllable is ControllableMasterControllable;

        //Create the global preset holder up front, while PresetHolder is still empty. Cloning it
        //lazily once the first SaveAll button appears would copy the Save/Load buttons already
        //reparented into PresetHolder, stacking them into this top row too.
        Transform globalPresetHolder = null;
        if (isGlobalPresetPanel)
        {
            globalPresetHolder = Instantiate(presetHolder);
            globalPresetHolder.name = "AllPresetHolder";
            globalPresetHolder.SetParent(lastPanel);
            globalPresetHolder.SetSiblingIndex(1); //Set first
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
        }

        if (usePreset)
        {
            presetHolder.SetSiblingIndex(lastPanel.transform.childCount - 2); //last index being the preset list
        }
        else
            presetHolder.gameObject.SetActive(false);

        lastPanel.GetComponent<PanelUI>().Init(controllable);

        //Close panel if needed
        if (controllable.closePanelAtStart)
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

    private void CreateDropDown(Transform parent, Controllable target, FieldInfo listProperty, FieldInfo activeElement, string enumName = "")
    {
        var newDropdown = Instantiate(_prefabs.DropdownPrefab);
        newDropdown.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newDropdown.GetComponent<DropdownUI>());
        if(string.IsNullOrEmpty(enumName))
            newDropdown.GetComponent<DropdownUI>().CreateUI(target, listProperty, activeElement);
        else
            newDropdown.GetComponent<DropdownUI>().CreateUI(target, activeElement, enumName);
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
        colorPicker.transform.position = Mouse.current.position.value;
        colorPicker.linkedUI = controllableUI as ColorUI;
        colorPicker.gameObject.SetActive(true);
    }

    void CloseColorPicker()
    {
        colorPicker.gameObject.SetActive(false);
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
        if (Keyboard.current.pageUpKey.isPressed || 
            (Keyboard.current.ctrlKey.isPressed && (Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed)))
        {
            //Avoid scaling the UI if currently writing in an input field
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject)
            {
                if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                {
                    return;
                }
            }

            UIScale += _UIScaleSpeed * Time.deltaTime;
        }

        //Key enum values are physical positions named after US QWERTY: minusKey prints '-' on QWERTY,
        //digit6Key prints '-' on AZERTY. Both are bound so Ctrl+- zooms out on either layout.
        if (Keyboard.current.pageDownKey.isPressed ||
            (Keyboard.current.ctrlKey.isPressed && (Keyboard.current.minusKey.isPressed || Keyboard.current.digit6Key.isPressed || Keyboard.current.numpadMinusKey.isPressed)))
        {
            //Avoid scaling the UI if currently writing in an input field
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject)
            {
                if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                {
                    return;
                }
            }

            UIScale -= _UIScaleSpeed * Time.deltaTime;
        }
    }

    void UpdateUIPosition()
    {
        if (Keyboard.current.ctrlKey.isPressed)
        {
            //Avoid scaling the UI if currently writing in an input field
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject)
            {
                if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                {
                    return;
                }
            }

            if (Keyboard.current.leftArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.left * _UIMovementSpeed * Time.deltaTime;

            if (Keyboard.current.rightArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.right * _UIMovementSpeed * Time.deltaTime;

            if (Keyboard.current.upArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.up * _UIMovementSpeed * Time.deltaTime;

            if (Keyboard.current.downArrowKey.isPressed)
                _scrollViewTransform.anchoredPosition += Vector2.down * _UIMovementSpeed * Time.deltaTime;

        }
    }

    #endregion
}