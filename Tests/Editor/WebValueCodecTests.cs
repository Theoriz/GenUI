using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    public enum CodecMode { First, Second }

    /// <summary>
    /// One member per supported type, each carrying [OCFProperty] so SetFieldProp writes it - which is
    /// what makes the round trip below go through the real inbound path rather than past it.
    /// </summary>
    public class CodecFixture : Controllable
    {
        [OCFProperty] public float speed;
        [OCFProperty] public int count;
        [OCFProperty] public bool toggled;
        [OCFProperty] public string text;
        [OCFProperty] public Color color;
        [OCFProperty] public Vector2 v2;
        [OCFProperty] public Vector2Int v2i;
        [OCFProperty] public Vector3 v3;
        [OCFProperty] public Vector3Int v3i;
        [OCFProperty] public Vector4 v4;
        [OCFProperty] public CodecMode mode;
        [OCFProperty(readOnly = true)] public float locked;
    }

    /// <summary>
    /// Every supported type out to the browser as JSON and back into the member it came from. The
    /// return leg goes through <c>SetFieldProp</c>, so these also pin that the codec hands back the
    /// argument list OCF actually expects.
    /// </summary>
    public class WebValueCodecTests
    {
        GameObject _go;
        CodecFixture _controllable;

        [SetUp]
        public void CreateControllable()
        {
            _go = new GameObject("web-value-codec-test") { hideFlags = HideFlags.HideAndDontSave };
            _controllable = _go.AddComponent<CodecFixture>();
            _controllable.controllableId = "Fixture";
        }

        [TearDown]
        public void DestroyControllable()
        {
            if (_go != null)
                UnityEngine.Object.DestroyImmediate(_go);
        }

        static FieldInfo Field(string member)
        {
            return typeof(CodecFixture).GetField(member, BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Writes <paramref name="value"/>, sends it out as JSON, clears the member, and puts the JSON
        /// back through the inbound path. Clearing in between is what proves the value travelled.
        /// </summary>
        void RoundTrip(string member, object value, object cleared)
        {
            var field = Field(member);
            field.SetValue(_controllable, value);

            var json = WebValueCodec.ToJson(field.FieldType, field.GetValue(_controllable));
            Assert.IsNotNull(json, member + " has no JSON form.");

            field.SetValue(_controllable, cleared);

            List<object> values;
            Assert.IsTrue(WebValueCodec.TryReadValues(field.FieldType, WebJson.Parse(json), out values),
                json + " did not read back.");

            _controllable.SetFieldProp(field, values);

            Assert.AreEqual(value, field.GetValue(_controllable));
        }

        #region Round trips

        [Test] public void AFloat_RoundTrips() { RoundTrip("speed", 0.25f, 0f); }
        [Test] public void AnInt_RoundTrips() { RoundTrip("count", 7, 0); }
        [Test] public void ABool_RoundTrips() { RoundTrip("toggled", true, false); }
        [Test] public void AString_RoundTrips() { RoundTrip("text", "a \"quoted\" value", ""); }
        [Test] public void AColor_RoundTrips() { RoundTrip("color", new Color(0.25f, 0.5f, 0.75f, 0.5f), Color.black); }
        [Test] public void AVector2_RoundTrips() { RoundTrip("v2", new Vector2(1.5f, -2.5f), Vector2.zero); }
        [Test] public void AVector2Int_RoundTrips() { RoundTrip("v2i", new Vector2Int(3, -4), Vector2Int.zero); }
        [Test] public void AVector3_RoundTrips() { RoundTrip("v3", new Vector3(1.5f, -2.5f, 3.25f), Vector3.zero); }
        [Test] public void AVector3Int_RoundTrips() { RoundTrip("v3i", new Vector3Int(3, -4, 5), Vector3Int.zero); }
        [Test] public void AVector4_RoundTrips() { RoundTrip("v4", new Vector4(1f, 2f, 3f, 4f), Vector4.zero); }
        [Test] public void AnEnum_RoundTrips() { RoundTrip("mode", CodecMode.Second, CodecMode.First); }

        #endregion

        #region What goes on the wire

        [Test]
        public void AnEnum_TravelsAsItsMemberName()
        {
            _controllable.mode = CodecMode.Second;
            Assert.AreEqual("\"Second\"", WebValueCodec.ToJson(typeof(CodecMode), _controllable.mode));
        }

        [Test]
        public void AColor_TravelsAsFourNumbers()
        {
            Assert.AreEqual("[0,0.5,1,1]", WebValueCodec.ToJson(typeof(Color), new Color(0f, 0.5f, 1f, 1f)));
        }

        //The panel draws no widget for these either, so there is nothing for the browser to show.
        [Test]
        public void AnUnsupportedType_HasNoJson()
        {
            Assert.IsNull(WebValueCodec.ToJson(typeof(Quaternion), Quaternion.identity));
        }

        #endregion

        #region What is accepted coming back

        //An input field sends its text, so a number spelled out is the ordinary case, not an oddity.
        [Test]
        public void ANumberSentAsText_IsAccepted()
        {
            List<object> values;
            Assert.IsTrue(WebValueCodec.TryReadValues(typeof(float), WebJson.Parse("\"1.5\""), out values));

            var field = Field("speed");
            _controllable.SetFieldProp(field, values);
            Assert.AreEqual(1.5f, _controllable.speed);
        }

        //Alpha is the one component SetFieldProp can fill in for itself.
        [Test]
        public void AThreeComponentColor_IsAcceptedAndOpaque()
        {
            List<object> values;
            Assert.IsTrue(WebValueCodec.TryReadValues(typeof(Color), WebJson.Parse("[1,0,0]"), out values));

            _controllable.SetFieldProp(Field("color"), values);
            Assert.AreEqual(new Color(1f, 0f, 0f, 1f), _controllable.color);
        }

        //Half a vector would be written through as a value nobody asked for.
        [Test]
        public void AnIncompleteVector_IsRefused()
        {
            List<object> values;
            Assert.IsFalse(WebValueCodec.TryReadValues(typeof(Vector3), WebJson.Parse("[1,2]"), out values));
        }

        [Test]
        public void SomethingThatIsNoValue_IsRefused()
        {
            List<object> values;
            Assert.IsFalse(WebValueCodec.TryReadValues(typeof(float), WebJson.Parse("\"abc\""), out values));
            Assert.IsFalse(WebValueCodec.TryReadValues(typeof(Vector2), WebJson.Parse("3"), out values));
            Assert.IsFalse(WebValueCodec.TryReadValues(typeof(Quaternion), WebJson.Parse("[0,0,0,1]"), out values));
            Assert.IsFalse(WebValueCodec.TryReadValues(typeof(float), null, out values));
        }

        //Refusal is the codec's job only for values it cannot read; read-only is SetFieldProp's.
        [Test]
        public void AReadOnlyMember_IsNotWritten()
        {
            List<object> values;
            Assert.IsTrue(WebValueCodec.TryReadValues(typeof(float), WebJson.Parse("2"), out values));

            _controllable.SetFieldProp(Field("locked"), values);
            Assert.AreEqual(0f, _controllable.locked);
        }

        #endregion
    }
}
