using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class Vector3UI : ControllableUI
{
    public FieldInfo Property;

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;

        this.transform.GetChild(1).GetComponent<Text>().text = property.Name;
        var XInput = this.transform.GetChild(0).Find("XInput").GetChild(0).GetComponent<InputField>();
        var YInput = this.transform.GetChild(0).Find("YInput").GetChild(0).GetComponent<InputField>();
        var ZInput = this.transform.GetChild(0).Find("ZInput").GetChild(0).GetComponent<InputField>();

        XInput.contentType = InputField.ContentType.DecimalNumber;
        YInput.contentType = InputField.ContentType.DecimalNumber;
        ZInput.contentType = InputField.ContentType.DecimalNumber;

        var scriptValue = (Vector3)property.GetValue(target);
        XInput.text = "" + scriptValue.x;
        YInput.text = "" + scriptValue.y;
        ZInput.text = "" + scriptValue.z;

        XInput.onEndEdit.AddListener((value) =>
        {
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
    }

    public override void HandleTargetChange(string name)
    { 
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var vector = (Vector3)Property.GetValue(LinkedControllable);

        this.transform.GetChild(0).Find("XInput").GetChild(0).GetComponent<InputField>().text = "" + vector.x;
        this.transform.GetChild(0).Find("YInput").GetChild(0).GetComponent<InputField>().text = "" + vector.y;
        this.transform.GetChild(0).Find("ZInput").GetChild(0).GetComponent<InputField>().text = "" + vector.z;
    }
}
