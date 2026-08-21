using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    public Button closeButton;
    public FlexibleColorPicker colorPicker;

    [HideInInspector] public ColorUI linkedUI;

    private Color _lastPushedColor;

    const float PickerWidth = 200f;
    const float PickerHeight = 280f;

    /// <summary>The picker itself, which follows the pointer. The root stays over the whole screen.</summary>
    public RectTransform Content { get; private set; }

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

    #region Building

    /// <summary>
    /// Wraps the vendored FlexibleColorPicker in a click target that dismisses it. It is left
    /// active; UIMaster hides it.
    /// </summary>
    public static ColorPicker Build(Transform canvas, GameObject pickerPrefab)
    {
        var root = UIFactory.CreateChild("ColorPicker", canvas);
        var wrapper = root.gameObject.AddComponent<ColorPicker>();

        //First, so it sits behind the picker: a click anywhere else dismisses it.
        wrapper.closeButton = UIFactory.AddBackdrop(root, GenUIStyle.PopupBackdrop);

        var picker = Instantiate(pickerPrefab, root, false);
        var pickerRect = (RectTransform)picker.transform;
        pickerRect.anchorMin = Vector2.zero;
        pickerRect.anchorMax = Vector2.zero;
        pickerRect.pivot = new Vector2(0.5f, 0.5f);
        pickerRect.sizeDelta = new Vector2(PickerWidth, PickerHeight);
        wrapper.Content = pickerRect;

        wrapper.colorPicker = picker.GetComponent<FlexibleColorPicker>();
        //The main square is redrawn as the hue changes rather than kept static, which is what makes
        //it track the colour being picked.
        wrapper.colorPicker.advancedSettings.mainStatic = false;

        return wrapper;
    }

    #endregion
}
