using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag a numeric member's label left or right to scrub its value, the way the Unity Inspector does.
/// </summary>
/// <remarks>
/// Added at runtime to the label, never to the field: InputField activates editing on pointer-down
/// and runs its own drag for text selection, so a handler on the field would scrub and select text at
/// the same time. Values are written by setting the field's text and raising its onEndEdit, which
/// reuses the widget's existing commit logic - clamping, and sending every component of a vector.
/// </remarks>
[AddComponentMenu("")]
public class DragValueUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //A [Range] member crosses its whole span in this many screen pixels, so it feels like its slider.
    const float RangeDragPixels = 400f;
    //Unbounded members have no span to scale against, so they step at a fixed rate.
    const float FloatUnitsPerPixel = 0.05f;
    const float PixelsPerIntStep = 10f;

    const float CoarseMultiplier = 10f;
    const float FineMultiplier = 0.1f;

    ControllableUI _owner;
    InputField _field;
    RangeAttribute _range;
    bool _isInt;

    Canvas _canvas;
    double _startValue;
    string _lastWritten;
    bool _scrubbing;

    public static void Attach(ControllableUI owner, ControllableUI.ScrubTarget target)
    {
        if (owner == null || target.Field == null || target.Label == null)
            return;

        //Read-only members are shown but not editable, so there is nothing to scrub.
        if (!owner.IsInteractible)
            return;

        var drag = target.Label.gameObject.AddComponent<DragValueUI>();
        drag._owner = owner;
        drag._field = target.Field;
        drag._isInt = target.Field.contentType == InputField.ContentType.IntegerNumber;
        drag._range = owner.Property != null
            ? Attribute.GetCustomAttribute(owner.Property, typeof(RangeAttribute)) as RangeAttribute
            : null;

        //The label has to receive raycasts to be draggable at all.
        target.Label.raycastTarget = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //A vertical drag belongs to the scroll view; only horizontal movement is a scrub. Measured
        //from the press rather than from one frame's delta, which is noisy at the drag threshold.
        var travel = eventData.position - eventData.pressPosition;
        if (Mathf.Abs(travel.x) < Mathf.Abs(travel.y))
        {
            _scrubbing = false;
            Forward(eventData, ExecuteEvents.beginDragHandler);
            return;
        }

        _scrubbing = true;
        _canvas = GetComponentInParent<Canvas>();
        _startValue = ReadValue();
        _lastWritten = _field.text;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_scrubbing)
        {
            Forward(eventData, ExecuteEvents.dragHandler);
            return;
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var coarse = keyboard != null && keyboard.shiftKey.isPressed;
        var fine = keyboard != null && keyboard.ctrlKey.isPressed;

        //Measured from where the drag started rather than integrated per frame, so rounding an int
        //cannot swallow movement smaller than one unit.
        var travelled = eventData.position.x - eventData.pressPosition.x;
        var scale = _canvas != null ? _canvas.scaleFactor : 1f;

        var value = _startValue + ScrubDelta(travelled, scale, _isInt, _range, coarse, fine);

        if (_range != null)
            value = Math.Min(_range.max, Math.Max(_range.min, value));

        var text = _isInt
            ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
            : ((float)value).ToString(CultureInfo.InvariantCulture);

        //A press that has not moved far enough to change the value must not emit anything, or a
        //stationary drag sends an OSC message every frame.
        if (text == _lastWritten)
            return;

        _lastWritten = text;
        _field.text = text;
        _field.onEndEdit.Invoke(text);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_scrubbing)
            Forward(eventData, ExecuteEvents.endDragHandler);

        _scrubbing = false;
    }

    /// <summary>
    /// Value change for a horizontal drag of <paramref name="pixelsX"/> screen pixels.
    /// </summary>
    /// <remarks>
    /// Divided by the canvas scale so a UI zoomed with PageUp scrubs at the same rate per screen
    /// pixel as one at 1x.
    /// </remarks>
    public static double ScrubDelta(float pixelsX, float canvasScale, bool isInt,
                                    RangeAttribute range, bool coarse, bool fine)
    {
        if (canvasScale <= 0f)
            canvasScale = 1f;

        var pixels = pixelsX / canvasScale;

        if (coarse)
            pixels *= CoarseMultiplier;
        if (fine)
            pixels *= FineMultiplier;

        if (range != null)
            return pixels * (range.max - range.min) / RangeDragPixels;

        if (isInt)
            return pixels / PixelsPerIntStep;

        return pixels * FloatUnitsPerPixel;
    }

    double ReadValue()
    {
        double parsed;
        return double.TryParse(_field.text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : 0;
    }

    //Hands the gesture to whatever handles it further up - the panel's ScrollRect - so dragging
    //vertically over a label still scrolls.
    void Forward<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
        where T : IEventSystemHandler
    {
        if (transform.parent != null)
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, handler);
    }
}
