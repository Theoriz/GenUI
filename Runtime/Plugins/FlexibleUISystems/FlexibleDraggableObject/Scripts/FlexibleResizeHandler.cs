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
        if (horizontalEdge != null)
        {
            if (horizontalEdge == RectTransform.Edge.Right)
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, 
                    Screen.width - Target.position.x - Target.pivot.x * Target.rect.width, 
                    Mathf.Clamp(Target.rect.width - ped.delta.x, MinimumDimmensions.x, MaximumDimmensions.x));
            else 
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)horizontalEdge, 
                    Target.position.x - Target.pivot.x * Target.rect.width, 
                    Mathf.Clamp(Target.rect.width + (ped.delta.x), MinimumDimmensions.x, MaximumDimmensions.x));
        }
        if (verticalEdge != null)
        {
            if (verticalEdge == RectTransform.Edge.Top)
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, 
                    Screen.height - Target.position.y - Target.pivot.y * Target.rect.height, 
                    Mathf.Clamp(Target.rect.height - ped.delta.y, MinimumDimmensions.y, MaximumDimmensions.y));
            else 
                Target.SetInsetAndSizeFromParentEdge((RectTransform.Edge)verticalEdge, 
                    Target.position.y - Target.pivot.y * Target.rect.height, 
                    Mathf.Clamp(Target.rect.height + ped.delta.y, MinimumDimmensions.y, MaximumDimmensions.y));
        }
    }
}
