using System.Linq;
using NUnit.Framework;
using Theoriz.GenUI.Editor;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Panel appearance moved off OCF's Controllable and onto this optional component, so two things
    /// have to keep holding: a controllable without one still draws exactly as it did (which is what
    /// makes the component optional rather than required), and adding one preserves the colour the
    /// panel already had rather than resetting it to white.
    /// </summary>
    public class GenUIPanelSettingsTests
    {
        GameObject _go;
        Controllable _controllable;

        [SetUp]
        public void CreateControllable()
        {
            _go = new GameObject("panel-settings-test");
            _controllable = _go.AddComponent<Controllable>();
            _controllable.controllableId = "TestScript";
        }

        [TearDown]
        public void DestroyControllable()
        {
            Object.DestroyImmediate(_go);
        }

        #region DefaultBarColor

        [Test]
        public void DefaultBarColor_IsTheSameForTheSameId()
        {
            Assert.AreEqual(GenUIPanelSettings.DefaultBarColor("TestScript"),
                GenUIPanelSettings.DefaultBarColor("TestScript"),
                "The derived colour must not change between calls, or a panel changes colour between runs.");
        }

        [Test]
        public void DefaultBarColor_DiffersAcrossIds()
        {
            var ids = new[] { "GenUI", "TestScript", "ControllableMaster", "MyScript", "Camera" };
            var colors = ids.Select(GenUIPanelSettings.DefaultBarColor).ToArray();

            Assert.AreEqual(ids.Length, colors.Distinct().Count(),
                "Two of these ids derive the same colour, so their panels are no longer told apart: "
                + string.Join(", ", ids));
        }

        [Test]
        public void DefaultBarColor_IsOpaqueAndVisible()
        {
            var color = GenUIPanelSettings.DefaultBarColor("TestScript");

            Assert.AreEqual(1f, color.a, "A transparent bar would draw nothing.");
            Assert.Greater(color.maxColorComponent, 0.5f, "The bar has to read against the panel.");
        }

        [TestCase("")]
        [TestCase(null)]
        public void DefaultBarColor_IsWhiteWithoutAnId(string id)
        {
            Assert.AreEqual(Color.white, GenUIPanelSettings.DefaultBarColor(id));
        }

        #endregion

        #region Resolving a controllable's settings

        [Test]
        public void WithoutTheComponent_TheDefaultsApply()
        {
            Assert.IsTrue(GenUIPanelSettings.UsePanel(_controllable), "A controllable draws a panel unless told not to.");
            Assert.IsTrue(GenUIPanelSettings.ClosePanelAtStart(_controllable), "Panels start collapsed, as they always have.");
            Assert.AreEqual(GenUIPanelSettings.DefaultBarColor("TestScript"),
                GenUIPanelSettings.BarColorFor(_controllable),
                "Without the component the bar colour comes from the id, not from white.");
        }

        [Test]
        public void WithTheComponent_ItsValuesWin()
        {
            var settings = _go.AddComponent<GenUIPanelSettings>();
            settings.usePanel = false;
            settings.closePanelAtStart = false;
            settings.barColor = Color.green;

            Assert.IsFalse(GenUIPanelSettings.UsePanel(_controllable));
            Assert.IsFalse(GenUIPanelSettings.ClosePanelAtStart(_controllable));
            Assert.AreEqual(Color.green, GenUIPanelSettings.BarColorFor(_controllable));
        }

        #endregion

        #region Attaching

        [Test]
        public void Attach_SeedsTheColourThePanelAlreadyHad()
        {
            var settings = PanelSettingsAttacher.Attach(_controllable);

            Assert.IsNotNull(settings);
            Assert.AreEqual(GenUIPanelSettings.DefaultBarColor("TestScript"), settings.barColor,
                "Adding the component must not change how the panel looks; it only makes the colour editable.");
        }

        [Test]
        public void Attach_KeepsTheExistingComponent()
        {
            var first = PanelSettingsAttacher.Attach(_controllable);
            first.barColor = Color.green;

            var second = PanelSettingsAttacher.Attach(_controllable);

            Assert.AreSame(first, second, "A second call must not add a second component or reseed the colour.");
            Assert.AreEqual(Color.green, second.barColor);
        }

        #endregion
    }
}
