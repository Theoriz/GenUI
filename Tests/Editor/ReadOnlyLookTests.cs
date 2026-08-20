using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers the display-only look every read-only widget shares. The transparent disabled tint used
    /// to come from one prefab happening to carry it, which is how read-only sliders and vectors ended
    /// up framed like editable ones; pinning it here keeps the look a decision rather than an asset.
    /// </summary>
    public class ReadOnlyLookTests
    {
        GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                Object.DestroyImmediate(_host);
        }

        T Create<T>() where T : Selectable
        {
            //Hidden and not saved, so the fixture never becomes part of the scene the user has open.
            _host = new GameObject("ReadOnlyLookTests", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
            return _host.AddComponent<T>();
        }

        [Test]
        public void MakeDisplayOnly_StopsInput()
        {
            var field = Create<InputField>();

            ControllableUI.MakeDisplayOnly(field);

            Assert.IsFalse(field.interactable);
        }

        [Test]
        public void MakeDisplayOnly_LeavesNoFrame()
        {
            var field = Create<InputField>();
            var colors = field.colors;
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            field.colors = colors;

            ControllableUI.MakeDisplayOnly(field);

            Assert.AreEqual(0f, field.colors.disabledColor.a,
                "A disabled field still tinted would draw a box around a value that cannot be edited.");
        }

        [Test]
        public void MakeDisplayOnly_AppliesToADropdownToo()
        {
            var dropdown = Create<Dropdown>();

            ControllableUI.MakeDisplayOnly(dropdown);

            Assert.IsFalse(dropdown.interactable);
            Assert.AreEqual(0f, dropdown.colors.disabledColor.a);
        }

        [Test]
        public void MakeDisplayOnly_IgnoresNothing()
        {
            Assert.DoesNotThrow(() => ControllableUI.MakeDisplayOnly(null));
        }
    }
}
