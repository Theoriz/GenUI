using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Widget and button labels are derived from the member name. OCF 2.0.0 prefixed every member
    /// Controllable declares, so without the strip the preset row reads "Controllable Save" and
    /// "Controllable Current Preset" — a visible regression that nothing else would catch, since the
    /// buttons still work and are still grouped correctly.
    ///
    /// The prefix is spelled both ways and both must strip: methods are "ControllableSave", while
    /// fields are serialized and keep the "controllableCurrentPreset" spelling they were given.
    /// </summary>
    public class ParseNameStringTests
    {
        GameObject _go;
        ControllableUI _ui;

        [SetUp]
        public void CreateWidget()
        {
            _go = new GameObject("parse-name-test");
            _ui = _go.AddComponent<ControllableUI>();
        }

        [TearDown]
        public void DestroyWidget()
        {
            Object.DestroyImmediate(_go);
        }

        [TestCase("ControllableSave", "Save")]
        [TestCase("ControllableSaveAs", "Save As")]
        [TestCase("ControllableLoad", "Load")]
        [TestCase("ControllableShow", "Show")]
        [TestCase("ControllableSaveAll", "Save All")]
        [TestCase("ControllableOpenPresetsFolder", "Open Presets Folder")]
        [TestCase("ControllableLoadWithName", "Load With Name")]
        [TestCase("controllableCurrentPreset", "Current Preset")]
        [TestCase("controllableBarColor", "Bar Color")]
        [TestCase("controllableTargetScript", "Target Script")]
        public void PrefixedMembers_KeepTheLabelTheyHadBeforeThePrefix(string member, string expected)
        {
            Assert.AreEqual(expected, _ui.ParseNameString(member));
        }

        [TestCase("speed", "Speed")]
        [TestCase("myValue", "My Value")]
        [TestCase("RandomizeColor", "Randomize Color")]
        public void UserMembers_AreUnaffected(string member, string expected)
        {
            Assert.AreEqual(expected, _ui.ParseNameString(member));
        }

        /// <summary>
        /// The strip requires an upper-case character after the prefix. Without that test a user
        /// member merely starting with those letters would silently lose them from its label.
        /// </summary>
        [TestCase("controllablething", "Controllablething")]
        [TestCase("Controllablething", "Controllablething")]
        [TestCase("controllable", "Controllable")]
        [TestCase("Controllable", "Controllable")]
        [TestCase("control", "Control")]
        public void NamesThatOnlyLookPrefixed_KeepTheirLetters(string member, string expected)
        {
            Assert.AreEqual(expected, _ui.ParseNameString(member));
        }

        [Test]
        public void EmptyAndNull_AreReturnedUnchanged()
        {
            Assert.AreEqual("", _ui.ParseNameString(""));
            Assert.IsNull(_ui.ParseNameString(null));
        }
    }
}
