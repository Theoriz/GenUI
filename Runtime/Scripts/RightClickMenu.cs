using UnityEngine;
using UnityEngine.UI;

public class RightClickMenu : MonoBehaviour
{
    public Button copyAddressButton;
    public Button closeButton;

    [HideInInspector] public ControllableUI linkedUI;

    //The menu grows to fit its one item.
    const float ItemMinWidth = 215f;
    const float ItemMinHeight = 28f;

    /// <summary>The part that follows the pointer. The root stays over the whole screen.</summary>
    public RectTransform Content { get; private set; }

    /// <summary>
    /// Creates the menu, positioned by whoever opens it. It is left active; UIMaster hides it.
    /// </summary>
    public static RightClickMenu Build(Transform canvas)
    {
        var root = UIFactory.CreateChild("RightClickMenu", canvas);
        var menu = root.gameObject.AddComponent<RightClickMenu>();

        //First, so it sits behind the menu: a click anywhere else dismisses it.
        menu.closeButton = UIFactory.AddBackdrop(root, GenUIStyle.PopupBackdrop);

        //Anchored by its top-left corner, so the menu opens down and to the right of the pointer.
        var items = UIFactory.CreateChild("Menu", root);
        items.anchorMin = new Vector2(0f, 0f);
        items.anchorMax = new Vector2(0f, 0f);
        items.pivot = new Vector2(0f, 1f);
        items.sizeDelta = Vector2.zero;
        menu.Content = items;
        UIFactory.AddImage(items.gameObject, GenUIAssets.Instance.Background, new Color(1f, 1f, 1f, 0f));
        UIFactory.AddVerticalLayout(items.gameObject, expandHeight: true);
        var fitter = items.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var copy = UIFactory.CreateChild("CopyAddress", items);
        copy.pivot = new Vector2(0f, 1f);
        UIFactory.AddLayoutElement(copy.gameObject, minWidth: ItemMinWidth, minHeight: ItemMinHeight);
        menu.copyAddressButton = UIFactory.AddButton(copy.gameObject, GenUIAssets.Instance.Box, Color.white);
        menu.copyAddressButton.colors = GenUIStyle.ControlColors();

        var label = UIFactory.CreateChild("Text", copy);
        UIFactory.AddText(label.gameObject, "Copy OSC control address", GenUIStyle.LabelFontSize,
            TextAnchor.MiddleCenter, GenUIStyle.LabelColor);

        return menu;
    }
}
