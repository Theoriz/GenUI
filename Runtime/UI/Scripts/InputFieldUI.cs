using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class InputFieldUI : ControllableUI
{
    Text _label;
    InputField _input;

    protected override void BuildHierarchy()
    {
        _label = UIFactory.CreateLabel(transform);

        var fieldRect = UIFactory.CreateSlice("InputField", transform, GenUIStyle.LabelWidthRatio, 1f);
        _input = UIFactory.AddInputField(fieldRect);
        UIFactory.AddMouseEvent(fieldRect.gameObject, this);
    }

    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible) {

        LinkedControllable = target;
        IsInteractible = isInteractible;
        Property = property;
        target.controllableValueChanged += HandleTargetChange;

        _label.text = ParseNameString(property.Name);

        if (property.FieldType.ToString() == "System.Int32")
            _input.contentType = InputField.ContentType.IntegerNumber;
        if (property.FieldType.ToString() == "System.Single")
            _input.contentType = InputField.ContentType.DecimalNumber;
        if (property.FieldType.ToString() == "System.String")
            _input.contentType = InputField.ContentType.Standard;

        var str = "" + property.GetValue(target).ToString();
        str = str.Replace(",", ".");
        _input.text = "" + str;

        _input.onEndEdit.AddListener((value) =>
        {
            RecordUndo();

            var list = new List<object>();
            var propertyType = property.FieldType;
            if (ShowDebug)
            {
                Debug.Log("Property type : " + propertyType.ToString());
                Debug.Log("Value : " + value + " size : " + value.Length);
            }
            if (propertyType.ToString() == "System.Int32")
            {
                var result = 0;
                try { result = int.Parse(value, CultureInfo.InvariantCulture);}
                catch (Exception e) { Debug.Log(e.Message); result = 0; }
                list.Add(result);
            }
            else if (propertyType.ToString() == "System.Single")
            {
                var result = 0.0f;
                try { result = float.Parse(value.ToString(), CultureInfo.InvariantCulture); }
                catch (Exception e) { Debug.Log(e.Message); result = 0.0f; }
                list.Add(result);
            }
            else if (propertyType.ToString() == "System.String")
                list.Add(value);

            target.SetFieldProp(property, list);
        });

        //The value the member started with, shown behind an emptied field.
        ((Text)_input.placeholder).text = target.GetPropInfoForAddress(property.Name).GetValue(target).ToString();

        ApplyReadOnlyLook();
    }

    public override InputField[] GetInputFields()
    {
        return new[] { _input };
    }

    //Only the int and float widgets scrub; the string one has nothing to scrub to.
    public override ScrubTarget[] GetScrubTargets()
    {
        if (_input.contentType == InputField.ContentType.Standard)
            return base.GetScrubTargets();

        return new[] { new ScrubTarget(_input, _label) };
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var str = "" + Property.GetValue(LinkedControllable);
        _input.text = str.Replace(",", ".");
    }
}
