using System;
using System.Collections;
using System.Collections.Generic;
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

    public bool showDebug;

    private GameObject _camera;
    private GameObject _rootCanvas;
    private Dictionary<string, GameObject> _panels;

    // Use this for initialization
    void Awake()
    {
        _panels = new Dictionary<string, GameObject>();

        ControllableMaster.controllableAdded += CreateUI;
        ControllableMaster.controllableRemoved += RemoveUI;
    }

    void Update()
    {
        if(Input.GetKeyDown("h"))
            transform.GetChild(0).gameObject.SetActive(!transform.GetChild(0).gameObject.activeSelf);
    }

    public void RemoveUI(Controllable dyingControllable)
    {
        if (!dyingControllable.usePanel) return;

        if (showDebug)
            Debug.Log("Removing UI for " + dyingControllable.name + "|" + dyingControllable.id);
        Destroy(_panels[dyingControllable.id]);
        _panels.Remove(dyingControllable.name);
    }

    public void CreateUI(Controllable newControllable)
    {
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
            if (propertyType.ToString() == "System.Single")
            {
                var rangeAttribut = (RangeAttribute[]) property.Value.GetCustomAttributes(typeof(RangeAttribute), false);
                if (rangeAttribut.Length == 0)
                    CreateInput(newPanel.transform, newControllable, property.Value, attribute.isInteractible);
                else
                    CreateSlider(newPanel.transform, newControllable, property.Value, rangeAttribut[0], attribute.isInteractible);
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
            if (text.text == "SavePreset" || text.text == "LoadPreset")
                text.transform.parent.SetSiblingIndex(newPanel.transform.childCount-2); //last index being the preset list
        }

    }

    private void CreateDropDown(Transform parent, Controllable target, FieldInfo listProperty, FieldInfo activeElement)
    {
        var newDropdown = Instantiate(DropdownPrefab);
        newDropdown.transform.SetParent(parent);
        var listToDisplay = new List<string>();

        //TODO remove string
        var listInObject = (List<string>) listProperty.GetValue(target);

        foreach (var item in listInObject)
        {
            listToDisplay.Add(item.ToString());
        }

        //Set value if element in list
        var linkedList = (List<string>) listProperty.GetValue(target);
        if (linkedList.Count > 0) { 
            string startValue = linkedList[newDropdown.GetComponent<Dropdown>().value-1];
            target.setFieldProp(activeElement, activeElement.Name, new List<object> {startValue});
        }

    newDropdown.GetComponent<Dropdown>().AddOptions(listToDisplay);
        newDropdown.GetComponent<Dropdown>().onValueChanged.AddListener((value) =>
        {
            //var currentList = (List<string>) target.getPropInfoForAddress(property.Name).GetValue(target);
            //var selected = currentList[value];
            //currentList.Add(selected);
            //currentList.RemoveAt(value);
            //newDropdown.GetComponent<Dropdown>().value = currentList.Count - 1;
            //Debug.Log("UI value changed  :" + value);
            var associatedList = (List<string>)listProperty.GetValue(target);
            string activeItem = associatedList[value];

            List<object> objParams = new List<object> { activeItem };

            target.setFieldProp(activeElement, activeElement.Name, objParams);
        });
        
        target.valueChanged += (name) =>
        {
            //Debug.Log(target.id + " UI has been updated");
            newDropdown.GetComponent<Dropdown>().ClearOptions();
            newDropdown.GetComponent<Dropdown>().AddOptions((List<string>) listProperty.GetValue(target));

            //switch string order to match index
            var options = newDropdown.GetComponent<Dropdown>().options;
            var actualIndex = newDropdown.GetComponent<Dropdown>().value;
            if (options.Count > 1)
            {
                var selectedElementInControllable = activeElement.GetValue(target);
                var temp = options[actualIndex];
                var replacementIndex = 0;

                for (var i = 0 ; i < options.Count ; i++)
                {
                    if (options[i].text == options[actualIndex].text)
                        replacementIndex = i;
                }

                options[actualIndex].text = (string)selectedElementInControllable;
                options[replacementIndex].text = temp.text;
            }

            //newDropdown.GetComponent<Dropdown>().value = listInControllable.IndexOf((string)activeElement.GetValue(target))-1;
        };
        newDropdown.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateSlider(Transform parent, Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible)
    {
        //Debug.Log("Range : " + rangeAttribut.min + ";" + rangeAttribut.max);
        var newSlider = Instantiate(SliderPrefab);
        newSlider.GetComponentInChildren<Text>().text = property.Name;
        newSlider.transform.SetParent(parent);
        newSlider.GetComponent<SliderValue>().name = property.Name;
        newSlider.GetComponent<Slider>().maxValue = rangeAttribut.max;
        newSlider.GetComponent<Slider>().minValue = rangeAttribut.min;
        newSlider.GetComponent<Slider>().interactable = isInteractible; 
        newSlider.GetComponent<Slider>().onValueChanged.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, property.Name, list);
        });
        target.valueChanged += (name) =>
        {
           // Debug.Log("Fired value changed : " + name);
            if (name == property.Name)
                newSlider.GetComponent<Slider>().value = (float)target.getPropInfoForAddress(name).GetValue(target);
        };
        newSlider.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateInput(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newInput = Instantiate(InputPrefab);
        newInput.GetComponent<Text>().text = property.Name;
        newInput.transform.SetParent(parent);
        newInput.transform.GetComponentInChildren<InputField>().interactable = isInteractible;
        newInput.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, property.Name, list);
        });
        
        target.valueChanged += (name) =>
        {
            if (name == property.Name)
            {
                //Specific to the prefab architecture
                    newInput.transform.GetChild(0).Find("Placeholder").gameObject.GetComponent<Text>().text = "" + 
                    Convert.ChangeType(target.getPropInfoForAddress(name).GetValue(target), property.FieldType);
            }
        };
        newInput.transform.GetChild(0).Find("Placeholder").gameObject.GetComponent<Text>().text = target.getPropInfoForAddress(property.Name).GetValue(target).ToString();
        newInput.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateCheckbox(Transform parent, Controllable target, FieldInfo property, bool isInteractible)
    {
        var newCheckbox = Instantiate(CheckboxPrefab);
        newCheckbox.GetComponentInChildren<Text>().text = property.Name;
        newCheckbox.transform.SetParent(parent);
        newCheckbox.GetComponent<Toggle>().interactable = isInteractible;
        newCheckbox.GetComponent<Toggle>().onValueChanged.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, property.Name, list);
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