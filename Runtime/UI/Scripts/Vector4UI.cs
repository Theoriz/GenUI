using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class Vector4UI : ControllableUI
{
    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;

        this.transform.GetChild(1).GetComponent<Text>().text = ParseNameString(property.Name);
        var XInput = this.transform.GetChild(0).Find("XInput").GetChild(0).GetComponent<InputField>();
        var YInput = this.transform.GetChild(0).Find("YInput").GetChild(0).GetComponent<InputField>();
        var ZInput = this.transform.GetChild(0).Find("ZInput").GetChild(0).GetComponent<InputField>();
        var WInput = this.transform.GetChild(0).Find("WInput").GetChild(0).GetComponent<InputField>();

        XInput.contentType = InputField.ContentType.DecimalNumber;
        YInput.contentType = InputField.ContentType.DecimalNumber;
        ZInput.contentType = InputField.ContentType.DecimalNumber;
        WInput.contentType = InputField.ContentType.DecimalNumber;

        var scriptValue = (Vector4)property.GetValue(target);

        var str = scriptValue.x.ToString();
        str = str.Replace(",", ".");
        XInput.text = "" + str;

        str = scriptValue.y.ToString();
        str = str.Replace(",", ".");
        YInput.text = "" + str;

        str = scriptValue.z.ToString();
        str = str.Replace(",", ".");
        ZInput.text = "" + str;

        str = scriptValue.w.ToString();
        str = str.Replace(",", ".");
        WInput.text = "" + str;

        XInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(TypeConverter.getFloat(value.ToString()));
            list.Add(TypeConverter.getFloat(YInput.text.ToString()));
            list.Add(TypeConverter.getFloat(ZInput.text.ToString()));
            list.Add(TypeConverter.getFloat(WInput.text.ToString()));

            target.setFieldProp(property, list);
        });

        YInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(TypeConverter.getFloat(XInput.text.ToString()));
            list.Add(TypeConverter.getFloat(value.ToString()));
            list.Add(TypeConverter.getFloat(ZInput.text.ToString()));
            list.Add(TypeConverter.getFloat(WInput.text.ToString()));

            target.setFieldProp(property, list);
        });

        ZInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(TypeConverter.getFloat(XInput.text.ToString()));
            list.Add(TypeConverter.getFloat(YInput.text.ToString()));
            list.Add(TypeConverter.getFloat(value.ToString()));
            list.Add(TypeConverter.getFloat(WInput.text.ToString()));

            target.setFieldProp(property, list);
        });

        WInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(TypeConverter.getFloat(XInput.text.ToString()));
            list.Add(TypeConverter.getFloat(YInput.text.ToString()));
            list.Add(TypeConverter.getFloat(ZInput.text.ToString()));
            list.Add(TypeConverter.getFloat(value.ToString()));

            target.setFieldProp(property, list);
        });
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var vector = (Vector4)Property.GetValue(LinkedControllable);

        this.transform.GetChild(0).Find("XInput").GetChild(0).GetComponent<InputField>().text = vector.x.ToString().Replace(",", ".");
        this.transform.GetChild(0).Find("YInput").GetChild(0).GetComponent<InputField>().text = vector.y.ToString().Replace(",", ".");
        this.transform.GetChild(0).Find("ZInput").GetChild(0).GetComponent<InputField>().text = vector.z.ToString().Replace(",", ".");
        this.transform.GetChild(0).Find("WInput").GetChild(0).GetComponent<InputField>().text = vector.w.ToString().Replace(",", ".");
    }
}
