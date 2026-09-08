using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shows a member's tooltip while the pointer is over its name.
/// </summary>
/// <remarks>
/// It carries no pointer-down handler on purpose: the object that takes the press is the nearest
/// pointer-down handler, and adding one to the label would take it from the row's MouseButtonEvent,
/// which then never hears the release - see MouseButtonEvent.OnPointerClick. A press is dealt with by
/// the popup, which hides itself for as long as a mouse button is held.
/// </remarks>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string text;

    #region MonoBehaviour

    //A row folded away or destroyed under the pointer sends no exit, so the tooltip would stay up.
    void OnDisable()
    {
        Release();
    }

    #endregion

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIMaster.Instance != null && !string.IsNullOrEmpty(text))
            UIMaster.Instance.ShowTooltip(text, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    void Release()
    {
        if (UIMaster.Instance != null)
            UIMaster.Instance.HideTooltip(this);
    }
}
