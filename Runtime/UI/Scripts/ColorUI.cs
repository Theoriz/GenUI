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

    Text _label;
    Image _swatch;

    #region Widget

    protected override void BuildHierarchy()
    {
        _label = UIFactory.CreateLabel(transform);

        var swatchRect = UIFactory.CreateSlice("Swatch", transform, GenUIStyle.LabelWidthRatio, 1f);
        _swatch = UIFactory.AddImage(swatchRect.gameObject, GenUIAssets.Instance.Box, Color.white);

        //The swatch is the control of this row, so it is the only part that opens the picker - the
        //label and the gap beside it behave like every other row's, right click only. It is drawn in
        //front of the row's own graphic, so it takes the press before the row does.
        UIFactory.AddMouseEvent(swatchRect.gameObject, this, true);
    }

    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;

        _label.text = ParseNameString(property.Name);
        _swatch.color = GetCurrentColorValue();
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        _swatch.color = GetCurrentColorValue();
    }

    #endregion

    #region Colour picker

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

        RecordUndo(new UndoStack.Value(new List<object> { _colorBeforePicker }));
    }

    public void OnColorPickerUpdated(Color newColor)
    {
        var list = new List<object>();
        list.Add(newColor);
        LinkedControllable.SetFieldProp(Property, list);
    }

    public Color GetCurrentColorValue()
    {
        return (Color)Property.GetValue(LinkedControllable);
    }

    #endregion
}
