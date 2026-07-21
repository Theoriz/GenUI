using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;

public class DropdownUI : ControllableUI
{
    [System.NonSerialized] public FieldInfo ListProperty;
    [System.NonSerialized] public Type enumType = null;

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
        dropdown.AddOptions(listInObject);
        //SetValueWithoutNotify: only a genuine user selection should fire onValueChanged (which loads
        //the selected preset). Programmatic updates here and in HandleTargetChange must not.
        dropdown.SetValueWithoutNotify(Mathf.Max(0, activeElementIndex));
        dropdown.onValueChanged.AddListener((value) =>
        {
            RecordUndo();

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
        var enumNames = Enum.GetNames(enumType).ToList();
        dropdown.AddOptions(enumNames);
        dropdown.SetValueWithoutNotify(Mathf.Max(0, TypeConverter.getIndexInEnum(enumNames, Property.GetValue(LinkedControllable)?.ToString() ?? "")));
        dropdown.onValueChanged.AddListener((value) =>
        {
            RecordUndo();

            List<object> objParams = new List<object> { Enum.GetNames(enumType)[value] };
            LinkedControllable.setFieldProp(Property, objParams, true);
        });
    }

    //setFieldProp reads an enum value back as its name, so that is what the undo stack has to carry;
    //the list route stores a plain string and the default capture already fits it.
    public override UndoStack.Value CaptureValue()
    {
        if (enumType == null)
            return base.CaptureValue();

        var current = Property.GetValue(LinkedControllable);
        return new UndoStack.Value(new List<object> { current == null ? "" : current.ToString() }, true);
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        if (enumType != null) //New real enum handling
        {
            var dropdown = this.GetComponentInChildren<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(Enum.GetNames(enumType).ToList());
            dropdown.SetValueWithoutNotify(Mathf.Max(0, TypeConverter.getIndexInEnum(Enum.GetNames(enumType).ToList(), Property.GetValue(LinkedControllable).ToString())));
        }
        else
        {
            var dropdown = this.GetComponentInChildren<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions((List<string>)ListProperty.GetValue(LinkedControllable));
            dropdown.SetValueWithoutNotify(Mathf.Max(0, TypeConverter.getIndexInEnum((List<string>)ListProperty.GetValue(LinkedControllable), Property.GetValue(LinkedControllable).ToString())));
        }
    }

}
