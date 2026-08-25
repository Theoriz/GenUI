using System.Globalization;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers the pure logic behind the colour picker - pointer mapping, HSV conversion and the hex
    /// field. The interaction itself, and whether the markers land where the pointer is, is an Editor
    /// check.
    /// </summary>
    public class GenUIColorPickerTests
    {
        static readonly Rect Area = new Rect(-50f, -50f, 100f, 100f);

        #region Pointer mapping

        [Test]
        public void CornersAndCentre_MapToTheirFractions()
        {
            Assert.AreEqual(Vector2.zero, GenUIColorPicker.NormalizedPoint(Area, new Vector2(-50f, -50f)));
            Assert.AreEqual(Vector2.one, GenUIColorPicker.NormalizedPoint(Area, new Vector2(50f, 50f)));

            var centre = GenUIColorPicker.NormalizedPoint(Area, Vector2.zero);
            Assert.AreEqual(0.5f, centre.x, 1e-6f);
            Assert.AreEqual(0.5f, centre.y, 1e-6f);
        }

        /// <summary>
        /// A drag that leaves the square must hold at the edge rather than run past it, or dragging
        /// off the picker would set a saturation above 1.
        /// </summary>
        [Test]
        public void PointsOutsideTheRect_AreClamped()
        {
            Assert.AreEqual(Vector2.zero, GenUIColorPicker.NormalizedPoint(Area, new Vector2(-500f, -500f)));
            Assert.AreEqual(Vector2.one, GenUIColorPicker.NormalizedPoint(Area, new Vector2(500f, 500f)));
        }

        //A rect has no size until the layout has run once, and dividing by it would report NaN.
        [Test]
        public void ZeroSizeRect_ReportsTheOrigin()
        {
            Assert.AreEqual(Vector2.zero, GenUIColorPicker.NormalizedPoint(new Rect(0f, 0f, 0f, 0f), new Vector2(5f, 5f)));
        }

        #endregion

        #region HSV

        [TestCase(0f, 1f, 1f, 1f)]
        [TestCase(0.33f, 0.5f, 0.75f, 0.5f)]
        [TestCase(0.66f, 1f, 0.2f, 1f)]
        [TestCase(0.9f, 0.1f, 1f, 0.25f)]
        public void HsvSurvivesARoundTrip(float h, float s, float v, float a)
        {
            var color = GenUIColorPicker.HsvToRgb(h, s, v, a);

            float rh, rs, rv;
            GenUIColorPicker.RgbToHsv(color, out rh, out rs, out rv);

            Assert.AreEqual(h, rh, 1e-3f, "hue");
            Assert.AreEqual(s, rs, 1e-3f, "saturation");
            Assert.AreEqual(v, rv, 1e-3f, "value");
            Assert.AreEqual(a, color.a, 1e-6f, "alpha carried through the conversion");
        }

        /// <summary>
        /// The reason the picker keeps HSVA rather than a Color: a grey reports hue 0, so pushing one
        /// in and reading it back through a Color would drop the hue the bar is sitting on.
        /// </summary>
        [Test]
        public void AGreyDoesNotTakeTheHueWithIt()
        {
            var picker = NewPicker();

            picker.SetColor(GenUIColorPicker.HsvToRgb(0.75f, 1f, 1f, 1f));
            picker.SetColor(new Color(0.5f, 0.5f, 0.5f, 1f));

            //The grey itself reports no hue, so what matters is the one the bar is still sitting on.
            Assert.AreEqual(0.75f, picker.Hue, 1e-3f, "The hue was thrown away by a colour that had none of its own.");
            Assert.AreEqual(0f, picker.Saturation, 1e-3f);
        }

        [Test]
        public void SetColorThenGetColor_ReturnsTheSameColor()
        {
            var picker = NewPicker();
            var color = new Color(0.2f, 0.6f, 0.9f, 0.4f);

            picker.SetColor(color);
            var read = picker.GetColor();

            Assert.AreEqual(color.r, read.r, 1e-3f);
            Assert.AreEqual(color.g, read.g, 1e-3f);
            Assert.AreEqual(color.b, read.b, 1e-3f);
            Assert.AreEqual(color.a, read.a, 1e-6f);
        }

        #endregion

        #region Channels

        [TestCase(0f, 0)]
        [TestCase(1f, 255)]
        [TestCase(0.5f, 128)]
        public void ChannelsAreShownAsBytes(float channel, int expected)
        {
            Assert.AreEqual(expected, GenUIColorPicker.ToByte(channel));
        }

        //Typing 300 into a box, or a negative, must not produce a colour outside 0..1.
        [Test]
        public void ChannelsOutsideTheByteRangeAreClamped()
        {
            Assert.AreEqual(255, GenUIColorPicker.ToByte(4f));
            Assert.AreEqual(0, GenUIColorPicker.ToByte(-1f));
            Assert.AreEqual(1f, GenUIColorPicker.FromByte(300), 1e-6f);
            Assert.AreEqual(0f, GenUIColorPicker.FromByte(-5), 1e-6f);
        }

        [Test]
        public void EveryByteSurvivesARoundTrip()
        {
            for (var value = 0; value <= 255; value++)
                Assert.AreEqual(value, GenUIColorPicker.ToByte(GenUIColorPicker.FromByte(value)),
                    "Byte " + value + " did not come back as itself.");
        }

        //The RGBA boxes and the hex field are two views of the same numbers, so they have to agree.
        [Test]
        public void TheChannelBoxesAgreeWithTheHexField()
        {
            Color color;
            Assert.IsTrue(GenUIColorPicker.TryParseHex("#3F7ABC", out color));

            Assert.AreEqual(0x3F, GenUIColorPicker.ToByte(color.r));
            Assert.AreEqual(0x7A, GenUIColorPicker.ToByte(color.g));
            Assert.AreEqual(0xBC, GenUIColorPicker.ToByte(color.b));
        }

        #endregion

        #region Hex

        [TestCase("#FF0000", 1f, 0f, 0f, 1f)]
        [TestCase("FF0000", 1f, 0f, 0f, 1f)]
        [TestCase("#F00", 1f, 0f, 0f, 1f)]
        [TestCase("#00FF0080", 0f, 1f, 0f, 0.5019608f)]
        [TestCase("  #0000ff  ", 0f, 0f, 1f, 1f)]
        public void ValidHexIsParsed(string text, float r, float g, float b, float a)
        {
            Color color;
            Assert.IsTrue(GenUIColorPicker.TryParseHex(text, out color), text + " should parse.");

            Assert.AreEqual(r, color.r, 1e-2f);
            Assert.AreEqual(g, color.g, 1e-2f);
            Assert.AreEqual(b, color.b, 1e-2f);
            Assert.AreEqual(a, color.a, 1e-2f);
        }

        //Colour names are rejected too: ColorUtility accepts them, but the field only ever shows hex,
        //so accepting "red" would let it display something it can never write back.
        [TestCase("")]
        [TestCase(null)]
        [TestCase("#")]
        [TestCase("12345")]
        [TestCase("#GGGGGG")]
        [TestCase("#FF00000")]
        [TestCase("red")]
        public void GarbageIsRejected(string text)
        {
            Color color;
            Assert.IsFalse(GenUIColorPicker.TryParseHex(text, out color), text + " should not parse.");
        }

        [Test]
        public void OpaqueColorsAreWrittenWithoutTheirAlpha()
        {
            Assert.AreEqual("#FF0000", GenUIColorPicker.ToHex(Color.red));
            Assert.AreEqual(9, GenUIColorPicker.ToHex(new Color(1f, 0f, 0f, 0.5f)).Length,
                "A colour that is not opaque is written as eight digits.");
        }

        [Test]
        public void HexSurvivesARoundTrip()
        {
            var color = new Color(0.13f, 0.42f, 0.87f, 0.66f);

            Color read;
            Assert.IsTrue(GenUIColorPicker.TryParseHex(GenUIColorPicker.ToHex(color), out read));

            //One byte per channel is all the hex form holds.
            Assert.AreEqual(color.r, read.r, 1f / 255f);
            Assert.AreEqual(color.g, read.g, 1f / 255f);
            Assert.AreEqual(color.b, read.b, 1f / 255f);
            Assert.AreEqual(color.a, read.a, 1f / 255f);
        }

        /// <summary>
        /// Hex is culture-independent, and has to stay so: a machine running under a French locale
        /// must read and write the same text as one running under an invariant culture.
        /// </summary>
        [Test]
        public void HexIsIndependentOfTheCulture()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

                Color color;
                Assert.IsTrue(GenUIColorPicker.TryParseHex("#3F7ABC", out color));
                Assert.AreEqual("#3F7ABC", GenUIColorPicker.ToHex(color));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        #endregion

        #region Fixture

        GameObject _parent;

        [SetUp]
        public void SetUp()
        {
            //Hidden and not saved, so the fixture never becomes part of the scene the user has open.
            _parent = new GameObject("GenUIColorPickerTests", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        }

        [TearDown]
        public void TearDown()
        {
            if (_parent != null)
                Object.DestroyImmediate(_parent);
        }

        GenUIColorPicker NewPicker()
        {
            return GenUIColorPicker.Build(_parent.transform);
        }

        #endregion
    }
}
