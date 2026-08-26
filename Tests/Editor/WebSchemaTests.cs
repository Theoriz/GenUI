using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    public enum SchemaMode { First, Second }

    public class SchemaTarget : MonoBehaviour
    {
        public float speed = 0.5f;
        public float ranged = 2f;
        public bool toggled;
        public Color tint;
        public SchemaMode mode;
        public string chosen = "two";
        public List<string> choices = new List<string> { "one", "two" };
        public Quaternion rotation;
        public float hidden;
        public float locked;

        public void Ping() { }
        public void WithArgument(int amount) { }

        //Declared here as well as on the mirror, which is what a generated mirror looks like: the
        //options stay readable through ClassMethodInfo even though the target's method is the one
        //that gets invoked.
        public void HiddenMethod() { }
    }

    /// <summary>
    /// The mirror as the generator would emit it: same names as the target, the drawing attributes on
    /// this side.
    /// </summary>
    public class SchemaMirror : Controllable
    {
        [OCFProperty] public float speed;

        [Header("Numbers")]
        [Tooltip("How fast")]
        [UnityEngine.Range(0f, 10f)]
        [OCFProperty] public float ranged;

        [OCFProperty] public bool toggled;
        [OCFProperty] public Color tint;
        [OCFProperty] public SchemaMode mode;
        [OCFProperty(targetList = "choices")] public string chosen;
        [OCFProperty] public Quaternion rotation;
        [OCFProperty(showInUI = false)] public float hidden;
        [OCFProperty(readOnly = true)] public float locked;

        [OCFMethod] public void Ping() { }
        [OCFMethod] public void WithArgument(int amount) { }
        [OCFMethod(showInUI = false)] public void HiddenMethod() { }
    }

    /// <summary>
    /// What the browser is told to draw. The point of these is that the schema says what the panel
    /// builds - same widget decision, same members left out, same buttons.
    /// </summary>
    public class WebSchemaTests
    {
        GameObject _go;
        SchemaMirror _mirror;

        [SetUp]
        public void CreateControllable()
        {
            _go = new GameObject("web-schema-test") { hideFlags = HideFlags.HideAndDontSave };

            var target = _go.AddComponent<SchemaTarget>();
            _mirror = _go.AddComponent<SchemaMirror>();
            _mirror.controllableTargetScript = target;
            _mirror.controllableId = "Schema";

            //Presets read and write files from OnEnable, which these tests have no use for.
            _mirror.controllableUsePresets = false;

            //Nothing calls Awake on a component added in the Editor, and it is what binds the mirror.
            _mirror.Awake();
        }

        [TearDown]
        public void DestroyControllable()
        {
            ControllableMaster.UnRegister(_mirror);

            if (_go != null)
                UnityEngine.Object.DestroyImmediate(_go);
        }

        #region Helpers

        object Parsed()
        {
            return WebJson.Parse(WebSchema.ControllableJson(_mirror));
        }

        static List<object> List(object node, string name)
        {
            var items = WebJson.Member(node, name) as List<object>;
            Assert.IsNotNull(items, "No '" + name + "' array in the schema.");

            return items;
        }

        object Member(string name)
        {
            foreach (var member in List(Parsed(), "members"))
            {
                if (WebJson.AsString(WebJson.Member(member, "name")) == name)
                    return member;
            }

            return null;
        }

        static string Text(object node, string name)
        {
            return WebJson.AsString(WebJson.Member(node, name));
        }

        static float Number(object node, string name)
        {
            float value;
            Assert.IsTrue(WebJson.TryGetFloat(WebJson.Member(node, name), out value),
                "No number in '" + name + "'.");

            return value;
        }

        #endregion

        #region Members

        //The kinds are MemberDescriptor's, not the schema's - the panel builds from the same call.
        [TestCase("speed", "Input")]
        [TestCase("ranged", "Slider")]
        [TestCase("toggled", "Toggle")]
        [TestCase("tint", "Color")]
        [TestCase("mode", "Dropdown")]
        [TestCase("chosen", "Dropdown")]
        public void EveryDrawnMember_CarriesItsKind(string member, string kind)
        {
            Assert.AreEqual(kind, Text(Member(member), "kind"));
        }

        [Test]
        public void ASlider_CarriesItsBoundsAndLabel()
        {
            var ranged = Member("ranged");

            Assert.AreEqual(0f, Number(ranged, "min"));
            Assert.AreEqual(10f, Number(ranged, "max"));
            Assert.AreEqual("Ranged", Text(ranged, "label"));
            Assert.AreEqual("Numbers", Text(ranged, "header"));
            Assert.AreEqual("How fast", Text(ranged, "tooltip"));
        }

        [Test]
        public void AnEnumDropdown_CarriesItsMemberNames()
        {
            CollectionAssert.AreEqual(new[] { "First", "Second" },
                Options(Member("mode")));
        }

        [Test]
        public void AListDropdown_CarriesTheListsEntries()
        {
            CollectionAssert.AreEqual(new[] { "one", "two" }, Options(Member("chosen")));
        }

        static List<string> Options(object member)
        {
            var options = new List<string>();
            foreach (var option in List(member, "options"))
                options.Add(WebJson.AsString(option));

            return options;
        }

        [Test]
        public void CurrentValues_RideAlongWithTheSchema()
        {
            Assert.AreEqual(0.5f, Number(Member("speed"), "value"));
            Assert.AreEqual("two", Text(Member("chosen"), "value"));
            Assert.AreEqual("First", Text(Member("mode"), "value"));
        }

        [Test]
        public void ReadOnly_IsCarried()
        {
            Assert.AreEqual(true, WebJson.Member(Member("locked"), "readOnly"));
            Assert.AreEqual(false, WebJson.Member(Member("speed"), "readOnly"));
        }

        [Test]
        public void AMemberHiddenFromTheUI_IsNotListed()
        {
            Assert.IsNull(Member("hidden"));
        }

        //Listed, but as nothing to draw: its header and tooltip are still the panel's, and leaving it
        //out would shift every row after it.
        [Test]
        public void AnUnsupportedMember_IsListedWithNoWidgetAndNoValue()
        {
            var rotation = Member("rotation");

            Assert.AreEqual("None", Text(rotation, "kind"));
            Assert.IsNull(WebJson.Member(rotation, "value"));
        }

        #endregion

        #region Methods

        [Test]
        public void AParameterlessMethod_GetsAButton()
        {
            Assert.AreEqual("Ping", Text(Method("Ping"), "label"));
        }

        //Same two exclusions UIMaster.CreateButton makes: no arguments, and not hidden from the UI.
        [TestCase("WithArgument")]
        [TestCase("HiddenMethod")]
        [TestCase("ControllableLoadWithName")]
        public void AMethodWithNoButton_IsNotListed(string method)
        {
            Assert.IsNull(Method(method));
        }

        object Method(string name)
        {
            foreach (var method in List(Parsed(), "methods"))
            {
                if (WebJson.AsString(WebJson.Member(method, "name")) == name)
                    return method;
            }

            return null;
        }

        #endregion

        #region Panel and message

        [Test]
        public void ThePanelAppearance_ComesFromGenUIPanelSettings()
        {
            var settings = _go.AddComponent<GenUIPanelSettings>();
            settings.barColor = new Color(1f, 0f, 0f, 1f);
            settings.closePanelAtStart = false;

            var panel = Parsed();

            CollectionAssert.AreEqual(new[] { 1d, 0d, 0d, 1d }, List(panel, "barColor"));
            Assert.AreEqual(false, WebJson.Member(panel, "closeAtStart"));
        }

        [Test]
        public void TheSchemaMessage_CarriesTheStyleTokensAndTheRegistry()
        {
            ControllableMaster.Register(_mirror);

            var message = WebJson.Parse(WebSchema.SchemaMessage());

            Assert.AreEqual("schema", Text(message, "t"));
            StringAssert.Contains(GenUIStyle.CssVariable("row-height"), Text(message, "css"));

            var ids = new List<string>();
            foreach (var controllable in List(message, "controllables"))
                ids.Add(WebJson.AsString(WebJson.Member(controllable, "id")));

            CollectionAssert.Contains(ids, "Schema");
        }

        //The browser scrubs a label at the panel's own rate because it is sent these, rather than
        //naming them again in JavaScript where nothing would catch the two drifting apart.
        [Test]
        public void TheScrubRates_AreDragValueUIsOwnConstants()
        {
            var rates = WebJson.Member(WebJson.Parse(WebSchema.SchemaMessage()), "scrub");

            Assert.AreEqual(DragValueUI.RangeDragPixels, Number(rates, "rangePixels"));
            Assert.AreEqual(DragValueUI.FloatUnitsPerPixel, Number(rates, "floatPerPixel"));
            Assert.AreEqual(DragValueUI.PixelsPerIntStep, Number(rates, "pixelsPerIntStep"));
            Assert.AreEqual(DragValueUI.CoarseMultiplier, Number(rates, "coarse"));
            Assert.AreEqual(DragValueUI.FineMultiplier, Number(rates, "fine"));
        }

        //A controllable told to draw no panel has nothing to mirror either.
        [Test]
        public void AControllableWithNoPanel_IsLeftOutOfTheMessage()
        {
            _go.AddComponent<GenUIPanelSettings>().usePanel = false;
            ControllableMaster.Register(_mirror);

            var ids = new List<string>();
            foreach (var controllable in List(WebJson.Parse(WebSchema.SchemaMessage()), "controllables"))
                ids.Add(WebJson.AsString(WebJson.Member(controllable, "id")));

            CollectionAssert.DoesNotContain(ids, "Schema");
        }

        #endregion
    }
}
