using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class ToggleUI : ControllableUI
{
    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;
        HandleTargetChange(property.Name); //To set color

        this.GetComponentInChildren<Text>().text = ParseNameString(property.Name);
        this.GetComponent<Toggle>().isOn = (bool)property.GetValue(target);
        this.GetComponent<Toggle>().interactable = isInteractible;
        this.GetComponent<Toggle>().onValueChanged.AddListener((value) =>
        {
            var list = new List<object>();
            list.Add(value);
            target.setFieldProp(property, list);
        });
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var newValue = (bool)Property.GetValue(LinkedControllable);
        this.GetComponent<Toggle>().isOn = newValue;
        if (newValue)
        { //GREEN
            var blockColors = this.GetComponent<Toggle>().colors;
            blockColors.disabledColor = new Color(0.43f, 0.9f, 0.47f, 0.75f);
            this.GetComponent<Toggle>().colors = blockColors;
        }
        else //RED
        {
            var blockColors = this.GetComponent<Toggle>().colors;
            blockColors.disabledColor = new Color(0.9f, 0.4f, 0.4f, 0.8f);
            this.GetComponent<Toggle>().colors = blockColors;
        }
    }
}
