using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// The hand-written JSON both directions of the web protocol are built on: what it writes has to
    /// be what a browser parses, and what it reads has to survive whatever a browser sends.
    /// </summary>
    public class WebJsonTests
    {
        #region Writing

        [Test]
        public void AString_IsQuotedAndEscaped()
        {
            Assert.AreEqual("\"a \\\"b\\\" \\\\ c\"", WebJson.Quote("a \"b\" \\ c"));
            Assert.AreEqual("\"line\\nbreak\"", WebJson.Quote("line\nbreak"));
        }

        [Test]
        public void AControlCharacter_IsEscapedAsAUnicodeCode()
        {
            Assert.AreEqual("\"\\u0001\"", WebJson.Quote("\u0001"));
        }

        [Test]
        public void ANullString_IsAnEmptyOne()
        {
            Assert.AreEqual("\"\"", WebJson.Quote(null));
        }

        //A French editor would otherwise write 0,5 - which no browser parses as a number.
        [Test]
        public void Numbers_AreWrittenInInvariantCulture()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

                Assert.AreEqual("0.5", WebJson.Number(0.5f));
                Assert.AreEqual("[0.5,-1.25]", WebJson.Array(0.5f, -1.25f));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        //Neither has a JSON form, and a token a browser cannot parse would break the whole message.
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void AValueWithNoJsonForm_IsWrittenAsZero(float value)
        {
            Assert.AreEqual("0", WebJson.Number(value));
        }

        #endregion

        #region Reading

        [Test]
        public void AnObject_ParsesToItsMembers()
        {
            var parsed = WebJson.Parse("{\"t\":\"set\",\"id\":\"Test\",\"value\":0.25}");

            Assert.AreEqual("set", WebJson.AsString(WebJson.Member(parsed, "t")));
            Assert.AreEqual("Test", WebJson.AsString(WebJson.Member(parsed, "id")));

            float value;
            Assert.IsTrue(WebJson.TryGetFloat(WebJson.Member(parsed, "value"), out value));
            Assert.AreEqual(0.25f, value);
        }

        [Test]
        public void AnArray_ParsesToAList()
        {
            var items = WebJson.Parse("[1, 2.5, -3]") as List<object>;

            Assert.IsNotNull(items);
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(2.5d, items[1]);
        }

        [Test]
        public void NestedValues_ParseThroughToTheLeaves()
        {
            var parsed = WebJson.Parse("{\"a\":{\"b\":[true,null,\"c\"]}}");
            var inner = WebJson.Member(WebJson.Member(parsed, "a"), "b") as List<object>;

            Assert.IsNotNull(inner);
            Assert.AreEqual(true, inner[0]);
            Assert.IsNull(inner[1]);
            Assert.AreEqual("c", inner[2]);
        }

        [Test]
        public void Escapes_AreUndoneOnTheWayBack()
        {
            Assert.AreEqual("a\"b\\c\nd\u00e9", WebJson.Parse("\"a\\\"b\\\\c\\nd\\u00e9\""));
        }

        [Test]
        public void AnEmptyObjectAndArray_Parse()
        {
            Assert.IsNotNull(WebJson.Parse("{}") as Dictionary<string, object>);
            Assert.IsNotNull(WebJson.Parse("[]") as List<object>);
        }

        //Anything a browser can send has to come back as "not usable" rather than as an exception.
        [TestCase("")]
        [TestCase("{")]
        [TestCase("{\"a\":}")]
        [TestCase("{\"a\" 1}")]
        [TestCase("[1,]")]
        [TestCase("\"unterminated")]
        [TestCase("tru")]
        [TestCase("{\"a\":1} trailing")]
        public void MalformedJson_IsRefused(string text)
        {
            Assert.IsNull(WebJson.Parse(text));
        }

        [Test]
        public void AMissingMember_IsNull()
        {
            Assert.IsNull(WebJson.Member(WebJson.Parse("{\"a\":1}"), "b"));
            Assert.IsNull(WebJson.Member("not an object", "a"));
        }

        //Input fields send what they hold, so a number can arrive spelled out.
        [Test]
        public void ANumberWrittenAsText_IsStillANumber()
        {
            float value;
            Assert.IsTrue(WebJson.TryGetFloat("1.5", out value));
            Assert.AreEqual(1.5f, value);
        }

        [Test]
        public void SomethingThatIsNoNumber_IsRefused()
        {
            float value;
            Assert.IsFalse(WebJson.TryGetFloat("abc", out value));
            Assert.IsFalse(WebJson.TryGetFloat(null, out value));
        }

        #endregion
    }
}
