using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    public Button closeButton;
    public GenUIColorPicker colorPicker;

    [HideInInspector] public ColorUI linkedUI;

    private Color _lastPushedColor;

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
    /// Wraps the picker in a click target that dismisses it. It is left active; UIMaster hides it.
    /// </summary>
    public static ColorPicker Build(Transform canvas)
    {
        var root = UIFactory.CreateChild("ColorPicker", canvas);
        var wrapper = root.gameObject.AddComponent<ColorPicker>();

        //First, so it sits behind the picker: a click anywhere else dismisses it.
        wrapper.closeButton = UIFactory.AddBackdrop(root, GenUIStyle.PopupBackdrop);

        wrapper.colorPicker = GenUIColorPicker.Build(root);
        wrapper.Content = (RectTransform)wrapper.colorPicker.transform;

        return wrapper;
    }

    #endregion
}
