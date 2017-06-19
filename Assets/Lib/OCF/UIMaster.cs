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

    public void RemoveUI(Controllable dyingControllable)
    {
        Destroy(_panels[dyingControllable.id]);
        _panels.Remove(dyingControllable.name);
    }

    public void CreateUI(Controllable newControllable)
    {

        //First we create a panel for the controllable
        var newPanel = Instantiate(PanelPrefab);
        newPanel.transform.SetParent(Panel.transform);
        newPanel.GetComponentInChildren<Text>().text = newControllable.id;
        newPanel.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        _panels.Add(newControllable.id, newPanel);

        //Read all methods and add button
        foreach (var method in newControllable.Methods)
        {
            CreateButton(newControllable, method.Value);
        }

        //Read all properties and add associated UI
        foreach (var property in newControllable.Properties)
        {
            var propertyType = property.Value.FieldType;
            OSCProperty attribute = Attribute.GetCustomAttribute(property.Value, typeof(OSCProperty)) as OSCProperty;
            if (attribute.TargetList != "" && attribute.TargetList != null)
            {
                var associatedListFieldInfo = newControllable.getFieldInfoByName(attribute.TargetList);
                CreateDropDown(newControllable, associatedListFieldInfo, property.Value);
                continue;
            }
            //property.Value.Attributes O
            if (propertyType.ToString() == "System.Single")
            {
                var rangeAttribut = (RangeAttribute[]) property.Value.GetCustomAttributes(typeof(RangeAttribute), false);
                if(rangeAttribut.Length == 0)
                    CreateInput(newControllable, property.Value);
                else
                    CreateSlider(newControllable, property.Value, rangeAttribut[0]);
                continue;
            }
            if (propertyType.ToString() == "System.Boolean")
            {
                CreateCheckbox(newControllable, property.Value);
                continue;
            }
            if (propertyType.ToString() == "System.Int32" || propertyType.ToString() == "System.Float" || propertyType.ToString() == "System.String")
            {
                CreateInput(newControllable, property.Value);
                continue;
            }
            //Debug.Log("Property type : " + propertyType + " of " + property.Key);
        }

    }

    private void CreateDropDown(Controllable target, FieldInfo listProperty, FieldInfo activeElement)
    {
        var newDropdown = Instantiate(DropdownPrefab);
        newDropdown.transform.SetParent(_panels.Last().Value.transform);
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
            Debug.Log("UI value changed  :" + value);
            var associatedList = (List<string>) listProperty.GetValue(target);
            string activeItem = associatedList[value];

            List<object> objParams = new List<object> {activeItem};

            target.setFieldProp(activeElement, activeElement.Name, objParams);
        });
        
        target.valueChanged += (name) =>
        {
            //Not proud of this code ..;
            var toAdd = new List<string>();
            foreach (var actualItem in (List<string>)listProperty.GetValue(target))
            {
                var isInList = false;
                var tempList = newDropdown.GetComponent<Dropdown>().options;
                foreach (var t in tempList)
                {
                    if (t.text == actualItem)
                        isInList = true;
                }
                if (!isInList)
                    toAdd.Add(actualItem);
            }
            
            Debug.Log(target.id + " UI has been updated");

            newDropdown.GetComponent<Dropdown>().AddOptions(toAdd);
        };
        newDropdown.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void CreateSlider(Controllable target, FieldInfo property, RangeAttribute rangeAttribut)
    {
        //Debug.Log("Range : " + rangeAttribut.min + ";" + rangeAttribut.max);
        var newSlider = Instantiate(SliderPrefab);
        newSlider.GetComponentInChildren<Text>().text = property.Name;
        newSlider.transform.SetParent(_panels.Last().Value.transform);
        newSlider.GetComponent<Slider>().maxValue = rangeAttribut.max;
        newSlider.GetComponent<Slider>().minValue = rangeAttribut.min;
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

    private void CreateInput(Controllable target, FieldInfo property)
    {
        var newInput = Instantiate(InputPrefab);
        newInput.GetComponent<Text>().text = property.Name;
        newInput.transform.SetParent(_panels.Last().Value.transform);
        newInput.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, property.Name, list);
        });
        target.valueChanged += (name) =>
        {
           // Debug.Log("Fired value changed : " + name);
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

    private void CreateCheckbox(Controllable target, FieldInfo property)
    {
        var newCheckbox = Instantiate(CheckboxPrefab);
        newCheckbox.GetComponentInChildren<Text>().text = property.Name;
        newCheckbox.transform.SetParent(_panels.Last().Value.transform);
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

    private void CreateButton(Controllable target, MethodInfo method)
    {
        //As we can't expose parameter in UI, ignore methods with arguments 
        if (method.GetParameters().Length == 0)
        {
            var newButton = Instantiate(MethodButtonPrefab);
            newButton.transform.SetParent(_panels.Last().Value.transform);
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