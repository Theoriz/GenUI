﻿using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class UIMaster : MonoBehaviour
{
    public Transform Panel;
    public GameObject PanelPrefab;
    public GameObject MethodButtonPrefab;
    public GameObject SliderPrefab;
    public GameObject CheckboxPrefab;
    public GameObject InputPrefab;
    public GameObject DropdownPrefab;

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
        var newPanel = Instantiate(PanelPrefab);
        newPanel.transform.SetParent(Panel.transform);
        newPanel.GetComponentInChildren<Text>().text = newControllable.id;
        newPanel.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        _panels.Add(newControllable.id, newPanel);

        //Read all methods and add button
        foreach (var method in newControllable.Methods)
        {
            CreateButton(newPanel.transform, newControllable, method.Value);
        }

        //Read all properties and add associated UI
        foreach (var property in newControllable.Properties)
        {
            var propertyType = property.Value.FieldType;
            OSCProperty attribute = Attribute.GetCustomAttribute(property.Value, typeof(OSCProperty)) as OSCProperty;

            //Check if needs to be in UI
            if (!attribute.ShowInUI) continue;

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
            //Debug.Log("Property type : " + propertyType + " of " + property.Key);
        }

        //Order Save and Load preset buttons
        var allText = newPanel.GetComponentsInChildren<Text>();
        foreach (var text in allText)
        {
            if (text.text == "OverwritePreset" || text.text == "SavePresetAs" || text.text == "LoadPreset")
                text.transform.parent.SetSiblingIndex(newPanel.transform.childCount-2); //last index being the preset list
        }

    }

    private void CreateDropDown(Transform parent, Controllable target, FieldInfo listProperty, FieldInfo activeElement)
    {
        var newDropdown = Instantiate(DropdownPrefab);
        newDropdown.transform.SetParent(parent);

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
        
        target.valueChanged += (name) =>
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

        textComponent.text = property.Name + " : " + 0;
        newSlider.transform.SetParent(parent);

        // Debug.Log("Value of " + property.Name + " is " + float.Parse(property.GetValue(target).ToString()));

        sliderComponent.maxValue = rangeAttribut.max;
        sliderComponent.minValue = rangeAttribut.min;
        sliderComponent.interactable = isInteractible;
        sliderComponent.wholeNumbers = !isFloat;

        sliderComponent.onValueChanged.AddListener((value) =>
        {
            if (isFloat)
                textComponent.text = property.Name + " : " + value.ToString("F2");
            else
                textComponent.text = property.Name + " : " + value;
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, list);
        });
        target.valueChanged += (name) =>
        {
           // Debug.Log("Fired value changed : " + name);
            if (name == property.Name)
            {
                if (isFloat)
                {
                    sliderComponent.value =
                        (float) target.getPropInfoForAddress(name).GetValue(target);
                    textComponent.text =
                        property.Name + " : " + sliderComponent.value.ToString("F2");
                }
                else
                {
                    sliderComponent.value = (int) target.getPropInfoForAddress(name).GetValue(target);
                    textComponent.text =
                        property.Name + " : " + sliderComponent.value;
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
            var setVal = value;
            var propertyType = property.FieldType;
            //Debug.Log("Property type : " + propertyType.ToString());
            if (propertyType.ToString() == "System.Int32")
                list.Add(int.Parse(value));
            if (propertyType.ToString() == "System.Single")
            {
                Debug.Log("Value : " + value + " size : " + value.Length);
                list.Add(float.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
            }
            if (propertyType.ToString() == "System.String")
                list.Add(value);

            target.setFieldProp(property, list);
        });
        
        target.valueChanged += (name) =>
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
        newInput.transform.GetChild(0).Find("Placeholder").gameObject.GetComponent<Text>().color = Color.white;
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
        target.valueChanged += (name) =>
        {
            //Debug.Log("Fired value changed : " + name);
            if (name == property.Name)
                newCheckbox.GetComponent<Toggle>().isOn = (bool)target.getPropInfoForAddress(name).GetValue(target);
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
    
}