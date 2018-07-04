﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIMaster : MonoBehaviour
{
    public Transform MainPanel;
    public GameObject PanelPrefab;
    public GameObject MethodButtonPrefab;
    public GameObject SliderPrefab;
    public GameObject CheckboxPrefab;
    public GameObject InputPrefab;
    public GameObject DropdownPrefab;
    public GameObject HeaderTextPrefab;
    public GameObject ColorPrefab;
    public GameObject Vector3Prefab;

    public bool AutoHideCursor;
    public bool HideUIAtStart;
    public bool showDebug;

    private GameObject _camera;
    private GameObject _rootCanvas;
    private Dictionary<string, GameObject> _panels;

    private bool displayUI;

    public static UIMaster Instance;
    // Use this for initialization
    void Awake()
    {
        Instance = this;
        _panels = new Dictionary<string, GameObject>();

        ControllableMaster.controllableAdded += CreateUI;
        ControllableMaster.controllableRemoved += RemoveUI;

        displayUI = true;

        if (HideUIAtStart)
            ToggleUI();
    }

    public void ToggleUI()
    {
        displayUI = !displayUI;
        if (AutoHideCursor)
            Cursor.visible = displayUI;
        transform.GetChild(0).gameObject.SetActive(displayUI);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleUI();
        }
    }

    public void RemoveUI(Controllable dyingControllable)
    {
        if (!dyingControllable.usePanel) return;

        if (showDebug)
            Debug.Log("Removing UI for " + dyingControllable.name + "|" + dyingControllable.id);
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
        newPanel.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        _panels.Add(newControllable.id, newControllableHolder);

        //Read all methods and add button
        foreach (var method in newControllable.Methods)
        {
            if (showDebug)
                Debug.Log("[UI] Adding button for (" + newControllable.GetType() + ") : " + method.Value.Name);

            CreateButton(newPanel.transform, newControllable, method.Value);
        }

        //Read all properties and add associated UI
        foreach (var property in newControllable.Properties)
        {
            var propertyType = property.Value.FieldType;
            OSCProperty attribute = Attribute.GetCustomAttribute(property.Value, typeof(OSCProperty)) as OSCProperty;

            //Check if needs to be in UI
            if (!attribute.ShowInUI) continue;

            if (showDebug)
                Debug.Log("[UI] Adding control for (" + newControllable.GetType() + ") : " + property.Value.Name);

            //Add header if it exists
            var headerAttribut = (HeaderAttribute[])property.Value.GetCustomAttributes(typeof(HeaderAttribute), false);
            if (headerAttribut.Length != 0)
            {
                CreateHeaderText(newPanel.transform, headerAttribut[0].header);
            }
            //Create list
            if (attribute.TargetList != "" && attribute.TargetList != null)
            {
                var associatedListFieldInfo = newControllable.getFieldInfoByName(attribute.TargetList);
                CreateDropDown(newPanel.transform, newControllable, associatedListFieldInfo, property.Value);
                continue;
            }
            //property.Value.Attributes O
            if (propertyType.ToString() == "System.Single" || propertyType.ToString() == "System.Int32")
            {
                var rangeAttribut = (RangeAttribute[]) property.Value.GetCustomAttributes(typeof(RangeAttribute), false);

                bool isFloat = propertyType.ToString() != "System.Int32";

                if (rangeAttribut.Length == 0)
                    CreateInput(newPanel.transform, newControllable, property.Value, attribute.isInteractible);
                else
                    CreateSlider(newPanel.transform, newControllable, property.Value, rangeAttribut[0], attribute.isInteractible, isFloat);
                continue;
            }
            if (propertyType.ToString() == "System.Boolean")
            {
                CreateCheckbox(newPanel.transform, newControllable, property.Value, attribute.isInteractible);
                continue;
            }
            if (propertyType.ToString() == "System.Int32" || propertyType.ToString() == "System.Float" || propertyType.ToString() == "System.String")
            {
                CreateInput(newPanel.transform, newControllable, property.Value, attribute.isInteractible);
                continue;
            }
            if (showDebug)
                Debug.Log("Type : " + propertyType.ToString());
            if (propertyType.ToString() == "UnityEngine.Color")
            {
                CreateColor(newPanel.transform, newControllable, property.Value, attribute.isInteractible);
                continue;
            }

            if(propertyType.ToString() == "UnityEngine.Vector3")
            {
                CreateVector3(newPanel.transform, newControllable, property.Value, attribute.isInteractible);
                continue;
            }
        }

        //Order Save and Load preset buttons
        var allText = newPanel.GetComponentsInChildren<Text>();
        var usePreset = false;
        foreach (var text in allText)
        {
            if (text.text == "Save" || text.text == "SaveAs" || text.text == "Load" || text.text == "Show")
            {
                text.transform.parent.SetParent(newPanel.transform.Find("PresetHolder"));
                usePreset = true;
            }

            if (text.text == "SaveAll" || text.text == "SaveAsAll" || text.text == "LoadAll")
            {
                var globalPresetHolder = newPanel.transform.Find("AllPresetHolder");
                if (globalPresetHolder == null)
                {
                    globalPresetHolder = Instantiate(newPanel.transform.Find("PresetHolder"));
                    globalPresetHolder.name = "AllPresetHolder";
                    globalPresetHolder.transform.SetParent(newPanel.transform);
                    globalPresetHolder.transform.SetSiblingIndex(1); //Set first
                }
                text.transform.parent.SetParent(globalPresetHolder);
            }
        }
        if (usePreset)
        {
            newPanel.transform.Find("PresetHolder").SetSiblingIndex(newPanel.transform.childCount - 2); //last index being the preset list
        }
        else
            newPanel.transform.Find("PresetHolder").gameObject.SetActive(false);

        //Set OCF on top
        if(_panels.ContainsKey("GenUI"))
        {
            _panels["GenUI"].transform.SetAsFirstSibling();
        }

        newPanel.transform.localScale = Vector3.one;
    }

    private void CreateHeaderText(Transform parent, string text)
    {
        var headerText = Instantiate(HeaderTextPrefab);
        headerText.GetComponent<Text>().text = text;
        headerText.transform.SetParent(parent);
    }

    private void CreateDropDown(Transform parent, Controllable target, FieldInfo listProperty, FieldInfo activeElement)
    {
        var newDropdown = Instantiate(DropdownPrefab);
        newDropdown.transform.SetParent(parent);
        //var trigger = newDropdown.GetComponent<EventTrigger>();
        //EventTrigger.Entry entry = new EventTrigger.Entry();
        //entry.eventID = EventTriggerType.PointerClick;
        //entry.callback.AddListener((eventData) => {  });
        //trigger.triggers.Add(entry);

        //TODO remove string
        var listInObject = (List<string>) listProperty.GetValue(target);
        var activeElementInObject = activeElement.GetValue(target).ToString();
        var activeElementIndex = -1;
        for (var i = 0; i < listInObject.Count; i++)
        {
            if (activeElementInObject == listInObject[i].ToString())
                activeElementIndex = i;
        }
        //Switch active element in list to be the first one, so the displayed one in dropdown
        if (activeElementIndex != -1 && listInObject.Count > 1)
        {
            var tmp = listInObject[0];
            listInObject[0] = listInObject[activeElementIndex];
            listInObject[activeElementIndex] = tmp;
        }

        newDropdown.GetComponent<Dropdown>().value = 0;
        
        newDropdown.GetComponent<Dropdown>().AddOptions(listInObject);
        newDropdown.GetComponent<Dropdown>().onValueChanged.AddListener((value) =>
        {
            var associatedList = (List<string>)listProperty.GetValue(target);
            string activeItem = associatedList[value];

            List<object> objParams = new List<object> { activeItem };
            target.setFieldProp(activeElement, objParams);
        });
        
        target.controllableValueChanged += (name) =>
        {
            //Debug.Log(name+ " UI has been updated with value " + activeElement.GetValue(target).ToString());
            
            activeElementIndex = -1;
            activeElementInObject = activeElement.GetValue(target).ToString();
            listInObject = (List<string>)listProperty.GetValue(target);

            for (var i = 0; i < listInObject.Count; i++)
            {
                if (activeElementInObject == listInObject[i].ToString())
                    activeElementIndex = i;
            }
            //Switch active element in list to be the first one, so the displayed one in dropdown
            if (activeElementIndex != -1 && listInObject.Count > 1)
            {
                var tmp = listInObject[newDropdown.GetComponent<Dropdown>().value];
                listInObject[newDropdown.GetComponent<Dropdown>().value] = listInObject[activeElementIndex];
                listInObject[activeElementIndex] = tmp;
            }
            newDropdown.GetComponent<Dropdown>().ClearOptions();
            newDropdown.GetComponent<Dropdown>().AddOptions(listInObject);
        };
        newDropdown.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateSlider(Transform parent, Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible, bool isFloat = true)
    {
        //Debug.Log("Range : " + rangeAttribut.min + ";" + rangeAttribut.max);
        var newSlider = Instantiate(SliderPrefab);
        var textComponent = newSlider.transform.Find("Text").gameObject.GetComponent<Text>();
        var sliderComponent = newSlider.GetComponentInChildren<Slider>();
        var inputComponent = newSlider.GetComponentInChildren<InputField>();
        if (property.FieldType.ToString() == "System.Int32")
            inputComponent.contentType = InputField.ContentType.IntegerNumber;
        if (property.FieldType.ToString() == "System.Single")
            inputComponent.contentType = InputField.ContentType.DecimalNumber;

        inputComponent.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            if (property.FieldType.ToString() == "System.Int32")
            {
                var result = int.Parse(value, CultureInfo.InvariantCulture);
                list.Add(result);
            }   
            if (property.FieldType.ToString() == "System.Single")
            {
                var result = float.Parse(value.ToString(), CultureInfo.InvariantCulture);
                list.Add(result);
            }

            target.setFieldProp(property, list);
        });

        textComponent.text = property.Name;
        var tmp = "" + property.GetValue(target);
        tmp = tmp.Replace(",", ".");
        inputComponent.text = "" + tmp;
        inputComponent.transform.Find("Text").gameObject.GetComponent<Text>().color = Color.white;
        newSlider.transform.SetParent(parent);

        // Debug.Log("Value of " + property.Name + " is " + float.Parse(property.GetValue(target).ToString()));

        sliderComponent.maxValue = rangeAttribut.max;
        sliderComponent.minValue = rangeAttribut.min;
        sliderComponent.interactable = isInteractible;
        sliderComponent.wholeNumbers = !isFloat;

        sliderComponent.onValueChanged.AddListener((value) =>
        {
            //if (isFloat)
            //else
            //    textComponent.text = property.Name;
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, list);
            inputComponent.text = property.GetValue(target).ToString();
        });
        target.controllableValueChanged += (name) =>
        {
            //Debug.Log("Fired value changed : " + name + " slider value : " + (float)target.getPropInfoForAddress(name).GetValue(target));
            if (name == property.Name)
            {
                if (isFloat)
                {
                    sliderComponent.value =
                        (float) target.getPropInfoForAddress(name).GetValue(target);
                    var str = "" + property.GetValue(target);
                    str = str.Replace(",", ".");
                    inputComponent.text = "" + str;
                }
                else
                {
                    sliderComponent.value = (int) target.getPropInfoForAddress(name).GetValue(target);
                    inputComponent.text = sliderComponent.value.ToString();
                }
            }
        };
        if (isFloat)
            sliderComponent.value = float.Parse(property.GetValue(target).ToString());
        else
            sliderComponent.value = (int)property.GetValue(target);
        newSlider.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateInput(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newInput = Instantiate(InputPrefab);

        var textComponent = newInput.transform.GetChild(1).gameObject.GetComponent<Text>();

        textComponent.text = property.Name;
        newInput.transform.SetParent(parent);
        newInput.transform.GetComponentInChildren<InputField>().interactable = isInteractible;

        if (property.FieldType.ToString() == "System.Int32")
            newInput.transform.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
        if (property.FieldType.ToString() == "System.Single")
            newInput.transform.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
        if (property.FieldType.ToString() == "System.String")
            newInput.transform.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Standard;

        newInput.GetComponentInChildren<InputField>().text = property.GetValue(target).ToString();
        newInput.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            var propertyType = property.FieldType;
            if (showDebug)
            {
                Debug.Log("Property type : " + propertyType.ToString());
                Debug.Log("Value : " + value + " size : " + value.Length);
            }
            if (propertyType.ToString() == "System.Int32")
            {
                var result = 0;
                try { result = int.Parse(value, CultureInfo.InvariantCulture); }
                catch(Exception e) { result = 0; }
                list.Add(result);
            }
            if (propertyType.ToString() == "System.Single")
            {
                var result = 0.0f;
                try { result = float.Parse(value.ToString(), CultureInfo.InvariantCulture); }
                catch (Exception e) { result = 0.0f; }
                list.Add(result);
            }
            if (propertyType.ToString() == "System.String")
                list.Add(value);

            target.setFieldProp(property, list);
        });
        
        target.controllableValueChanged += (name) =>
        {
            
            if (name == property.Name)
            {
                // Debug.Log("Value " + name + " changed ");
                //Specific to the prefab architecture
                var str = "" + property.GetValue(target);
                str = str.Replace(",", ".");
                newInput.transform.GetChild(0).GetComponent<InputField>().text =
                    "" + str; //Convert.ChangeType(target.getPropInfoForAddress(name).GetValue(target), property.FieldType))ype);
            }
        };

        newInput.transform.GetChild(0).Find("Text").gameObject.GetComponent<Text>().color = Color.white;
        newInput.transform.GetChild(0).Find("Placeholder").gameObject.GetComponent<Text>().text = target.getPropInfoForAddress(property.Name).GetValue(target).ToString();
        newInput.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateCheckbox(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newCheckbox = Instantiate(CheckboxPrefab);
        newCheckbox.GetComponentInChildren<Text>().text = property.Name;
        newCheckbox.transform.SetParent(parent);
        newCheckbox.GetComponent<Toggle>().isOn = (bool) property.GetValue(target);
        newCheckbox.GetComponent<Toggle>().interactable = isInteractible;
        newCheckbox.GetComponent<Toggle>().onValueChanged.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, list);
        });
        target.controllableValueChanged += (name) =>
        {
            //Debug.Log("Fired value changed : " + name);
            if (name == property.Name) {
                var newValue = (bool)target.getPropInfoForAddress(name).GetValue(target);
                newCheckbox.GetComponent<Toggle>().isOn = newValue;
                if (newValue) { //GREEN
                    var blockColors = newCheckbox.GetComponent<Toggle>().colors;
                    blockColors.disabledColor = new Color(0.43f, 0.9f, 0.47f, 0.75f);
                    newCheckbox.GetComponent<Toggle>().colors = blockColors;
                }
                else //RED
                {
                    var blockColors = newCheckbox.GetComponent<Toggle>().colors;
                    blockColors.disabledColor = new Color(0.9f, 0.4f, 0.4f, 0.8f);
                    newCheckbox.GetComponent<Toggle>().colors = blockColors;
                }
            }
        };
        newCheckbox.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateButton(Transform parent, Controllable target, MethodInfo method)
    {
        //As we can't expose parameter in UI, ignore methods with arguments 
        if (method.GetParameters().Length == 0)
        {
            var newButton = Instantiate(MethodButtonPrefab);
            newButton.transform.SetParent(parent);
            newButton.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
            newButton.GetComponentInChildren<Text>().text = method.Name;
            newButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                target.setMethodProp(method, method.Name, null);
            });
        }
        else
        {
            foreach (var parameter in method.GetParameters())
            {
                //Will do cool stuff in the future
            }
        }
    }

    private void CreateColor(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newColor = Instantiate(ColorPrefab);
        newColor.GetComponentInChildren<Text>().text = property.Name;
        newColor.transform.SetParent(parent);
        newColor.GetComponentInChildren<Image>().color = (Color)property.GetValue(target);

        //newColor.GetComponent<Toggle>().onValueChanged.AddListener((value) =>
        //{
        //    var list = new List<object>();
        //    list.Add(value);
        //    target.setFieldProp(property, list);
        //});
        target.controllableValueChanged += (name) =>
        {
            //Debug.Log("Fired value changed : " + name);
            if (name == property.Name)
            {
                newColor.GetComponentInChildren<Image>().color = (Color)property.GetValue(target);
            }
        };
        newColor.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateVector3(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newVector3 = Instantiate(Vector3Prefab);
        newVector3.transform.SetParent(parent);
        newVector3.transform.GetChild(1).GetComponent<Text>().text = property.Name;
        var XInput = newVector3.transform.GetChild(0).Find("XInput").GetChild(0).GetComponent<InputField>();
        var YInput = newVector3.transform.GetChild(0).Find("YInput").GetChild(0).GetComponent<InputField>();
        var ZInput = newVector3.transform.GetChild(0).Find("ZInput").GetChild(0).GetComponent<InputField>();

        XInput.contentType = InputField.ContentType.DecimalNumber;
        YInput.contentType = InputField.ContentType.DecimalNumber;
        ZInput.contentType = InputField.ContentType.DecimalNumber;

        var scriptValue = (Vector3)property.GetValue(target);
        XInput.text = "" + scriptValue.x;
        YInput.text = "" + scriptValue.y;
        ZInput.text = "" + scriptValue.z;

        XInput.onEndEdit.AddListener((value) =>
        {
            //if (showDebug)
            //    Debug.Log("[GenUI] UI change X : " + float.Parse(value.ToString(), CultureInfo.InvariantCulture));

            var list = new List<object>();
            list.Add(float.Parse(value.ToString(), CultureInfo.InvariantCulture));
            list.Add(float.Parse(YInput.text.ToString(), CultureInfo.InvariantCulture));
            list.Add(float.Parse(ZInput.text.ToString(), CultureInfo.InvariantCulture));

            target.setFieldProp(property, list);
        });

        YInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(float.Parse(XInput.text.ToString(), CultureInfo.InvariantCulture));
            list.Add(float.Parse(value.ToString(), CultureInfo.InvariantCulture));
            list.Add(float.Parse(ZInput.text.ToString(), CultureInfo.InvariantCulture));

            target.setFieldProp(property, list);
        });

        ZInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(float.Parse(XInput.text.ToString(), CultureInfo.InvariantCulture));
            list.Add(float.Parse(YInput.text.ToString(), CultureInfo.InvariantCulture));
            list.Add(float.Parse(value.ToString(), CultureInfo.InvariantCulture));

            target.setFieldProp(property, list);
        });

        target.controllableValueChanged += (name) =>
        {
            if (name == property.Name)
            {
                //if (showDebug)
                //    Debug.Log("[GenUI] Value changed X : " + ((Vector3)property.GetValue(target)).x);

                var vector = (Vector3)property.GetValue(target);

                XInput.text = "" + vector.x;
                YInput.text = "" + vector.y;
                ZInput.text = "" + vector.z;
            }
        };
    }

    public void ClickOnDropdown()
    {
        ControllableMaster.RefreshAllPresets();
    }
}