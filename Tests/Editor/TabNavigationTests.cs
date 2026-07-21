using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers the index arithmetic behind Tab / Shift+Tab. Wrapping at either end is where this kind
    /// of traversal goes wrong by one, and the mistake shows up as Tab appearing to stick on the last
    /// field rather than as an error.
    ///
    /// Focus, commit-on-blur and scroll-into-view need a running panel and are Editor checks.
    /// </summary>
    public class TabNavigationTests
    {
        [Test]
        public void NothingFocused_StartsAtEitherEnd()
        {
            Assert.AreEqual(0, UIMaster.NextIndex(-1, 5, backwards: false));
            Assert.AreEqual(4, UIMaster.NextIndex(-1, 5, backwards: true),
                "Shift+Tab with nothing focused should start at the last field.");
        }

        [Test]
        public void Forward_AdvancesAndWrapsAtTheEnd()
        {
            Assert.AreEqual(1, UIMaster.NextIndex(0, 5, backwards: false));
            Assert.AreEqual(4, UIMaster.NextIndex(3, 5, backwards: false));
            Assert.AreEqual(0, UIMaster.NextIndex(4, 5, backwards: false), "Past the last field wraps to the first.");
        }

        [Test]
        public void Backward_RetreatsAndWrapsAtTheStart()
        {
            Assert.AreEqual(3, UIMaster.NextIndex(4, 5, backwards: true));
            Assert.AreEqual(0, UIMaster.NextIndex(1, 5, backwards: true));
            Assert.AreEqual(4, UIMaster.NextIndex(0, 5, backwards: true), "Before the first field wraps to the last.");
        }

        [Test]
        public void SingleField_StaysPut()
        {
            Assert.AreEqual(0, UIMaster.NextIndex(0, 1, backwards: false));
            Assert.AreEqual(0, UIMaster.NextIndex(0, 1, backwards: true));
            Assert.AreEqual(0, UIMaster.NextIndex(-1, 1, backwards: false));
        }

        [Test]
        public void NoFields_ReturnsNoIndex()
        {
            Assert.AreEqual(-1, UIMaster.NextIndex(-1, 0, backwards: false));
            Assert.AreEqual(-1, UIMaster.NextIndex(0, 0, backwards: true));
        }
    }
}
