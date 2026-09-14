using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lets the panel scrollbar resize the panel too: a drag that sets off sideways is forwarded to the
/// resize handle, one that sets off along the bar is left to the Scrollbar.
/// </summary>
/// <remarks>
/// The input module runs every drag handler on the pressed object, so the Scrollbar scrolls during
/// the first few pixels whichever way they go; a sideways lock puts its value back and switches it
/// off for the rest of the drag, which Scrollbar.OnDrag checks on every event.
/// </remarks>
[RequireComponent(typeof(Scrollbar))]
public class ScrollbarResizeGrip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    const float LockDistance = 4f;

    [SerializeField] FlexibleResizeHandler _resizeHandler;

    Scrollbar _scrollbar;
    Vector2 _travel;
    float _startValue;
    bool _decided;
    bool _resizing;

    #region MonoBehaviour

    void Awake()
    {
        _scrollbar = GetComponent<Scrollbar>();
        if (_resizeHandler == null && transform.parent != null)
            _resizeHandler = transform.parent.GetComponentInChildren<FlexibleResizeHandler>();
    }

    void OnDisable()
    {
        EndResize();
    }

    #endregion

    public void OnBeginDrag(PointerEventData eventData)
    {
        _travel = Vector2.zero;
        _startValue = _scrollbar.value;
        _decided = false;
        _resizing = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_decided)
        {
            _travel += eventData.delta;
            if (_travel.sqrMagnitude < LockDistance * LockDistance)
                return;

            _decided = true;
            _resizing = _resizeHandler != null && Mathf.Abs(_travel.x) > Mathf.Abs(_travel.y);
            if (!_resizing)
                return;

            _scrollbar.value = _startValue;
            _scrollbar.interactable = false;
        }

        if (_resizing)
            ExecuteEvents.Execute(_resizeHandler.gameObject, eventData, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndResize();
    }

    void EndResize()
    {
        if (!_resizing)
            return;

        _resizing = false;
        _scrollbar.interactable = true;
    }
}
