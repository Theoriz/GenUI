using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Globalization;

public class SliderUI : ControllableUI
{
    public bool IsFloat;

    // True while the UI is being refreshed from the target value; the slider/input callbacks
    // ignore writes made during that window instead of pushing them back to the Controllable.
    private bool _updating;

    Text _label;
    Slider _slider;
    InputField _input;

    #region Widget

    protected override void BuildHierarchy()
    {
        _label = UIFactory.CreateLabel(transform, "Text");

        _slider = BuildSlider();
        UIFactory.AddMouseEvent(_slider.gameObject, this);

        var fieldRect = UIFactory.CreateSlice("InputField", transform, GenUIStyle.SliderValueStart, 1f);
        _input = UIFactory.AddInputField(fieldRect);
        UIFactory.AddMouseEvent(fieldRect.gameObject, this);
    }

    Slider BuildSlider()
    {
        var rect = UIFactory.CreateSlice("Slider", transform, GenUIStyle.SliderTrackStart, GenUIStyle.SliderTrackEnd);
        rect.sizeDelta = new Vector2(0f, -GenUIStyle.SliderTrackInset);

        //The bar and the fill are inset by half a handle at each end, which is exactly how far the
        //handle's centre travels. The handle then reaches both ends of the bar, and the fill always
        //stops under its centre - Unity's own proportions leave the bar wider than the travel, so the
        //handle visibly stops short of the ends it is supposed to reach.
        var background = Band("Background", rect);
        background.sizeDelta = new Vector2(-GenUIStyle.SliderHandleWidth, 0f);
        UIFactory.AddImage(background.gameObject, GenUIAssets.Instance.Background, Color.white);

        var fillArea = Band("Fill Area", rect);
        fillArea.sizeDelta = new Vector2(-GenUIStyle.SliderHandleWidth, 0f);

        var fill = UIFactory.CreateChild("Fill", fillArea);
        UIFactory.AddImage(fill.gameObject, GenUIAssets.Instance.Box, Color.white);

        var slideArea = UIFactory.CreateChild("Handle Slide Area", rect);
        slideArea.sizeDelta = new Vector2(-GenUIStyle.SliderHandleWidth, 0f);

        var handle = UIFactory.CreateChild("Handle", slideArea);
        handle.sizeDelta = new Vector2(GenUIStyle.SliderHandleWidth, 0f);
        var handleImage = UIFactory.AddImage(handle.gameObject, GenUIAssets.Instance.Knob, Color.white, Image.Type.Simple);

        //The Slider drives the fill's and the handle's anchors from the value, so what they hold here
        //only has to be a starting point.
        var slider = rect.gameObject.AddComponent<Slider>();
        slider.targetGraphic = handleImage;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    //A strip across the middle of the track's rect, which is what leaves the handle taller than the bar.
    static RectTransform Band(string name, Transform parent)
    {
        var rect = UIFactory.CreateChild(name, parent);
        rect.anchorMin = new Vector2(0f, GenUIStyle.SliderBandMin);
        rect.anchorMax = new Vector2(1f, GenUIStyle.SliderBandMax);
        return rect;
    }

    public void CreateUI(Controllable target, FieldInfo property, RangeAttribute rangeAttribut, bool isInteractible, bool isFloat = true) {

        Property = property;
        IsFloat = isFloat;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        _input.contentType = IsFloat ? InputField.ContentType.DecimalNumber : InputField.ContentType.IntegerNumber;

        _input.onEndEdit.AddListener((value) =>
        {
            if (_updating)
                return;

            RecordUndo();

            var list = new List<object>();
            if (!IsFloat)
                list.Add(Mathf.Clamp(TypeConverter.GetInt(value), (int)rangeAttribut.min, (int)rangeAttribut.max));
            else
                list.Add(Mathf.Clamp(TypeConverter.GetFloat(value), rangeAttribut.min, rangeAttribut.max));

            target.SetFieldProp(property, list);
        });

        _label.text = ParseNameString(property.Name);
        _input.text = FormatValue(property.GetValue(target));

        _slider.maxValue = rangeAttribut.max;
        _slider.minValue = rangeAttribut.min;
        _slider.interactable = isInteractible;
        _slider.wholeNumbers = !isFloat;

        _slider.onValueChanged.AddListener((value) =>
        {
            if (_updating)
                return;

            RecordUndo();

            var list = new List<object>();
            list.Add(value);
            LinkedControllable.SetFieldProp(property, list);
            _input.text = FormatValue(property.GetValue(target));
        });

        if (isFloat)
            _slider.value = TypeConverter.GetFloat(property.GetValue(target));
        else
            _slider.value = TypeConverter.GetInt(property.GetValue(target));

        //The bar stays visible when read-only - greyed out by the line above - because it still shows
        //where the value sits in its range. Only the box beside it becomes a plain value.
        ApplyReadOnlyLook();
    }

    #endregion

    #region Fields the panel drives

    //The numeric box beside the slider. The slider itself is not a field, so Tab skips it.
    public override InputField[] GetInputFields()
    {
        return new[] { _input };
    }

    public override ScrubTarget[] GetScrubTargets()
    {
        return new[] { new ScrubTarget(_input, _label) };
    }

    #endregion

    #region Value

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        if (_updating)
            return;

        _updating = true;
        try
        {
            var value = Property.GetValue(LinkedControllable);

            _slider.value = IsFloat ? TypeConverter.GetFloat(value) : TypeConverter.GetInt(value);
            _input.text = FormatValue(value);
        }
        finally
        {
            _updating = false;
        }
    }

    static string FormatValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    #endregion
}
