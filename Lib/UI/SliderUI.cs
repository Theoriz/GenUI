using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class SliderUI : ControllableUI
{
    public FieldInfo Property;
    public bool IsFloat;

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible, bool isFloat = true) {

        Property = property;
        IsFloat = isFloat;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;

        var textComponent = this.transform.Find("Text").gameObject.GetComponent<Text>();
        var sliderComponent = this.GetComponentInChildren<Slider>();
        var inputComponent = this.GetComponentInChildren<InputField>();
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
                result = (int)Mathf.Clamp(result, rangeAttribut.min, rangeAttribut.max);
                HandleTargetChange("");
                list.Add(result);
            }
            if (property.FieldType.ToString() == "System.Single")
            {
                var result = float.Parse(value.ToString(), CultureInfo.InvariantCulture);
                result = Mathf.Clamp(result, rangeAttribut.min, rangeAttribut.max);
                HandleTargetChange("");
                list.Add(result);
            }

            target.setFieldProp(property, list);
        });

        textComponent.text = property.Name;
        var tmp = "" + property.GetValue(target);
        tmp = tmp.Replace(",", ".");
        inputComponent.text = "" + tmp;
        inputComponent.transform.Find("Text").gameObject.GetComponent<Text>().color = Color.white;

        sliderComponent.maxValue = rangeAttribut.max;
        sliderComponent.minValue = rangeAttribut.min;
        sliderComponent.interactable = isInteractible;
        sliderComponent.wholeNumbers = !isFloat;

        sliderComponent.onValueChanged.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, list);
            inputComponent.text = property.GetValue(target).ToString();
        });


        if (isFloat)
            sliderComponent.value = float.Parse(property.GetValue(target).ToString());
        else
            sliderComponent.value = (int)property.GetValue(target);
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        if (IsFloat)
        {
            this.GetComponentInChildren<Slider>().value =
                (float)Property.GetValue(LinkedControllable);
            var str = "" + Property.GetValue(LinkedControllable);
            str = str.Replace(",", ".");
            this.GetComponentInChildren<InputField>().text = "" + str;
        }
        else
        {
            this.GetComponentInChildren<Slider>().value = (int)Property.GetValue(LinkedControllable);
            this.GetComponentInChildren<InputField>().text = this.GetComponentInChildren<Slider>().value.ToString();
        }
    }
}
