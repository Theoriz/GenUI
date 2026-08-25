using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One draggable area of the colour picker - the SV square, the hue bar, the alpha bar - reporting
/// where the pointer is inside it as a fraction of its rect.
/// </summary>
/// <remarks>
/// It handles the press as well as the drag: a drag only starts once the pointer has moved, so
/// without IPointerDownHandler a single click on a bar would move nothing. Added at runtime by
/// <see cref="GenUIColorPicker"/>, never serialized.
/// </remarks>
[AddComponentMenu("")]
public class PickerAreaDrag : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    Action<Vector2> _onPoint;
    RectTransform _area;

    #region Setup

    public static PickerAreaDrag Attach(RectTransform area, Action<Vector2> onPoint)
    {
        var drag = area.gameObject.AddComponent<PickerAreaDrag>();
        drag._area = area;
        drag._onPoint = onPoint;
        return drag;
    }

    #endregion

    #region Drag handling

    public void OnPointerDown(PointerEventData eventData) { Report(eventData); }

    public void OnBeginDrag(PointerEventData eventData) { Report(eventData); }

    public void OnDrag(PointerEventData eventData) { Report(eventData); }

    void Report(PointerEventData eventData)
    {
        if (_onPoint == null || _area == null)
            return;

        Vector2 local;
        //pressEventCamera is null on an overlay canvas, which is what this call expects there.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out local))
            return;

        _onPoint(GenUIColorPicker.NormalizedPoint(_area.rect, local));
    }

    #endregion
}
