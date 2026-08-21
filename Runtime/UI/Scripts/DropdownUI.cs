using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;

public class DropdownUI : ControllableUI
{
    //Set by whichever CreateUI ran: a list-backed dropdown carries the name of the List<string> its
    //entries come from, an enum-backed one carries the enum's Type.
    [System.NonSerialized] public string TargetListName;
    [System.NonSerialized] public Type enumType = null;

    //An enum's members never change, so its options and values are read once. The list route has to
    //re-read its entries instead: they can be added to at runtime, as controllablePresetList is.
    string[] _enumNames;
    Array _enumValues;

    Text _label;
    Dropdown _dropdown;
    Image _arrow;

    #region Building

    //Unity's stock dropdown template, reproduced: a scrolling list under the closed control, whose
    //single Item the Dropdown clones once per option. The numbers are that template's, not GenUI's,
    //so they live here beside the code that lays them out rather than in GenUIStyle.
    const float TemplateHeight = 150f;
    const float TemplateGap = 2f;
    const float ItemHeight = 20f;
    const float ContentHeight = 28f;
    const float ScrollbarWidth = 20f;
    const float ViewportInset = 18f;
    const float ArrowSize = 20f;
    const float ArrowInset = -15f;
    const float CheckmarkSize = 20f;
    const float CheckmarkInset = 10f;

    protected override void BuildHierarchy()
    {
        _label = UIFactory.CreateLabel(transform, "Text");

        var rect = UIFactory.CreateSlice("Dropdown", transform, GenUIStyle.LabelWidthRatio, 1f);
        var background = UIFactory.AddImage(rect.gameObject, GenUIAssets.Instance.Box, Color.white);

        var caption = UIFactory.CreateChild("Label", rect);
        caption.anchoredPosition = new Vector2(-7.5f, 0f);
        caption.sizeDelta = new Vector2(-35f, 0f);
        var captionText = UIFactory.AddText(caption.gameObject, string.Empty, GenUIStyle.LabelFontSize,
            TextAnchor.MiddleLeft, GenUIStyle.LabelColor);

        var arrow = UIFactory.CreateRect("Arrow", rect);
        arrow.anchorMin = new Vector2(1f, 0.5f);
        arrow.anchorMax = new Vector2(1f, 0.5f);
        arrow.anchoredPosition = new Vector2(ArrowInset, 0f);
        arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        _arrow = UIFactory.AddImage(arrow.gameObject, GenUIAssets.Instance.DropdownArrow, Color.white, Image.Type.Simple);

        Text itemText;
        var template = BuildTemplate(rect, out itemText);

        _dropdown = rect.gameObject.AddComponent<Dropdown>();
        _dropdown.targetGraphic = background;
        _dropdown.colors = GenUIStyle.ControlColors();
        _dropdown.template = template;
        _dropdown.captionText = captionText;
        _dropdown.itemText = itemText;

        //Only now: the Dropdown clones the template every time it opens, so it must not be a live
        //part of the panel in between.
        template.gameObject.SetActive(false);

        UIFactory.AddMouseEvent(rect.gameObject, this);
    }

    RectTransform BuildTemplate(Transform parent, out Text itemText)
    {
        var template = UIFactory.CreateRect("Template", parent);
        template.anchorMin = Vector2.zero;
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, TemplateGap);
        template.sizeDelta = new Vector2(0f, TemplateHeight);
        UIFactory.AddImage(template.gameObject, GenUIAssets.Instance.Box, GenUIStyle.DropdownTemplateBackground);

        var viewport = UIFactory.CreateChild("Viewport", template);
        viewport.pivot = new Vector2(0f, 1f);
        viewport.sizeDelta = new Vector2(-ViewportInset, 0f);
        UIFactory.AddImage(viewport.gameObject, GenUIAssets.Instance.UIMask, Color.white);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        var content = UIFactory.CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, ContentHeight);

        itemText = BuildItem(content);

        var scrollbar = BuildScrollbar(template);

        var scroll = template.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = -3f;

        return template;
    }

    Text BuildItem(Transform content)
    {
        var item = UIFactory.CreateRect("Item", content);
        item.anchorMin = new Vector2(0f, 0.5f);
        item.anchorMax = new Vector2(1f, 0.5f);
        item.sizeDelta = new Vector2(0f, ItemHeight);

        var itemBackground = UIFactory.CreateChild("Item Background", item);
        var backgroundImage = UIFactory.AddImage(itemBackground.gameObject, null, GenUIStyle.DropdownItemBackground, Image.Type.Simple);

        var checkmark = UIFactory.CreateRect("Item Checkmark", item);
        checkmark.anchorMin = new Vector2(0f, 0.5f);
        checkmark.anchorMax = new Vector2(0f, 0.5f);
        checkmark.anchoredPosition = new Vector2(CheckmarkInset, 0f);
        checkmark.sizeDelta = new Vector2(CheckmarkSize, CheckmarkSize);
        var checkmarkImage = UIFactory.AddImage(checkmark.gameObject, GenUIAssets.Instance.Checkmark, Color.white, Image.Type.Simple);

        var label = UIFactory.CreateChild("Item Label", item);
        label.anchoredPosition = new Vector2(5f, -0.5f);
        label.sizeDelta = new Vector2(-30f, -3f);
        var text = UIFactory.AddText(label.gameObject, string.Empty, GenUIStyle.LabelFontSize,
            TextAnchor.MiddleLeft, GenUIStyle.DropdownItemLabel);

        var toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;

        return text;
    }

    Scrollbar BuildScrollbar(Transform template)
    {
        var rect = UIFactory.CreateRect("Scrollbar", template);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
        UIFactory.AddImage(rect.gameObject, GenUIAssets.Instance.Background, Color.white);

        var slidingArea = UIFactory.CreateChild("Sliding Area", rect);
        slidingArea.sizeDelta = new Vector2(-ScrollbarWidth, -ScrollbarWidth);

        var handle = UIFactory.CreateRect("Handle", slidingArea);
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = new Vector2(1f, 0.2f);
        handle.sizeDelta = new Vector2(ScrollbarWidth, ScrollbarWidth);
        var handleImage = UIFactory.AddImage(handle.gameObject, GenUIAssets.Instance.Box, Color.white);

        var scrollbar = rect.gameObject.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.size = 0.2f;
        return scrollbar;
    }

    #endregion

    #region Creation

    /// <summary>A dropdown over the entries of a <c>List&lt;string&gt;</c> named by `targetList`.</summary>
    public void CreateUI(Controllable target, string targetListName, FieldInfo activeElement, bool isInteractible) {

        TargetListName = targetListName;
        Property = activeElement;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        _label.text = ParseNameString(activeElement.Name);

        _dropdown.AddOptions(GetListEntries());
        //SetValueWithoutNotify: only a genuine user selection should fire onValueChanged (which loads
        //the selected preset). Programmatic updates here and in HandleTargetChange must not.
        _dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedListIndex()));
        _dropdown.onValueChanged.AddListener((value) =>
        {
            RecordUndo();

            var entries = GetListEntries();
            if (value < 0 || value >= entries.Count)
                return;

            List<object> objParams = new List<object> { entries[value] };
            LinkedControllable.SetFieldProp(Property, objParams);
        });

        ApplyReadOnlyLook(_dropdown);
    }

    /// <summary>A dropdown over the members of an enum, taken from the member's own type.</summary>
    public void CreateUI(Controllable target, FieldInfo activeElement, Type _enumType, bool isInteractible)
    {
        Property = activeElement;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        enumType = _enumType;
        _enumNames = Enum.GetNames(enumType);
        _enumValues = Enum.GetValues(enumType);

        _label.text = ParseNameString(activeElement.Name);

        _dropdown.AddOptions(_enumNames.ToList());
        _dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedEnumIndex()));
        _dropdown.onValueChanged.AddListener((value) =>
        {
            RecordUndo();

            if (value < 0 || value >= _enumValues.Length)
                return;

            //The member itself, not its position: an enum numbered explicitly (Spot = 5) would
            //otherwise store whichever member happens to sit at that index.
            List<object> objParams = new List<object> { _enumValues.GetValue(value) };
            LinkedControllable.SetFieldProp(Property, objParams);
        });

        ApplyReadOnlyLook(_dropdown);
    }

    //The dropdown is not one of the fields GetInputFields returns, so the base pass does not reach it.
    //Its arrow goes with the frame: left alone it would still read as something to open.
    void ApplyReadOnlyLook(Dropdown dropdown)
    {
        if (IsInteractible)
            return;

        MakeDisplayOnly(dropdown);

        _arrow.gameObject.SetActive(false);
    }

    #endregion

    #region Selection

    List<string> GetListEntries()
    {
        return LinkedControllable.GetTargetList(TargetListName) ?? new List<string>();
    }

    int GetSelectedListIndex()
    {
        var current = Property.GetValue(LinkedControllable);
        return TypeConverter.GetIndexInEnum(GetListEntries(), current == null ? "" : current.ToString());
    }

    int GetSelectedEnumIndex()
    {
        return Array.IndexOf(_enumValues, Property.GetValue(LinkedControllable));
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        if (enumType != null)
        {
            _dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedEnumIndex()));
            return;
        }

        //The entries themselves can have changed - controllablePresetList grows every time a preset is saved.
        _dropdown.ClearOptions();
        _dropdown.AddOptions(GetListEntries());
        _dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedListIndex()));
    }

    #endregion
}
