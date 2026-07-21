using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Collections.Generic;

public class ColorUI : ControllableUI
{
    //The picker pushes a colour on every frame it changes, so undo is bracketed by the picker being
    //open instead: one pick is one undo, back to the colour from before it opened.
    private Color _colorBeforePicker;
    private bool _picking;

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

    public void BeginPickerEdit()
    {
        _colorBeforePicker = GetCurrentColorValue();
        _picking = true;
    }

    public void EndPickerEdit()
    {
        if (!_picking)
            return;

        _picking = false;

        //Opened and closed without touching the colour, so there is nothing to undo.
        if (GetCurrentColorValue() == _colorBeforePicker)
            return;

        RecordUndo(new UndoStack.Value(new List<object> { _colorBeforePicker }, false));
    }

    public void OnColorPickerUpdated(Color newColor)
    {
        var list = new List<object>();
        list.Add(newColor);
        LinkedControllable.setFieldProp(Property, list);
    }

    public Color GetCurrentColorValue()
    {
        return (Color)Property.GetValue(LinkedControllable);
    }
}
