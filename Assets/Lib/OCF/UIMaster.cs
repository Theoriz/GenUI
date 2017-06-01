using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public class UIMaster : MonoBehaviour
{
    public ControllableMaster TheControllableMaster;

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
        CreateUICamera();
        CreateRootCanvas();

        TheControllableMaster.controllableAdded += CreateUI;
        TheControllableMaster.controllableRemoved += RemoveUI;
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
        newPanel.transform.SetParent(_rootCanvas.transform);
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
            if (propertyType.ToString() == "System.Single")
            {
                var rangeAttribut = (RangeAttribute[]) property.Value.GetCustomAttributes(typeof(RangeAttribute), false);
                if(rangeAttribut.Length == 0)
                    CreateInput(newControllable, property.Value);
                else
                    CreateSlider(newControllable, property.Value, rangeAttribut[0]);
            }
            if (propertyType.ToString() == "System.Boolean")
            {
                CreateCheckbox(newControllable, property.Value);
            }
            if (propertyType.ToString() == "System.Int32" || propertyType.ToString() == "System.Float")
            {
                CreateInput(newControllable, property.Value);
            }

            if (propertyType.ToString() == "System.Collections.Generic.List`1[System.String]")
            {
                CreateDropDown(newControllable, property.Value);
            }
            //Debug.Log("Property type : " + propertyType + " of " + property.Key);
        }

        //black magic to make added UI visible
        _rootCanvas.GetComponent<Canvas>().planeDistance += 1;
    }

    private void CreateDropDown(Controllable target, FieldInfo property)
    {
        var newDropdown = Instantiate(DropdownPrefab);
        newDropdown.transform.SetParent(_panels.Last().Value.transform);
        newDropdown.GetComponent<Dropdown>().AddOptions((List<string>)target.getPropInfoForAddress(property.Name).GetValue(target));
        newDropdown.GetComponent<Dropdown>().onValueChanged.AddListener((value) =>
        {
            var currentList = (List<string>) target.getPropInfoForAddress(property.Name).GetValue(target);
            var selected = currentList[value];
            currentList.Add(selected);
            currentList.RemoveAt(value);
            newDropdown.GetComponent<Dropdown>().value = currentList.Count - 1;
            List<object> objParams = currentList.ConvertAll(s => (object)s);
            target.setFieldProp(property, property.Name, objParams);
        });
        
        target.valueChanged += (name) =>
        {
           // Debug.Log("Fired value changed : " + name + " and I am " + property.Name);
            if (name == property.Name)
            {
                newDropdown.GetComponent<Dropdown>().ClearOptions();
                newDropdown.GetComponent<Dropdown>().AddOptions((List<string>)target.getPropInfoForAddress(property.Name).GetValue(target));
            }
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
                if(property.FieldType.ToString() == "System.Single")
                    newInput.transform.GetChild(0).Find("Placeholder").gameObject.GetComponent<Text>().text = "" + 
                    (float) target.getPropInfoForAddress(name).GetValue(target);
                else
                {
                    newInput.transform.GetChild(0).Find("Placeholder").gameObject.GetComponent<Text>().text = "" +
                     (int)target.getPropInfoForAddress(name).GetValue(target);
                }
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

    private void CreateRootCanvas()
    {
        _rootCanvas = new GameObject();
        _rootCanvas.name = "UICanvas";
        _rootCanvas.transform.parent = _camera.transform;
        _rootCanvas.layer = 5;
        var canvasComponent = _rootCanvas.AddComponent<Canvas>();
        _rootCanvas.AddComponent<VerticalLayoutGroup>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceCamera;
        canvasComponent.worldCamera = _camera.GetComponent<Camera>();

        _rootCanvas.AddComponent<GraphicRaycaster>();
    }

    private void CreateUICamera()
    {
        _camera = new GameObject();
        _camera.name = "UICamera";
        var cameraComponent = _camera.AddComponent<Camera>();
        //camera settings
        cameraComponent.backgroundColor = Color.black;
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.orthographic = true;
        cameraComponent.nearClipPlane = 0.0f;
        cameraComponent.cullingMask = (1 << 5);
        cameraComponent.backgroundColor = Color.white;
        //Position in display
        var newRect = cameraComponent.rect;
        newRect.x = -0.7f;
        cameraComponent.rect = newRect;
    }
}