using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers the placement rule that keeps a popup opened near a screen edge fully visible. Screen
    /// coordinates, so y grows upwards.
    /// </summary>
    public class ScreenNudgeTests
    {
        static readonly Vector2 Screen1080p = new Vector2(1920f, 1080f);

        static Vector2 Nudge(float x, float y, float width, float height)
        {
            var min = new Vector2(x, y);
            return UIFactory.ScreenNudge(min, min + new Vector2(width, height), Screen1080p);
        }

        [Test]
        public void APopupAlreadyOnScreen_IsNotMoved()
        {
            Assert.AreEqual(Vector2.zero, Nudge(800f, 400f, 300f, 200f));
        }

        [Test]
        public void APopupTouchingAnEdgeExactly_IsNotMoved()
        {
            Assert.AreEqual(Vector2.zero, Nudge(0f, 0f, 1920f, 1080f));
        }

        [Test]
        public void APopupOverTheRightEdge_IsPushedLeft()
        {
            Assert.AreEqual(new Vector2(-100f, 0f), Nudge(1720f, 400f, 300f, 200f));
        }

        [Test]
        public void APopupOverTheLeftEdge_IsPushedRight()
        {
            Assert.AreEqual(new Vector2(50f, 0f), Nudge(-50f, 400f, 300f, 200f));
        }

        [Test]
        public void APopupOverTheTopEdge_IsPushedDown()
        {
            Assert.AreEqual(new Vector2(0f, -120f), Nudge(800f, 1000f, 300f, 200f));
        }

        [Test]
        public void APopupBelowTheBottomEdge_IsPushedUp()
        {
            Assert.AreEqual(new Vector2(0f, 80f), Nudge(800f, -80f, 300f, 200f));
        }

        [Test]
        public void ACornerOverflow_IsResolvedOnBothAxes()
        {
            Assert.AreEqual(new Vector2(-100f, -120f), Nudge(1720f, 1000f, 300f, 200f));
        }

        //Nothing can make an oversized popup fit, so the corner it is pinned by is the one carrying
        //its first controls.
        [Test]
        public void APopupWiderThanTheScreen_KeepsItsLeftEdgeVisible()
        {
            Assert.AreEqual(new Vector2(200f, 0f), Nudge(-200f, 400f, 2400f, 200f));
        }

        [Test]
        public void APopupTallerThanTheScreen_KeepsItsTopEdgeVisible()
        {
            Assert.AreEqual(new Vector2(0f, -100f), Nudge(0f, -200f, 300f, 1380f));
        }
    }
}
