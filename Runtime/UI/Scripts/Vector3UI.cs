using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class Vector3UI : ControllableUI
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

        XInput.contentType = InputField.ContentType.DecimalNumber;
        YInput.contentType = InputField.ContentType.DecimalNumber;
        ZInput.contentType = InputField.ContentType.DecimalNumber;

        var scriptValue = (Vector3)property.GetValue(target);

        var str = scriptValue.x.ToString();
        str = str.Replace(",", ".");
        XInput.text = "" + str;

        str = scriptValue.y.ToString();
        str = str.Replace(",", ".");
        YInput.text = "" + str;

        str = scriptValue.z.ToString();
        str = str.Replace(",", ".");
        ZInput.text = "" + str;

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

		this.transform.GetChild(0).Find("XInput").GetChild(0).GetComponent<InputField>().text = vector.x.ToString().Replace(",", ".");
		this.transform.GetChild(0).Find("YInput").GetChild(0).GetComponent<InputField>().text = vector.y.ToString().Replace(",", ".");
		this.transform.GetChild(0).Find("ZInput").GetChild(0).GetComponent<InputField>().text = vector.z.ToString().Replace(",", ".");
	}
}
