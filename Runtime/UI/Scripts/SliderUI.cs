using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class SliderUI : ControllableUI
{
    public bool IsFloat;

    // True while the UI is being refreshed from the target value; the slider/input callbacks
    // ignore writes made during that window instead of pushing them back to the Controllable.
    private bool _updating;

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible, bool isFloat = true) {

        Property = property;
        IsFloat = isFloat;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        var textComponent = this.transform.Find("Text").gameObject.GetComponent<Text>();
        var sliderComponent = this.GetComponentInChildren<Slider>();
        var inputComponent = this.GetComponentInChildren<InputField>();

        if (IsFloat)
            inputComponent.contentType = InputField.ContentType.DecimalNumber;
        else
            inputComponent.contentType = InputField.ContentType.IntegerNumber;

        inputComponent.onEndEdit.AddListener((value) =>
        {
            if (_updating)
                return;

            var list = new List<object>();
            if (!IsFloat)
                list.Add(Mathf.Clamp(TypeConverter.getInt(value), (int)rangeAttribut.min, (int)rangeAttribut.max));
            else
                list.Add(Mathf.Clamp(TypeConverter.getFloat(value), rangeAttribut.min, rangeAttribut.max));

            target.setFieldProp(property, list);
        });

        textComponent.text = ParseNameString(property.Name);
        inputComponent.text = FormatValue(property.GetValue(target));
        inputComponent.transform.Find("Text").gameObject.GetComponent<Text>().color = Color.white;

        sliderComponent.maxValue = rangeAttribut.max;
        sliderComponent.minValue = rangeAttribut.min;
        sliderComponent.interactable = isInteractible;
        sliderComponent.wholeNumbers = !isFloat;

        sliderComponent.onValueChanged.AddListener((value) =>
        {
            if (_updating)
                return;

            var list = new List<object>();
            list.Add(value);
            LinkedControllable.setFieldProp(property, list);
            inputComponent.text = FormatValue(property.GetValue(target));
        });


        if (isFloat)
            sliderComponent.value = TypeConverter.getFloat(property.GetValue(target));
        else
            sliderComponent.value = TypeConverter.getInt(property.GetValue(target));
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        if (_updating)
            return;

        _updating = true;
        try
        {
            var value = Property.GetValue(LinkedControllable);

            this.GetComponentInChildren<Slider>().value =
                IsFloat ? TypeConverter.getFloat(value) : TypeConverter.getInt(value);
            this.GetComponentInChildren<InputField>().text = FormatValue(value);
        }
        finally
        {
            _updating = false;
        }
    }

    static string FormatValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
