using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[ExecuteInEditMode]
[AddComponentMenu("Event/RightButtonEvent")]
public class RightButtonEvent : MonoBehaviour, IPointerUpHandler
{
    public ControllableUI linkedUI;

    [System.Serializable] public class RightButton : UnityEvent { }
    public RightButton onRightUp;


    void Start()
    {
        onRightUp.AddListener(delegate { UIMaster.Instance.CreateRightClickMenu(linkedUI); } );        
    }

    /*Called whenever a mouse click or touch screen tap is registered
    on the UI object this script is attached to.*/
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            onRightUp.Invoke();
    }
}
