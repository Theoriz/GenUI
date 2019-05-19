using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
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
        int clickID = eventData.pointerId;

        if (clickID == -1)
        {
            //Debug.Log("Left mouse click registered");
        }
        else if (clickID == -2)
        {
            onRightUp.Invoke();
            //Debug.Log("Right mouse click registered");
        }
        else if (clickID == -3)
        {
           // Debug.Log("Center mouse click registered");
        }
    }
}
