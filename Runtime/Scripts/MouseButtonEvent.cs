using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[ExecuteInEditMode]
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

    void OnRightButtonUp()
    {
        if (enableRightClickMenu)
            UIMaster.Instance.CreateRightClickMenu(linkedUI);
    }

    void OnLeftButtonUp()
    {
        if (enableColorPicker)
            UIMaster.Instance.CreateColorPicker(linkedUI);
    }
}
