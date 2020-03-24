using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;

public class DropdownUI : ControllableUI
{
    public FieldInfo ListProperty;
    public Type enumType = null;

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo listProperty, FieldInfo activeElement) {

        ListProperty = listProperty;
        Property = activeElement;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        var listInObject = (List<string>)ListProperty.GetValue(LinkedControllable);
        var activeElementIndex = TypeConverter.getIndexInEnum(listInObject, Property.GetValue(LinkedControllable).ToString());

        var text = this.transform.GetChild(0).GetComponent<Text>();
        text.text = ParseNameString(activeElement.Name);

        var dropdown = this.GetComponentInChildren<Dropdown>();
        dropdown.value = 0;
        dropdown.AddOptions(listInObject);
        dropdown.onValueChanged.AddListener((value) =>
        {
            var associatedList = (List<string>)ListProperty.GetValue(LinkedControllable);
            string activeItem = associatedList[value];

            List<object> objParams = new List<object> { activeItem };
            LinkedControllable.setFieldProp(Property, objParams);
        });
    }

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo activeElement, string _enumName)
    {
        Property = activeElement;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        enumType = Type.GetType(_enumName);
        if (enumType == null)
            Debug.LogError("Can't find Enum " + _enumName + ", if GenUI is in Plugin folder move it outside from it.");

        var text = this.transform.GetChild(0).GetComponent<Text>();
        text.text = ParseNameString(activeElement.Name);

        var dropdown = this.GetComponentInChildren<Dropdown>();
        dropdown.value = 0;
        dropdown.AddOptions(Enum.GetNames(enumType).ToList());
        dropdown.onValueChanged.AddListener((value) =>
        {
            List<object> objParams = new List<object> { Enum.GetNames(enumType)[value] };
            LinkedControllable.setFieldProp(Property, objParams, true);
        });
    }

    public override void HandleTargetChange(string name)
    {
        if(enumType != null) //New real enum handling
        {
            var dropdown = this.GetComponentInChildren<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(Enum.GetNames(enumType).ToList());
            dropdown.value = TypeConverter.getIndexInEnum(Enum.GetNames(enumType).ToList(), Property.GetValue(LinkedControllable).ToString());
        }
        else
        {
            var dropdown = this.GetComponentInChildren<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions((List<string>)ListProperty.GetValue(LinkedControllable));
            dropdown.value = TypeConverter.getIndexInEnum((List<string>)ListProperty.GetValue(LinkedControllable), Property.GetValue(LinkedControllable).ToString());
        }
    }

}
