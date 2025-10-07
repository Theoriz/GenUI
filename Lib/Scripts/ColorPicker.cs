using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    public Button closeButton;
    public FlexibleColorPicker colorPicker;

    [HideInInspector] public ColorUI linkedUI;

    private void OnEnable()
    {
        if (linkedUI)
            colorPicker.SetColor(linkedUI.GetCurrentColorValue());
    }

    private void Update()
    {
        if (linkedUI)
            linkedUI.OnColorPickerUpdated(colorPicker.GetColor());
    }
}
