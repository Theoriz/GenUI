using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelUI : ControllableUI
{
    List<ControllableUI> _uiElements;

    public bool IsExpanded = true;

    /// <summary>The whole panel, colour bar included. What the caller parents, orders and destroys.</summary>
    public GameObject Root { get { return transform.parent.gameObject; } }

    /// <summary>The row the preset controls are gathered into.</summary>
    public RectTransform PresetHolder { get { return _presetHolder; } }

    RectTransform _title;
    RectTransform _presetHolder;
    Transform _arrow;
    Text _titleText;

    #region Building

    /// <summary>
    /// Creates an empty panel: a colour bar, a foldable title, and the row the preset controls end
    /// up in. Its widgets are then parented to the returned PanelUI's own transform.
    /// </summary>
    public static PanelUI Build(Transform parent, string title, Color barColor)
    {
        var root = UIFactory.CreateRect("Panel", parent);
        UIFactory.AddHorizontalLayout(root.gameObject, GenUIStyle.PanelBarGap,
            controlHeight: true, expandWidth: false, expandHeight: true, alignment: TextAnchor.LowerRight);

        var bar = UIFactory.CreateChild("ColorBar", root);
        UIFactory.AddImage(bar.gameObject, null, barColor, Image.Type.Simple);
        UIFactory.AddLayoutElement(bar.gameObject, minWidth: GenUIStyle.ColorBarWidth);

        var control = UIFactory.CreateChild("ControlPanel", root);
        UIFactory.AddImage(control.gameObject, GenUIAssets.Instance.Background, GenUIStyle.PanelBackground);
        UIFactory.AddVerticalLayout(control.gameObject, padding: GenUIStyle.PanelPadding, controlHeight: false);

        var panel = control.gameObject.AddComponent<PanelUI>();
        panel.BuildTitle(title, barColor);
        panel.BuildPresetHolder();
        return panel;
    }

    void BuildTitle(string title, Color barColor)
    {
        _title = UIFactory.CreateRect("Title", transform, GenUIStyle.PanelTitleHeight);

        var arrow = UIFactory.CreateRect("Image", _title);
        arrow.anchorMin = new Vector2(0f, 0.5f);
        arrow.anchorMax = new Vector2(0f, 0.5f);
        arrow.anchoredPosition = new Vector2(GenUIStyle.PanelArrowInset, 0f);
        arrow.sizeDelta = new Vector2(GenUIStyle.PanelArrowSize, GenUIStyle.PanelArrowSize);
        UIFactory.AddImage(arrow.gameObject, GenUIAssets.Instance.PanelArrow, barColor, Image.Type.Simple);
        _arrow = arrow;

        var titleRect = UIFactory.CreateCentered("Text", _title, 300f, 30f);
        _titleText = UIFactory.AddText(titleRect.gameObject, title, GenUIStyle.PanelTitleFontSize,
            TextAnchor.MiddleCenter, GenUIStyle.LabelColor, bold: true);

        //Both halves of the title fold the panel, so the whole bar is clickable and not just the arrow.
        AddFoldButton(arrow.gameObject);
        AddFoldButton(titleRect.gameObject);
    }

    void AddFoldButton(GameObject go)
    {
        var button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();

        button.targetGraphic = go.GetComponent<Graphic>();
        button.onClick.AddListener(HandleClickOnButton);
    }

    void BuildPresetHolder()
    {
        _presetHolder = UIFactory.CreateRect("PresetHolder", transform, GenUIStyle.PresetRowHeight);
        UIFactory.AddHorizontalLayout(_presetHolder.gameObject, expandHeight: true);
    }

    #endregion

    #region Contents

    public void Init(Controllable target)
    {
        LinkedControllable = target;
        if(PlayerPrefs.HasKey(LinkedControllable.controllableId)) {
            IsExpanded = PlayerPrefs.GetInt(LinkedControllable.controllableId) != 0;
            HandleClickOnButton();
        }
    }

    public void AddUIElement(ControllableUI newElement)
    {
        if(_uiElements == null)
        {
            _uiElements = new List<ControllableUI>();
        }
        _uiElements.Add(newElement);
    }

    public override void RemoveUI()
    {
        if (_uiElements == null)
            return;

        foreach (var element in _uiElements)
            element.RemoveUI();
    }

    #endregion

    #region Fold and unfold

    public void HandleClickOnButton()
    {
        if (IsExpanded)
            Close();
        else
            Open();
    }

    public void Close()
    {
        IsExpanded = false;
        ShowContents();
        PlayerPrefs.SetInt(LinkedControllable.controllableId, IsExpanded ? 0 : 1);
    }

    public void Open()
    {
        IsExpanded = true;
        ShowContents();

        //Catch the widgets up: while folded they stopped being refreshed.
        if (_uiElements != null)
        {
            foreach (var element in _uiElements)
                element.HandleTargetChange("");
        }

        PlayerPrefs.SetInt(LinkedControllable.controllableId, IsExpanded ? 0 : 1);
    }

    //Everything but the title, which is what stays behind to unfold the panel again.
    void ShowContents()
    {
        foreach (Transform child in transform)
        {
            if (child != _title)
                child.gameObject.SetActive(IsExpanded);
        }

        _arrow.rotation = Quaternion.Euler(new Vector3(0, 0, IsExpanded ? -90 : 0));
    }

    #endregion
}
