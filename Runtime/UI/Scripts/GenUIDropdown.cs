using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A <see cref="Dropdown"/> that only opens on the left button.</summary>
/// <remarks>
/// Unity's Dropdown opens on a click from any button, and draws its option list on an overlay canvas
/// above everything else - so a right-click would show that list on top of the right-click menu the
/// same click opens. Every other Selectable already filters the button itself.
/// </remarks>
[AddComponentMenu("")]
public class GenUIDropdown : Dropdown
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        base.OnPointerClick(eventData);
    }
}
