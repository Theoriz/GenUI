using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class ColorUI : ControllableUI
{
    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;

        this.GetComponentInChildren<Text>().text = ParseNameString(property.Name);
        this.GetComponentInChildren<Image>().color = (Color)property.GetValue(target);
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        this.GetComponentInChildren<Image>().color = (Color)Property.GetValue(LinkedControllable);
    }
}
