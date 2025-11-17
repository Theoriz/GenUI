using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class ToggleUI : ControllableUI
{
    public Toggle toggle;

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;
        HandleTargetChange(property.Name); //To set color

        this.GetComponentInChildren<Text>().text = ParseNameString(property.Name);
        
        toggle.isOn = (bool)property.GetValue(target);
        toggle.interactable = isInteractible;
        toggle.onValueChanged.AddListener((value) =>
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
        toggle.isOn = newValue;
        if (newValue)
        { //GREEN
            var blockColors = toggle.colors;
            blockColors.disabledColor = new Color(0.43f, 0.9f, 0.47f, 0.75f);
            toggle.colors = blockColors;
        }
        else //RED
        {
            var blockColors = toggle.colors;
            blockColors.disabledColor = new Color(0.9f, 0.4f, 0.4f, 0.8f);
            toggle.colors = blockColors;
        }
    }
}
