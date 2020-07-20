using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class Vector2IntUI : ControllableUI
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

        XInput.contentType = InputField.ContentType.IntegerNumber;
        YInput.contentType = InputField.ContentType.IntegerNumber;

        var scriptValue = (Vector2Int)property.GetValue(target);

        XInput.text = scriptValue.x.ToString();
        YInput.text = scriptValue.y.ToString();

        XInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(int.Parse(value.ToString(), CultureInfo.InvariantCulture));
            list.Add(int.Parse(YInput.text.ToString(), CultureInfo.InvariantCulture));

            target.setFieldProp(property, list);
        });

        YInput.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(int.Parse(XInput.text.ToString(), CultureInfo.InvariantCulture));
            list.Add(int.Parse(value.ToString(), CultureInfo.InvariantCulture));

            target.setFieldProp(property, list);
        });
    }

    public override void HandleTargetChange(string name)
    { 
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var vector = (Vector2Int)Property.GetValue(LinkedControllable);

		this.transform.GetChild(0).Find("XInput").GetChild(0).GetComponent<InputField>().text = vector.x.ToString();
		this.transform.GetChild(0).Find("YInput").GetChild(0).GetComponent<InputField>().text = vector.y.ToString();
	}
}
