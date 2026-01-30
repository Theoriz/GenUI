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

    public float UIScale
    {
        get => _UIScale;
        set { 
            _UIScale = value;
            _canvasScaler.scaleFactor = _UIScale;
        }
    }

    [HideInInspector] public GameObject PanelPrefab;
    [HideInInspector] public GameObject MethodButtonPrefab;
    [HideInInspector] public GameObject SliderPrefab;
    [HideInInspector] public GameObject CheckboxPrefab;
    [HideInInspector] public GameObject InputPrefab;
    [HideInInspector] public GameObject DropdownPrefab;
    [HideInInspector] public GameObject HeaderTextPrefab;
	[HideInInspector] public GameObject TooltipTextPrefab;
    [HideInInspector] public GameObject ColorPrefab;
    [HideInInspector] public GameObject Vector2Prefab;
    [HideInInspector] public GameObject Vector2IntPrefab;
    [HideInInspector] public GameObject Vector3Prefab;
    [HideInInspector] public GameObject Vector3IntPrefab;

    [Header("Components")]
    public Transform MainPanel;
    public RightClickMenu rightClickMenu;
    public ColorPicker colorPicker;

    [Header("Debug")]
    public bool showDebug = false;

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

        InitializeRightClickMenu();
        InitializeColorPicker();

        Instance = this;
        _panels = new Dictionary<string, GameObject>();

        ControllableMaster.controllableAdded += CreateUI;
        ControllableMaster.controllableRemoved += RemoveUI;

        _rootCanvas = transform.GetChild(0).gameObject;
        _canvasScaler = _rootCanvas.GetComponent<CanvasScaler>();
        _scrollViewTransform = (RectTransform)_rootCanvas.transform.GetChild(0);
        displayUI = true;

        ResetUITransform();

        if (HideUIAtStart)
            ToggleUI();
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
        if (Keyboard.current.f1Key.wasPressedThisFrame)
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

        //if(_destroyRightClickMenuOnNextFrame)
        //{
        //    _destroyRightClickMenuOnNextFrame = false;
        //    _rightClickMenuInstantiated = false;
        //    Destroy(_rightClickMenu);
        //}

        //if (_destroyColorPickerOnNextFrame)
        //{
        //    _destroyColorPickerOnNextFrame = false;
        //    _colorPickerInstantiated = false;
        //    Destroy(_colorPicker);
        //}

        //if ((Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame) && !_skipNextButton)
        //{
        //    if(_rightClickMenuInstantiated)
        //        _destroyRightClickMenuOnNextFrame = true;

        //    if (_colorPickerInstantiated)
        //        _destroyColorPickerOnNextFrame = true;
        //}

        //if(_skipNextButton)
        //{
        //    _skipNextButton = false;
        //}
    }

    public void RemoveUI(Controllable dyingControllable)
    {
        if (!dyingControllable.usePanel) return;
        
        if (showDebug)
            Debug.Log("Removing UI for " + dyingControllable.id);

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

        //First we create a panel for the controllable
        var newControllableHolder = Instantiate(PanelPrefab);
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
            if (!attribute.ShowInUI) continue;

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
			if (!string.IsNullOrEmpty(attribute.TargetList) && !propertyDrawn)
            {
                var associatedListFieldInfo = newControllable.getFieldInfoByName(attribute.TargetList);
                CreateDropDown(newPanel.transform, newControllable, associatedListFieldInfo, property.Value);

				propertyDrawn = true;
                //continue;
            }

            if(!string.IsNullOrEmpty(attribute.enumName) && !propertyDrawn)
            {
                var associatedListFieldInfo = newControllable.getFieldInfoByName(attribute.TargetList);
                CreateDropDown(newPanel.transform, newControllable, associatedListFieldInfo, property.Value, attribute.enumName);

                propertyDrawn = true;
                //continue;
            }
            //property.Value.Attributes O
            if (propertyType.ToString() == "System.Single" || propertyType.ToString() == "System.Int32" && !propertyDrawn)
            {
                var rangeAttribut = (RangeAttribute[]) property.Value.GetCustomAttributes(typeof(RangeAttribute), false);

                bool isFloat = propertyType.ToString() != "System.Int32";

                if (rangeAttribut.Length == 0)
                    CreateInput(newPanel.transform, newControllable, property.Value, attribute.isInteractible);
                else
                    CreateSlider(newPanel.transform, newControllable, property.Value, rangeAttribut[0], attribute.isInteractible, isFloat);

				propertyDrawn = true;
				//continue;
			}
            if (propertyType.ToString() == "System.Boolean" && !propertyDrawn)
            {
                CreateCheckbox(newPanel.transform, newControllable, property.Value, attribute.isInteractible);

				propertyDrawn = true;
				//continue;
			}
            if ((propertyType.ToString() == "System.Int32" || propertyType.ToString() == "System.Float" || propertyType.ToString() == "System.String") && !propertyDrawn)
            {
                CreateInput(newPanel.transform, newControllable, property.Value, attribute.isInteractible);

				propertyDrawn = true;
				//continue;
			}

            if (propertyType.ToString() == "UnityEngine.Color" && !propertyDrawn)
            {
                CreateColor(newPanel.transform, newControllable, property.Value, attribute.isInteractible);

				propertyDrawn = true;
				//continue;
			}

            if(propertyType.ToString() == "UnityEngine.Vector3" && !propertyDrawn)
            {
                CreateVector3(newPanel.transform, newControllable, property.Value, attribute.isInteractible);

				propertyDrawn = true;
				//continue;
			}

            if (propertyType.ToString() == "UnityEngine.Vector3Int" && !propertyDrawn) {
                CreateVector3Int(newPanel.transform, newControllable, property.Value, attribute.isInteractible);

                propertyDrawn = true;
                //continue;
            }

            if (propertyType.ToString() == "UnityEngine.Vector2" && !propertyDrawn) {
                CreateVector2(newPanel.transform, newControllable, property.Value, attribute.isInteractible);

                propertyDrawn = true;
                //continue;
            }

            if (propertyType.ToString() == "UnityEngine.Vector2Int" && !propertyDrawn) {
                CreateVector2Int(newPanel.transform, newControllable, property.Value, attribute.isInteractible);

                propertyDrawn = true;
                //continue;
            }

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
        //Order Save and Load preset buttons
        var lastPanel = _panels[controllableId].transform.GetChild(1);
        var allText = lastPanel.GetComponentsInChildren<Text>();
        var usePreset = false;
        foreach (var text in allText)
        {
            if (text.text == "Save" || text.text == "Save As" || text.text == "Load" || text.text == "Show")
            {
                text.transform.parent.SetParent(lastPanel.transform.Find("PresetHolder"));
                usePreset = true;
            }

            if (text.text == "Save All" || text.text == "Save As All" || text.text == "Load All")
            {
                var globalPresetHolder = lastPanel.transform.Find("AllPresetHolder");
                if (globalPresetHolder == null)
                {
                    globalPresetHolder = Instantiate(lastPanel.transform.Find("PresetHolder"));
                    globalPresetHolder.name = "AllPresetHolder";
                    globalPresetHolder.transform.SetParent(lastPanel.transform);
                    globalPresetHolder.transform.SetSiblingIndex(1); //Set first
                }
                text.transform.parent.SetParent(globalPresetHolder);
            }
        }

        if (usePreset)
        {
            lastPanel.transform.Find("PresetHolder").SetSiblingIndex(lastPanel.transform.childCount - 2); //last index being the preset list
        }
        else
            lastPanel.transform.Find("PresetHolder").gameObject.SetActive(false);

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
        var headerText = Instantiate(HeaderTextPrefab);
        headerText.transform.SetParent(parent);
        headerText.GetComponent<HeaderUI>().CreateUI(target, text);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(headerText.GetComponent<HeaderUI>());
    }

	private void CreateTooltipText(Transform parent, Controllable target, string text) {
		var tooltipText = Instantiate(TooltipTextPrefab);
		tooltipText.transform.SetParent(parent);
		tooltipText.GetComponent<TooltipUI>().CreateUI(target, text);
		parent.gameObject.GetComponent<PanelUI>().AddUIElement(tooltipText.GetComponent<TooltipUI>());
	}

    private void CreateDropDown(Transform parent, Controllable target, FieldInfo listProperty, FieldInfo activeElement, string enumName = "")
    {
        var newDropdown = Instantiate(DropdownPrefab);
        newDropdown.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newDropdown.GetComponent<DropdownUI>());
        if(string.IsNullOrEmpty(enumName))
            newDropdown.GetComponent<DropdownUI>().CreateUI(target, listProperty, activeElement);
        else
            newDropdown.GetComponent<DropdownUI>().CreateUI(target, activeElement, enumName);
    }

    private void CreateSlider(Transform parent, Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible, bool isFloat = true)
    {
        var newSlider = Instantiate(SliderPrefab);
        newSlider.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newSlider.GetComponent<SliderUI>());
        newSlider.GetComponent<SliderUI>().CreateUI(target, property, rangeAttribut, isInteractible,  isFloat);
    }

    private void CreateInput(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newInput = Instantiate(InputPrefab);
        newInput.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newInput.GetComponent<InputFieldUI>());
        newInput.GetComponent<InputFieldUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateCheckbox(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newCheckbox = Instantiate(CheckboxPrefab);
        newCheckbox.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newCheckbox.GetComponent<ToggleUI>());
        newCheckbox.GetComponent<ToggleUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateButton(Transform parent, Controllable target, ClassMethodInfo method)
    {
        //As we can't expose parameter in UI, ignore methods with arguments 
        if (method.methodInfo.GetParameters().Length == 0)
        {
            var newButton = Instantiate(MethodButtonPrefab);
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
        var newColor = Instantiate(ColorPrefab);
        newColor.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newColor.GetComponent<ColorUI>());
        newColor.GetComponent<ColorUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector3(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newVector3 = Instantiate(Vector3Prefab);
        newVector3.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector3.GetComponent<Vector3UI>());
        newVector3.GetComponent<Vector3UI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector3Int(Transform parent, Controllable target, FieldInfo property, bool isInteractible) {
        var newVector3Int = Instantiate(Vector3IntPrefab);
        newVector3Int.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector3Int.GetComponent<Vector3IntUI>());
        newVector3Int.GetComponent<Vector3IntUI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector2(Transform parent, Controllable target, FieldInfo property, bool isInteractible) {
        var newVector2 = Instantiate(Vector2Prefab);
        newVector2.transform.SetParent(parent);
        parent.gameObject.GetComponent<PanelUI>().AddUIElement(newVector2.GetComponent<Vector2UI>());
        newVector2.GetComponent<Vector2UI>().CreateUI(target, property, isInteractible);
    }

    private void CreateVector2Int(Transform parent, Controllable target, FieldInfo property, bool isInteractible) {
        var newVector2Int = Instantiate(Vector2IntPrefab);
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
        if(Keyboard.current.f2Key.wasPressedThisFrame)
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
            if (EventSystem.current.currentSelectedGameObject)
            {
                if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                {
                    return;
                }
            }

            UIScale += _UIScaleSpeed * Time.deltaTime;
        }

        if (Keyboard.current.pageDownKey.isPressed || 
            (Keyboard.current.ctrlKey.isPressed && (Keyboard.current.digit6Key.isPressed || Keyboard.current.numpadMinusKey.isPressed)))
        {
            //Avoid scaling the UI if currently writing in an input field
            if (EventSystem.current.currentSelectedGameObject)
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
            if (EventSystem.current.currentSelectedGameObject)
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