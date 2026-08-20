using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[AddComponentMenu("Event/MouseButtonEvent")]
public class MouseButtonEvent : MonoBehaviour, IPointerUpHandler
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

    //Both actions are about controlling the member: the menu copies the OSC address that writes it,
    //the picker writes it directly. A read-only member cannot be written by either, so neither opens.
    bool CanControlLinkedUI()
    {
        return linkedUI != null && linkedUI.IsInteractible;
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
