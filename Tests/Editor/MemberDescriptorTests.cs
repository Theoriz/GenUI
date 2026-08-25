using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    public enum DescriptorMode { First, Second }

    [Flags]
    public enum DescriptorFlags { None = 0, A = 1, B = 2 }

    /// <summary>
    /// One field per branch of the dispatch. The fields carry no [OCFProperty]: the attribute is
    /// supplied by the test instead, so one field can be described several ways.
    /// </summary>
    public class DescriptorFixture : Controllable
    {
        public List<string> presets = new List<string> { "a", "b" };

        public float speed;
        //Qualified: NUnit declares a RangeAttribute of its own, and this file uses both namespaces.
        [UnityEngine.Range(1f, 5f)] public float ranged;
        public int count;
        [UnityEngine.Range(0f, 10f)] public int rangedInt;
        public bool toggled;
        public string text;
        public Color color;
        public Vector2 v2;
        public Vector2Int v2i;
        public Vector3 v3;
        public Vector3Int v3i;
        public Vector4 v4;
        public DescriptorMode mode;
        public DescriptorFlags flags;
        public Quaternion rotation;

        public string chosen;
        public DescriptorMode modeFromList;
        public string broken;

        [Header("Section")]
        [Tooltip("What it does")]
        public float documented;

        public float locked;
    }

    /// <summary>
    /// The widget an exposed member gets is decided once, by MemberDescriptor, so the in-game panel
    /// and anything else drawing the same members cannot disagree. These cases pin the whole dispatch
    /// — one per supported type, plus every branch that deliberately draws nothing.
    /// </summary>
    public class MemberDescriptorTests
    {
        GameObject _go;
        DescriptorFixture _controllable;

        [SetUp]
        public void CreateControllable()
        {
            //Hidden and not saved so the fixture never becomes part of the scene the user has open.
            _go = new GameObject("member-descriptor-test") { hideFlags = HideFlags.HideAndDontSave };
            _controllable = _go.AddComponent<DescriptorFixture>();
            _controllable.controllableId = "Fixture";
        }

        [TearDown]
        public void DestroyControllable()
        {
            if (_go != null)
                UnityEngine.Object.DestroyImmediate(_go);
        }

        MemberDescriptor Describe(string member, OCFProperty attribute = null)
        {
            var field = typeof(DescriptorFixture).GetField(member, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(field, "The fixture has no field named " + member + ".");

            return MemberDescriptor.Describe(_controllable, field, attribute ?? new OCFProperty());
        }

        #region Type dispatch

        [TestCase("speed", WidgetKind.Input)]
        [TestCase("count", WidgetKind.Input)]
        [TestCase("ranged", WidgetKind.Slider)]
        [TestCase("rangedInt", WidgetKind.Slider)]
        [TestCase("toggled", WidgetKind.Toggle)]
        [TestCase("text", WidgetKind.Input)]
        [TestCase("color", WidgetKind.Color)]
        [TestCase("v2", WidgetKind.Vector2)]
        [TestCase("v2i", WidgetKind.Vector2Int)]
        [TestCase("v3", WidgetKind.Vector3)]
        [TestCase("v3i", WidgetKind.Vector3Int)]
        [TestCase("v4", WidgetKind.Vector4)]
        [TestCase("mode", WidgetKind.Dropdown)]
        public void EverySupportedType_GetsItsWidget(string member, WidgetKind expected)
        {
            Assert.AreEqual(expected, Describe(member).Kind);
        }

        [Test]
        public void Range_CarriesItsBounds()
        {
            var descriptor = Describe("ranged");

            Assert.AreEqual(1f, descriptor.Min);
            Assert.AreEqual(5f, descriptor.Max);
        }

        //Int members format and clamp as whole numbers; a slider also uses this for wholeNumbers.
        [TestCase("speed", true)]
        [TestCase("ranged", true)]
        [TestCase("count", false)]
        [TestCase("rangedInt", false)]
        public void IsFloat_FollowsTheNumericType(string member, bool expected)
        {
            Assert.AreEqual(expected, Describe(member).IsFloat);
        }

        [Test]
        public void EnumMember_CarriesItsTypeAndNoTargetList()
        {
            var descriptor = Describe("mode");

            Assert.AreEqual(typeof(DescriptorMode), descriptor.EnumType);
            Assert.IsNull(descriptor.TargetList);
        }

        #endregion

        #region Target lists

        [Test]
        public void TargetList_MakesADropdownOverTheList()
        {
            var descriptor = Describe("chosen", new OCFProperty { targetList = "presets" });

            Assert.AreEqual(WidgetKind.Dropdown, descriptor.Kind);
            Assert.AreEqual("presets", descriptor.TargetList);
            Assert.IsNull(descriptor.EnumType, "A list dropdown must not also be resolved as an enum.");
        }

        //Both dropdown routes can apply to the same member; the list is the one the user asked for.
        [Test]
        public void TargetList_WinsOverTheMembersOwnEnum()
        {
            var descriptor = Describe("modeFromList", new OCFProperty { targetList = "presets" });

            Assert.AreEqual("presets", descriptor.TargetList);
            Assert.IsNull(descriptor.EnumType);
        }

        [Test]
        public void TargetList_NamingNoList_DrawsNothingAndSaysWhy()
        {
            var descriptor = Describe("broken", new OCFProperty { targetList = "noSuchList" });

            Assert.AreEqual(WidgetKind.None, descriptor.Kind);
            StringAssert.Contains("noSuchList", descriptor.SkipReason);
            StringAssert.Contains("broken", descriptor.SkipReason);
        }

        #endregion

        #region Members that deliberately get no widget

        [Test]
        public void FlagsEnum_StaysOscOnly()
        {
            var descriptor = Describe("flags");

            Assert.AreEqual(WidgetKind.None, descriptor.Kind);
            StringAssert.Contains("[Flags]", descriptor.SkipReason);
        }

        [Test]
        public void UnsupportedType_DrawsNothingAndNamesTheType()
        {
            var descriptor = Describe("rotation");

            Assert.AreEqual(WidgetKind.None, descriptor.Kind);
            StringAssert.Contains("rotation", descriptor.SkipReason);
            StringAssert.Contains("Quaternion", descriptor.SkipReason);
        }

        //The controllable is named so the warning points at the panel the member is missing from.
        [Test]
        public void SkipReason_NamesTheControllable()
        {
            StringAssert.Contains("Fixture", Describe("rotation").SkipReason);
        }

        [Test]
        public void ADrawnMember_HasNoSkipReason()
        {
            Assert.IsNull(Describe("speed").SkipReason);
        }

        #endregion

        #region Label, header, tooltip, read-only

        [Test]
        public void Label_IsTheParsedMemberName()
        {
            Assert.AreEqual("Ranged Int", Describe("rangedInt").Label);
        }

        [Test]
        public void HeaderAndTooltip_AreCarriedThrough()
        {
            var descriptor = Describe("documented");

            Assert.AreEqual("Section", descriptor.Header);
            Assert.AreEqual("What it does", descriptor.Tooltip);
        }

        [Test]
        public void WithoutTheAttributes_HeaderAndTooltipAreNull()
        {
            var descriptor = Describe("speed");

            Assert.IsNull(descriptor.Header);
            Assert.IsNull(descriptor.Tooltip);
        }

        //A skipped member still carries its header and tooltip: they are drawn either way.
        [Test]
        public void ASkippedMember_StillCarriesItsHeader()
        {
            Assert.AreEqual("Section", Describe("documented",
                new OCFProperty { targetList = "noSuchList" }).Header);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadOnly_ComesFromTheAttribute(bool readOnly)
        {
            Assert.AreEqual(readOnly, Describe("locked", new OCFProperty { readOnly = readOnly }).ReadOnly);
        }

        #endregion
    }
}
