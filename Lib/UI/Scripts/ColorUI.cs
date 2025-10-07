using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

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
        this.GetComponentInChildren<Image>().color = GetCurrentColorValue();
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        this.GetComponentInChildren<Image>().color = GetCurrentColorValue();
    }

    public void OnColorPickerUpdated(Color newColor)
    {
        var list = new List<object>();
        list.Add(newColor);
        LinkedControllable.setFieldProp(Property, list);
        HandleTargetChange("");
    }

    public Color GetCurrentColorValue()
    {
        return (Color)Property.GetValue(LinkedControllable);
    }
}
