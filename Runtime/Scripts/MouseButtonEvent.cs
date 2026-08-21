using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[AddComponentMenu("Event/MouseButtonEvent")]
public class MouseButtonEvent : MonoBehaviour, IPointerUpHandler, IPointerClickHandler
{
    public ControllableUI linkedUI;
    [Space]
    public bool enableRightClickMenu = true;
    public bool enableColorPicker = false;

    /*Called whenever a mouse click or touch screen tap is registered
    on the UI object this script is attached to.*/
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            OnRightButtonUp();

        if (eventData.button == PointerEventData.InputButton.Left)
            OnLeftButtonUp();
    }

    /// <summary>Deliberately empty: implementing the interface is what this is for.</summary>
    /// <remarks>
    /// The input module sends pointer-up to the object that took the press, which it resolves as the
    /// nearest pointer-down handler and, failing that, the nearest click handler. A part with no
    /// Selectable of its own - a plain label, a row's own backing graphic - is neither, so without
    /// this it would never take the press and would never hear the release.
    /// </remarks>
    public void OnPointerClick(PointerEventData eventData)
    {
    }

    //Both actions are about controlling the member: the menu copies the OSC address that writes it,
    //the picker writes it directly. A read-only member cannot be written by either, so neither opens,
    //and the rows that stand for no member at all - a header, a tooltip - have no address to copy.
    bool CanControlLinkedUI()
    {
        return linkedUI != null && linkedUI.HasAddress && linkedUI.IsInteractible;
    }

    void OnRightButtonUp()
    {
        if (enableRightClickMenu && CanControlLinkedUI())
            UIMaster.Instance.CreateRightClickMenu(linkedUI);
    }

    void OnLeftButtonUp()
    {
        if (enableColorPicker && CanControlLinkedUI())
            UIMaster.Instance.CreateColorPicker(linkedUI);
    }
}
