using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public enum HandlerType
{
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
    TopLeft,
    Top
}

[RequireComponent(typeof(EventTrigger))]
public class FlexibleResizeHandler : MonoBehaviour
{
    public HandlerType Type;
    public RectTransform Target;
    public Vector2 MinimumDimmensions = new Vector2(50, 50);
    public Vector2 MaximumDimmensions = new Vector2(800, 800);

    public bool ClampToCanvasWidth;
    public RectTransform Canvas;

    private EventTrigger _eventTrigger;
    
	void Start ()
	{
	    _eventTrigger = GetComponent<EventTrigger>();
        _eventTrigger.AddEventTrigger(OnDrag, EventTriggerType.Drag);
	}

    void OnDrag(BaseEventData data)
    {
        if (ClampToCanvasWidth)
            MaximumDimmensions.x = Canvas.rect.width;

        PointerEventData ped = (PointerEventData) data;
        //Target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Target.rect.width + ped.delta.x);
        //Target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Target.rect.height + ped.delta.y);
        RectTransform.Edge? horizontalEdge = null;
        RectTransform.Edge? verticalEdge = null;

        switch (Type)
        {
            case HandlerType.TopRight:
                horizontalEdge = RectTransform.Edge.Left;
                verticalEdge = RectTransform.Edge.Bottom;
                break;
            case HandlerType.Right:
                horizontalEdge = RectTransform.Edge.Left;
                break;
            case HandlerType.BottomRight:
                horizontalEdge = RectTransform.Edge.Left;
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.Bottom:
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.BottomLeft:
                horizontalEdge = RectTransform.Edge.Right;
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.Left:
                horizontalEdge = RectTransform.Edge.Right;
                break;
            case HandlerType.TopLeft:
                horizontalEdge = RectTransform.Edge.Right;
                verticalEdge = RectTransform.Edge.Bottom;
                break;
            case HandlerType.Top:
                verticalEdge = RectTransform.Edge.Bottom;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        // Insets and the drag delta are measured in the parent's local units rather than screen pixels,
        // so the resize follows the pointer under a scaled, camera or world-space canvas alike.
        var parent = (RectTransform) Target.parent;
        var delta = LocalDelta(parent, ped);
        var left = Target.anchorMin.x * parent.rect.width + Target.offsetMin.x;
        var right = (1f - Target.anchorMax.x) * parent.rect.width - Target.offsetMax.x;
        var bottom = Target.anchorMin.y * parent.rect.height + Target.offsetMin.y;
        var top = (1f - Target.anchorMax.y) * parent.rect.height - Target.offsetMax.y;

        if (horizontalEdge != null)
        {
            if (horizontalEdge == RectTransform.Edge.Right)
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, right,
                    Mathf.Clamp(Target.rect.width - delta.x, MinimumDimmensions.x, MaximumDimmensions.x));
            else
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, left,
                    Mathf.Clamp(Target.rect.width + delta.x, MinimumDimmensions.x, MaximumDimmensions.x));
        }
        if (verticalEdge != null)
        {
            if (verticalEdge == RectTransform.Edge.Top)
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, top,
                    Mathf.Clamp(Target.rect.height - delta.y, MinimumDimmensions.y, MaximumDimmensions.y));
            else
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, bottom,
                    Mathf.Clamp(Target.rect.height + delta.y, MinimumDimmensions.y, MaximumDimmensions.y));
        }
    }

    static Vector2 LocalDelta(RectTransform parent, PointerEventData ped)
    {
        Vector2 now, before;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, ped.position, ped.pressEventCamera, out now)
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, ped.position - ped.delta, ped.pressEventCamera, out before))
            return now - before;

        return ped.delta;
    }
}
