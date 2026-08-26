using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Reports the press on the object it sits on, so a widget can do something before the control that
/// took the press acts on it.
/// </summary>
/// <remarks>
/// The input module runs every handler on the pressed object, so this fires even where a Selectable
/// takes the press - and pointer-down comes before the click a Dropdown opens on. Added at runtime,
/// never serialized.
/// </remarks>
[AddComponentMenu("")]
public class PressNotifier : MonoBehaviour, IPointerDownHandler
{
    public Action Pressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        //The left button alone: it is the one that opens the control this press prepares for.
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Pressed != null)
            Pressed();
    }
}
