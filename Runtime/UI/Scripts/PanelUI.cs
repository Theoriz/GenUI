using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelUI : ControllableUI
{
    List<ControllableUI> _uiElements;

    public bool IsExpanded = true;

    /// <summary>The whole panel, colour bar included. What the caller parents, orders and destroys.</summary>
    public GameObject Root { get { return transform.parent.gameObject; } }

    /// <summary>The row the preset buttons are gathered into, inside <see cref="PresetSection"/>.</summary>
    public RectTransform PresetHolder { get { return _presetHolder; } }

    /// <summary>The panel's own preset section: the block the preset buttons and the preset dropdown
    /// are gathered into. What the caller orders and hides.</summary>
    public RectTransform PresetSection { get { return _presetSection; } }

    /// <summary>Sibling index of the first row: what a caller moving a section to the top of the panel
    /// body sets, so it lands under the title and the space kept beneath it.</summary>
    public int FirstRowIndex { get { return _titleGap.GetSiblingIndex() + 1; } }

    RectTransform _title;
    RectTransform _titleGap;
    RectTransform _presetHolder;
    RectTransform _presetSection;
    Transform _arrow;
    Text _titleText;

    //Sections CleanGeneratedUI turned off because nothing landed in them. Unfolding the panel shows
    //every other child again, so it has to know which ones were never meant to be shown.
    readonly List<Transform> _hiddenSections = new List<Transform>();

    #region Building

    /// <summary>
    /// Creates an empty panel: a colour bar, a foldable title, and the section the preset controls end
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
        panel._presetSection = panel.CreatePresetSection("PresetSection", out panel._presetHolder);
        return panel;
    }

    void BuildTitle(string title, Color barColor)
    {
        _title = UIFactory.CreateRect("Title", transform, GenUIStyle.PanelTitleHeight);
        UIFactory.AddImage(_title.gameObject, GenUIAssets.Instance.Background,
            GenUIStyle.PanelTitleBackground(barColor));

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

        _titleGap = UIFactory.CreateRect("TitleGap", transform, GenUIStyle.PanelTitleBottomSpacing);
    }

    void AddFoldButton(GameObject go)
    {
        var button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();

        button.targetGraphic = go.GetComponent<Graphic>();
        button.onClick.AddListener(HandleClickOnButton);
    }

    /// <summary>
    /// Creates a preset section - a block set off from the member rows by a rule and the space around
    /// it - and hands back the button row inside it. Further rows are parented to the section itself.
    /// </summary>
    /// <remarks>
    /// Called once per block rather than cloning the panel's own: a clone would copy the separator and
    /// whatever has already been reparented into it.
    /// </remarks>
    public RectTransform CreatePresetSection(string name, out RectTransform holder)
    {
        var section = UIFactory.CreateRect(name, transform);
        var layout = UIFactory.AddVerticalLayout(section.gameObject, controlHeight: false);
        //The gap is part of the section's own height and holds the separator, so the block keeps its
        //distance from the row above whatever ends up in it.
        layout.padding = new RectOffset(GenUIStyle.PresetSectionPadding, GenUIStyle.PresetSectionPadding,
            GenUIStyle.PresetSectionPadding + GenUIStyle.PresetSectionGap, GenUIStyle.PresetSectionPadding);

        //Placed in the gap rather than laid out as a row of its own, so the space around it stays the
        //same whatever the section holds.
        var separator = UIFactory.CreateChild("Separator", section);
        separator.anchorMin = new Vector2(0f, 1f);
        separator.anchorMax = new Vector2(1f, 1f);
        separator.sizeDelta = new Vector2(0f, GenUIStyle.SeparatorThickness);
        separator.anchoredPosition = new Vector2(0f, -GenUIStyle.SeparatorSpaceAbove);
        UIFactory.AddImage(separator.gameObject, null, GenUIStyle.SeparatorColor, Image.Type.Simple);
        UIFactory.AddLayoutElement(separator.gameObject, ignoreLayout: true);

        holder = UIFactory.CreateRect("PresetHolder", section, GenUIStyle.PresetRowHeight);
        UIFactory.AddHorizontalLayout(holder.gameObject, expandHeight: true);

        LayoutSection(section);
        return section;
    }

    /// <summary>
    /// Sizes a preset section to the rows it holds. The panel's layout group does not control child
    /// heights, so a section that has just been filled has to be given its own.
    /// </summary>
    public void LayoutSection(RectTransform section)
    {
        var height = GenUIStyle.PresetSectionGap + 2f * GenUIStyle.PresetSectionPadding;
        foreach (Transform child in section)
        {
            //Same two exclusions the layout group itself makes: the separator is placed in the gap
            //rather than stacked, and a hidden row takes no space.
            var element = child.GetComponent<LayoutElement>();
            if (!child.gameObject.activeSelf || (element != null && element.ignoreLayout)) continue;

            height += ((RectTransform)child).rect.height;
        }

        section.sizeDelta = new Vector2(section.sizeDelta.x, height);
    }

    /// <summary>
    /// Inserts an empty row above the first method button left in the panel body, so the buttons are
    /// not stacked tight under the last member row.
    /// </summary>
    /// <remarks>
    /// Called once the preset buttons have been reparented into their sections, since those are not
    /// the block being set off. Does nothing when no member row precedes the buttons: the gap would
    /// then only push the first button away from the title.
    /// </remarks>
    public void AddMethodGap()
    {
        Transform firstButton = null;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<ButtonUI>() == null) continue;

            firstButton = child;
            break;
        }

        if (firstButton == null) return;

        var index = firstButton.GetSiblingIndex();
        var hasRowAbove = false;
        for (var i = 0; i < index; i++)
        {
            var sibling = transform.GetChild(i);
            //The title and its gap are not rows, and a hidden section takes no space.
            if (sibling == _title || sibling == _titleGap || !sibling.gameObject.activeSelf) continue;

            hasRowAbove = true;
            break;
        }

        if (!hasRowAbove) return;

        var gap = UIFactory.CreateRect("MethodGap", transform, GenUIStyle.MethodGapHeight);
        gap.SetSiblingIndex(index);
    }

    /// <summary>
    /// Drops the space under the title when the panel opens on a header, which is taller than a row
    /// and so already carries space of its own - stacked, the two read as a hole under the heading.
    /// </summary>
    /// <remarks>
    /// Called once the sections have been ordered, since one of them can be the first row. The space
    /// is a row rather than a margin, so it cannot collapse into the header's on its own.
    /// </remarks>
    public void TrimTitleGap()
    {
        foreach (Transform child in transform)
        {
            if (child == _title || child == _titleGap || !child.gameObject.activeSelf) continue;

            if (child.GetComponent<HeaderUI>() != null)
                HideSection(_titleGap);

            return;
        }
    }

    /// <summary>Hides a section nothing landed in, and keeps unfolding the panel from showing it again.</summary>
    public void HideSection(Transform section)
    {
        section.gameObject.SetActive(false);
        if (!_hiddenSections.Contains(section))
            _hiddenSections.Add(section);
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
            if (child != _title && !_hiddenSections.Contains(child))
                child.gameObject.SetActive(IsExpanded);
        }

        _arrow.rotation = Quaternion.Euler(new Vector3(0, 0, IsExpanded ? -90 : 0));
    }

    #endregion
}
