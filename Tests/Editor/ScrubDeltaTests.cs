using NUnit.Framework;
using UnityEngine;

//NUnit declares a RangeAttribute too, so the one meant here has to be named explicitly.
using RangeAttribute = UnityEngine.RangeAttribute;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers the pixels-to-value conversion behind drag-to-scrub. The interaction itself - which
    /// label claims the drag, whether the panel still scrolls, what reaches OSC - is an Editor check.
    /// </summary>
    public class ScrubDeltaTests
    {
        const float NoScale = 1f;

        [Test]
        public void DraggingRightRaises_AndLeftLowers()
        {
            Assert.Greater(DragValueUI.ScrubDelta(100f, NoScale, false, null, false, false), 0);
            Assert.Less(DragValueUI.ScrubDelta(-100f, NoScale, false, null, false, false), 0);
        }

        [Test]
        public void NoMovement_ChangesNothing()
        {
            Assert.AreEqual(0, DragValueUI.ScrubDelta(0f, NoScale, false, null, false, false));
            Assert.AreEqual(0, DragValueUI.ScrubDelta(0f, NoScale, true, new RangeAttribute(0, 1), true, false));
        }

        /// <summary>
        /// A ranged member should feel like its own slider: the documented drag distance covers the
        /// whole span, whatever that span is.
        /// </summary>
        [Test]
        public void RangedMember_CrossesItsWholeSpanOverTheDragDistance()
        {
            var small = DragValueUI.ScrubDelta(400f, NoScale, false, new RangeAttribute(0f, 1f), false, false);
            var large = DragValueUI.ScrubDelta(400f, NoScale, false, new RangeAttribute(-500f, 500f), false, false);

            Assert.AreEqual(1.0, small, 1e-6);
            Assert.AreEqual(1000.0, large, 1e-6,
                "The same drag should cross the full span regardless of how wide the range is.");
        }

        [Test]
        public void UnboundedInt_NeedsSeveralPixelsPerStep()
        {
            //Fewer pixels than one step must not already be a whole unit, or small drags jump.
            Assert.Less(DragValueUI.ScrubDelta(5f, NoScale, true, null, false, false), 1.0);
            Assert.AreEqual(1.0, DragValueUI.ScrubDelta(10f, NoScale, true, null, false, false), 1e-6);
        }

        [Test]
        public void ShiftIsCoarse_AndCtrlIsFine()
        {
            var plain = DragValueUI.ScrubDelta(100f, NoScale, false, null, false, false);
            var coarse = DragValueUI.ScrubDelta(100f, NoScale, false, null, true, false);
            var fine = DragValueUI.ScrubDelta(100f, NoScale, false, null, false, true);

            Assert.AreEqual(plain * 10.0, coarse, 1e-6);
            Assert.AreEqual(plain * 0.1, fine, 1e-6);
        }

        /// <summary>
        /// The UI can be zoomed with PageUp, which scales the canvas. The same physical mouse travel
        /// must move the value by the same amount at any zoom.
        /// </summary>
        [Test]
        public void CanvasScale_CancelsOut()
        {
            var atOneX = DragValueUI.ScrubDelta(100f, 1f, false, null, false, false);
            var atTwoX = DragValueUI.ScrubDelta(100f, 2f, false, null, false, false);

            Assert.AreEqual(atOneX / 2.0, atTwoX, 1e-6);
        }

        [Test]
        public void InvalidCanvasScale_IsTreatedAsUnscaled()
        {
            Assert.AreEqual(DragValueUI.ScrubDelta(100f, 1f, false, null, false, false),
                            DragValueUI.ScrubDelta(100f, 0f, false, null, false, false), 1e-6);
        }
    }
}
