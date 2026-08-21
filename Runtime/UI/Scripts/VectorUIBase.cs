using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A row of numeric boxes, one per axis, each with its own letter beside it.
/// </summary>
/// <remarks>
/// The five vector widgets differ only in how many axes they have, whether those are whole numbers,
/// and which struct they unbox - so that is all a subclass declares. Everything else, including the
/// rule that editing one axis sends all of them, lives here.
/// </remarks>
public abstract class VectorUIBase : ControllableUI
{
    protected static readonly string[] XY = { "x", "y" };
    protected static readonly string[] XYZ = { "x", "y", "z" };
    protected static readonly string[] XYZW = { "x", "y", "z", "w" };

    /// <summary>The axis letters, in the order Tab visits them.</summary>
    protected abstract string[] AxisNames { get; }

    protected abstract bool IsInteger { get; }

    /// <summary>The member's current value, one already-formatted string per axis.</summary>
    protected abstract string[] ReadAxisValues();

    InputField[] _inputs;
    Text[] _axisLabels;
    Text _label;

    #region Widget

    protected override void BuildHierarchy()
    {
        var axes = AxisNames;
        _inputs = new InputField[axes.Length];
        _axisLabels = new Text[axes.Length];

        _label = UIFactory.CreateLabel(transform, "Text");

        //The cells share the control half evenly, which is what keeps a Vector4's boxes the same
        //width as each other however wide the panel is.
        var value = UIFactory.CreateSlice("Value", transform, GenUIStyle.LabelWidthRatio, 1f);
        UIFactory.AddHorizontalLayout(value.gameObject, GenUIStyle.AxisSpacing, expandHeight: true);

        var contentType = IsInteger ? InputField.ContentType.IntegerNumber : InputField.ContentType.DecimalNumber;

        for (var i = 0; i < axes.Length; i++)
        {
            var cell = UIFactory.CreateChild(axes[i].ToUpperInvariant() + "Input", value);

            //The letter takes exactly its glyph's width and the box takes the rest, so the two stay
            //together whatever the axis count makes the cell and whichever letter it is.
            UIFactory.AddHorizontalLayout(cell.gameObject, GenUIStyle.AxisLabelGap,
                expandWidth: false, expandHeight: true, alignment: TextAnchor.MiddleLeft);

            var letter = UIFactory.CreateChild("Text", cell);
            _axisLabels[i] = UIFactory.AddText(letter.gameObject, axes[i], GenUIStyle.LabelFontSize,
                TextAnchor.MiddleLeft, GenUIStyle.LabelColor);

            var fieldRect = UIFactory.CreateChild("InputField", cell);
            //Nothing else in the cell is flexible, so the box absorbs the whole remainder. It asks
            //for no width of its own: InputField reports the width of its current text as its
            //preferred width, so a long value - a scrubbed float, say - would otherwise widen its
            //cell and squeeze the other axes' boxes to nothing. Overriding that needs a priority
            //above the InputField's own, since equal priorities take the larger value.
            var element = UIFactory.AddLayoutElement(fieldRect.gameObject, preferredWidth: 0f, flexibleWidth: 1f);
            element.layoutPriority = 2;
            _inputs[i] = UIFactory.AddInputField(fieldRect, contentType);
            UIFactory.AddMouseEvent(fieldRect.gameObject, this);
        }
    }

    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;

        _label.text = ParseNameString(property.Name);

        var values = ReadAxisValues();
        for (var i = 0; i < _inputs.Length; i++)
        {
            _inputs[i].text = values[i];

            //Captured per iteration: the listener has to know which axis it belongs to when it runs.
            var axis = i;
            _inputs[i].onEndEdit.AddListener((edited) =>
            {
                RecordUndo();

                target.SetFieldProp(property, ValuesWith(axis, edited));
            });
        }

        ApplyReadOnlyLook();
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var values = ReadAxisValues();
        for (var i = 0; i < _inputs.Length; i++)
            _inputs[i].text = values[i];
    }

    #endregion

    #region Fields the panel drives

    public override InputField[] GetInputFields()
    {
        return _inputs;
    }

    //Each axis carries its own label beside the box, so a scrub knows which component it moves.
    public override ScrubTarget[] GetScrubTargets()
    {
        var targets = new ScrubTarget[_inputs.Length];
        for (var i = 0; i < _inputs.Length; i++)
            targets[i] = new ScrubTarget(_inputs[i], _axisLabels[i]);

        return targets;
    }

    #endregion

    #region Values

    /// <summary>
    /// Every axis, with <paramref name="edited"/> in place of the one being committed.
    /// </summary>
    /// <remarks>
    /// SetFieldProp takes the whole vector, so an edit to one axis has to carry the other axes'
    /// current text along with it.
    /// </remarks>
    List<object> ValuesWith(int editedAxis, string edited)
    {
        var values = new List<object>(_inputs.Length);

        for (var i = 0; i < _inputs.Length; i++)
        {
            var text = i == editedAxis ? edited : _inputs[i].text;
            values.Add(IsInteger ? (object)TypeConverter.GetInt(text) : TypeConverter.GetFloat(text));
        }

        return values;
    }

    //A comma decimal separator on the user's locale would not parse back, so it is written as a point.
    protected static string FormatAxis(float value)
    {
        return value.ToString().Replace(",", ".");
    }

    protected static string FormatAxis(int value)
    {
        return value.ToString();
    }

    #endregion
}
