using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    public Button closeButton;
    public FlexibleColorPicker colorPicker;

    [HideInInspector] public ColorUI linkedUI;

    private Color _lastPushedColor;

    #region MonoBehaviour

    private void OnEnable()
    {
        if (linkedUI)
        {
            var c = linkedUI.GetCurrentColorValue();
            colorPicker.SetColor(c);
            _lastPushedColor = c;
        }
    }

    private void Update()
    {
        if (!linkedUI)
            return;

        var c = colorPicker.GetColor();
        if (c != _lastPushedColor)
        {
            _lastPushedColor = c;
            linkedUI.OnColorPickerUpdated(c);
        }
    }

    #endregion
}
