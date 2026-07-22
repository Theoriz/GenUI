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
            RecordUndo();

            var list = new List<object>();
            list.Add(TypeConverter.GetFloat(value.ToString()));
            list.Add(TypeConverter.GetFloat(YInput.text.ToString()));
            list.Add(TypeConverter.GetFloat(ZInput.text.ToString()));

            target.SetFieldProp(property, list);
        });

        YInput.onEndEdit.AddListener((value) =>
        {
            RecordUndo();

            var list = new List<object>();
            list.Add(TypeConverter.GetFloat(XInput.text.ToString()));
            list.Add(TypeConverter.GetFloat(value.ToString()));
            list.Add(TypeConverter.GetFloat(ZInput.text.ToString()));

            target.SetFieldProp(property, list);
        });

        ZInput.onEndEdit.AddListener((value) =>
        {
            RecordUndo();

            var list = new List<object>();
            list.Add(TypeConverter.GetFloat(XInput.text.ToString()));
            list.Add(TypeConverter.GetFloat(YInput.text.ToString()));
            list.Add(TypeConverter.GetFloat(value.ToString()));

            target.SetFieldProp(property, list);
        });
    }

    //Named rather than indexed, so Tab visits x, y, z whatever order the prefab holds them in.
    public override InputField[] GetInputFields()
    {
        return new[] { FindInput("XInput"), FindInput("YInput"), FindInput("ZInput") };
    }

    InputField FindInput(string childName)
    {
        return this.transform.GetChild(0).Find(childName).GetChild(0).GetComponent<InputField>();
    }

    //Each axis carries its own label beside the box, so a scrub knows which component it moves.
    public override ScrubTarget[] GetScrubTargets()
    {
        return new[] { MakeScrubTarget("XInput"), MakeScrubTarget("YInput"), MakeScrubTarget("ZInput") };
    }

    ScrubTarget MakeScrubTarget(string childName)
    {
        var axis = this.transform.GetChild(0).Find(childName);
        return new ScrubTarget(axis.GetChild(0).GetComponent<InputField>(),
                               axis.Find("Text").GetComponent<Text>());
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
