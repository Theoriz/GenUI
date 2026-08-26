using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Theoriz.GenUI.Tests
{
    /// <summary>
    /// PlayMode tests for the hierarchies the widgets build themselves.
    /// </summary>
    /// <remarks>
    /// These could not exist while the structure lived in prefabs: a widget only became whole once a
    /// prefab was instantiated, and the prefabs were what the assertions would have had to trust.
    /// Now the structure is created by the same file that reads it back, so what a built widget
    /// reports is testable without a scene, a Controllable or a UIMaster.
    /// </remarks>
    public class WidgetBuildTests
    {
        GameObject _parent;

        [SetUp]
        public void SetUp()
        {
            //Hidden and not saved, so the fixture never becomes part of the scene the user has open.
            _parent = new GameObject("WidgetBuildTests", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        }

        [TearDown]
        public void TearDown()
        {
            //Qualified: this file also uses System, where Object means something else.
            if (_parent != null)
                UnityEngine.Object.DestroyImmediate(_parent);
        }

        T Build<T>() where T : ControllableUI
        {
            return ControllableUI.Create<T>(_parent.transform);
        }

        //Create is generic on the widget type, which a [TestCase] cannot express; this is the only
        //way to run one case per widget rather than thirteen near-identical test methods.
        ControllableUI Build(Type widgetType)
        {
            var create = typeof(ControllableUI).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            return (ControllableUI)create.MakeGenericMethod(widgetType).Invoke(null, new object[] { _parent.transform });
        }

        #region Editable fields

        //The counts Tab navigation walks and the read-only pass covers. A widget that silently built
        //one field too few would still look right and would drop out of the Tab sequence.
        [TestCase(typeof(HeaderUI), 0)]
        [TestCase(typeof(TooltipUI), 0)]
        [TestCase(typeof(ButtonUI), 0)]
        [TestCase(typeof(ToggleUI), 0)]
        [TestCase(typeof(ColorUI), 0)]
        [TestCase(typeof(DropdownUI), 0)]
        [TestCase(typeof(InputFieldUI), 1)]
        [TestCase(typeof(SliderUI), 1)]
        [TestCase(typeof(Vector2UI), 2)]
        [TestCase(typeof(Vector2IntUI), 2)]
        [TestCase(typeof(Vector3UI), 3)]
        [TestCase(typeof(Vector3IntUI), 3)]
        [TestCase(typeof(Vector4UI), 4)]
        public void Build_ReportsItsEditableFields(Type widgetType, int expected)
        {
            var fields = Build(widgetType).GetInputFields();

            Assert.AreEqual(expected, fields.Length, widgetType.Name + " built the wrong number of fields.");
            CollectionAssert.DoesNotContain(fields, null, widgetType.Name + " left a field unbuilt.");
        }

        //Tab and the scrub labels both depend on this order; nothing else pins it now that the axes
        //are built in a loop rather than found by name.
        [Test]
        public void Vector4_ReportsItsAxesInOrder()
        {
            var targets = Build<Vector4UI>().GetScrubTargets();

            Assert.AreEqual(4, targets.Length);
            Assert.AreEqual("x", targets[0].Label.text);
            Assert.AreEqual("y", targets[1].Label.text);
            Assert.AreEqual("z", targets[2].Label.text);
            Assert.AreEqual("w", targets[3].Label.text);
        }

        //Every field is built with the transparent disabled state ApplyReadOnlyLook relies on. This
        //used to come from one prefab happening to carry it, which is how read-only sliders and
        //vectors ended up framed like editable ones.
        [Test]
        public void Fields_AreBuiltWithATransparentDisabledTint()
        {
            foreach (var field in Build<Vector3UI>().GetInputFields())
                Assert.AreEqual(0f, field.colors.disabledColor.a);
        }

        #endregion

        #region Mouse events

        //The invariant UIMaster.BindMouseEvents used to repair after the fact: a MouseButtonEvent
        //whose linkedUI is empty silently does nothing when right-clicked.
        [TestCase(typeof(ButtonUI))]
        [TestCase(typeof(ToggleUI))]
        [TestCase(typeof(ColorUI))]
        [TestCase(typeof(InputFieldUI))]
        [TestCase(typeof(SliderUI))]
        [TestCase(typeof(DropdownUI))]
        [TestCase(typeof(Vector3UI))]
        public void Build_LinksEveryMouseEventToItsWidget(Type widgetType)
        {
            var built = Build(widgetType);

            var events = built.GetComponentsInChildren<MouseButtonEvent>(true);

            Assert.IsNotEmpty(events, widgetType.Name + " has nothing to right-click.");
            foreach (var mouseEvent in events)
                Assert.AreSame(built, mouseEvent.linkedUI, widgetType.Name + " left a MouseButtonEvent unlinked.");
        }

        //What makes the right-click menu behave the same on every row, whatever the member type: the
        //event is on the row itself, over a graphic covering it, so it catches the presses the
        //widget's own controls do not take - over the label, or over the gap beside it. While each
        //widget added its own, a bool row had none that ever fired and a slider's label had one that
        //could not: pointer-up only reaches the object that took the press.
        [TestCase(typeof(HeaderUI))]
        [TestCase(typeof(TooltipUI))]
        [TestCase(typeof(ButtonUI))]
        [TestCase(typeof(ToggleUI))]
        [TestCase(typeof(ColorUI))]
        [TestCase(typeof(InputFieldUI))]
        [TestCase(typeof(SliderUI))]
        [TestCase(typeof(DropdownUI))]
        [TestCase(typeof(Vector3UI))]
        public void Build_PutsAMouseEventOnTheRowItself(Type widgetType)
        {
            var built = Build(widgetType);

            Assert.IsNotNull(built.GetComponent<MouseButtonEvent>(),
                widgetType.Name + " leaves its own row unclickable.");
            Assert.IsNotNull(built.GetComponent<Graphic>(),
                widgetType.Name + " has no graphic on its row, so its bare parts are not raycast.");
        }

        //The swatch is the control of the colour row, so it is the only part that opens the picker: a
        //left click on the label or beside it must do nothing, like it does on every other row.
        [Test]
        public void ColorWidget_OpensThePickerFromTheSwatchOnly()
        {
            var built = Build<ColorUI>();
            var events = built.GetComponentsInChildren<MouseButtonEvent>(true);

            Assert.IsTrue(Array.Exists(events, e => e.enableColorPicker && e.name == "Swatch"),
                "The colour swatch would not open the picker on a left click.");
            Assert.IsFalse(built.GetComponent<MouseButtonEvent>().enableColorPicker,
                "A left click anywhere on the colour row opens the picker, not just on the swatch.");
        }

        #endregion

        #region Dropdown template

        //The Dropdown clones its template every time it opens, so a template left active would draw
        //an open list permanently over the panel below it.
        [Test]
        public void Dropdown_BuildsATemplateAndLeavesItInactive()
        {
            var dropdown = Build<DropdownUI>().GetComponentInChildren<Dropdown>(true);

            Assert.IsNotNull(dropdown.template, "The dropdown has no template to clone.");
            Assert.IsFalse(dropdown.template.gameObject.activeSelf, "The template must not be a live part of the panel.");
            Assert.IsNotNull(dropdown.captionText);
            Assert.IsNotNull(dropdown.itemText);
        }

        //Unity's Dropdown opens on any button, and its list is drawn above everything - so a stock one
        //would show that list over the right-click menu the same click opens.
        [Test]
        public void Dropdown_OnlyOpensOnTheLeftButton()
        {
            var dropdown = Build<DropdownUI>().GetComponentInChildren<Dropdown>(true);

            Assert.IsInstanceOf<GenUIDropdown>(dropdown, "The dropdown would open on a right click.");
        }

        #endregion

        #region Colour picker

        //The picker is built from UIFactory like every widget rather than instantiated from a prefab,
        //so what it is made of is testable here too.
        [Test]
        public void ColorPicker_BuildsItsBackdropAndItsParts()
        {
            var picker = ColorPicker.Build(_parent.transform);

            Assert.IsNotNull(picker.closeButton, "Nothing behind the picker would dismiss it.");
            Assert.IsNotNull(picker.colorPicker, "The picker itself was not built.");
            Assert.AreSame(picker.colorPicker.transform, picker.Content,
                "Content is what UIMaster moves to the pointer.");

            //The SV square, the hue ramp, the alpha checkerboard and the alpha ramp.
            var images = picker.GetComponentsInChildren<RawImage>(true);
            Assert.AreEqual(4, images.Length, "The picker draws its areas from procedural textures.");
            foreach (var image in images)
                Assert.IsNotNull(image.texture, "An area was built with no texture behind it.");

            //Four channel boxes and the hex field.
            Assert.AreEqual(5, picker.GetComponentsInChildren<InputField>(true).Length,
                "The picker builds an RGBA row and a hex field.");
        }

        [Test]
        public void ColorPicker_RoundTripsAColorThroughItsHsvState()
        {
            var picker = ColorPicker.Build(_parent.transform).colorPicker;
            var color = new Color(0.2f, 0.6f, 0.9f, 0.4f);

            picker.SetColor(color);

            Assert.AreEqual(color.r, picker.GetColor().r, 1e-3f);
            Assert.AreEqual(color.g, picker.GetColor().g, 1e-3f);
            Assert.AreEqual(color.b, picker.GetColor().b, 1e-3f);
            Assert.AreEqual(color.a, picker.GetColor().a, 1e-6f);
        }

        //Its textures are created in code, so nothing else releases them when the picker goes.
        [Test]
        public void ColorPicker_DestroysCleanly()
        {
            var picker = ColorPicker.Build(_parent.transform);

            UnityEngine.Object.DestroyImmediate(picker.gameObject);

            Assert.IsTrue(picker == null, "The picker survived being destroyed.");
        }

        #endregion

        #region Panel

        [Test]
        public void Panel_BuildsItsRootAndPresetSection()
        {
            var panel = PanelUI.Build(_parent.transform, "Test Panel", Color.red);

            Assert.AreSame(_parent.transform, panel.Root.transform.parent,
                "The panel's root is what the caller parents, orders and destroys.");
            Assert.IsNotNull(panel.PresetHolder, "The preset controls have nowhere to be gathered.");
            Assert.AreSame(panel.PresetSection, panel.PresetHolder.parent,
                "The preset row is drawn inside the preset section.");
            Assert.AreSame(panel.transform, panel.PresetSection.parent,
                "The preset section belongs to the panel body, which is what folding hides.");
        }

        [Test]
        public void Panel_SizesAPresetSectionToItsRows()
        {
            var panel = PanelUI.Build(_parent.transform, "Test Panel", Color.red);

            //The separator sits in the section's gap and must not be counted as a row of its own.
            var expected = GenUIStyle.PresetRowHeight + GenUIStyle.PresetSectionGap
                + 2f * GenUIStyle.PresetSectionPadding;
            Assert.AreEqual(expected, panel.PresetSection.rect.height, 0.01f,
                "A section holding one row is as tall as that row plus its gap and padding.");
        }

        #endregion
    }
}
