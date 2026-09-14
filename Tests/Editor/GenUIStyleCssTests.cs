using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers the CSS custom properties the web mirror draws its rows with. They are generated from
    /// GenUIStyle so the browser cannot drift from the panel, which is only true while every token is
    /// emitted, carries the unit the client uses it as, and stays CSS-legal whatever the editor's
    /// locale. The values themselves are a look to be tuned, and are nowhere written out here.
    /// </summary>
    public class GenUIStyleCssTests
    {
        //Every token ToCss is expected to carry. Spelled out rather than derived, so a token that
        //disappears fails here instead of silently leaving the client with no value.
        static readonly string[] ExpectedTokens =
        {
            "row-height", "header-height", "panel-title-height", "preset-row-height",
            "checkbox-size", "panel-arrow-size", "panel-arrow-inset", "color-bar-width", "panel-padding",
            "preset-section-padding", "preset-section-gap", "separator-space-above", "separator-thickness",
            "panel-bar-gap", "axis-spacing", "axis-label-gap", "slider-track-start", "slider-track-end",
            "slider-value-start", "slider-track-inset", "slider-handle-width", "slider-band-min",
            "slider-band-max", "label-width-ratio", "toggle-box-width", "input-text-inset-x", "input-text-inset-y",
            "method-gap-height", "panel-title-bottom-spacing", "panel-width", "panel-spacing",
            "label-font-size", "panel-title-font-size", "tooltip-font-size", "label-min-font-size",
            "label-color", "input-text-color", "placeholder-color", "tooltip-color", "scroll-view-background",
            "panel-title-background-alpha", "separator-color", "toggle-on", "toggle-off",
            "dropdown-template-background", "dropdown-item-background", "dropdown-item-label",
            "popup-backdrop", "caret-color", "selection-color",
            "control-normal", "control-highlighted", "control-pressed", "control-selected",
            "control-disabled", "control-fade-duration",
            "slider-handle-normal", "slider-handle-highlighted", "slider-handle-pressed", "slider-handle-disabled",
            "picker-padding", "picker-content-width", "picker-sv-height", "picker-bar-thickness",
            "picker-bar-spacing", "picker-field-row-height", "picker-width", "picker-height",
            "picker-marker-size", "picker-marker-thickness", "picker-checker-cell-size",
            "picker-background", "picker-marker-color", "picker-checker-light", "picker-checker-dark"
        };

        static Dictionary<string, string> Declarations(string css)
        {
            var declarations = new Dictionary<string, string>();
            foreach (Match match in Regex.Matches(css, @"--genui-([a-z0-9-]+)\s*:\s*([^;]+);"))
                declarations.Add(match.Groups[1].Value, match.Groups[2].Value.Trim());
            return declarations;
        }

        static string ValueOf(string token)
        {
            return Declarations(GenUIStyle.ToCss())[token];
        }

        //What the assertions below are made of. Every value in GenUIStyle is a look to be tuned, so
        //what is pinned here is the shape each one is emitted in - a number carrying the unit the
        //client uses it as - and never the number itself, which a test would only restate.
        const string Number = @"-?\d+(\.\d+)?";

        static void AssertShape(string token, string pattern)
        {
            var value = ValueOf(token);
            Assert.IsTrue(Regex.IsMatch(value, "^" + pattern + "$"),
                token + " is not CSS the client can use: " + value);
        }

        [Test]
        public void TheOutput_IsARootBlock()
        {
            var css = GenUIStyle.ToCss();
            Assert.IsTrue(css.StartsWith(":root {"), "Expected a :root block, got: " + css);
            Assert.IsTrue(css.TrimEnd().EndsWith("}"), "Expected the block to be closed, got: " + css);
        }

        [Test]
        public void EveryToken_IsEmitted()
        {
            var declarations = Declarations(GenUIStyle.ToCss());
            foreach (var token in ExpectedTokens)
                Assert.IsTrue(declarations.ContainsKey(token), "Missing token: " + token);
        }

        //Dictionary.Add throws on a duplicate key, so parsing at all is the check: two declarations of
        //one token would mean the last one silently wins in the browser.
        [Test]
        public void NoToken_IsEmittedTwice()
        {
            Assert.AreEqual(ExpectedTokens.Length, Declarations(GenUIStyle.ToCss()).Count);
        }

        [Test]
        public void CssVariable_SpellsTheEmittedName()
        {
            Assert.AreEqual("--genui-row-height", GenUIStyle.CssVariable("row-height"));
        }

        [Test]
        public void AMetric_IsEmittedInPixels()
        {
            AssertShape("row-height", Number + "px");
            AssertShape("input-text-inset-x", Number + "px");
        }

        [Test]
        public void AFontSize_IsEmittedInPixels()
        {
            AssertShape("label-font-size", Number + "px");
        }

        [Test]
        public void ARatio_IsEmittedAsAPercentage()
        {
            AssertShape("label-width-ratio", Number + "%");
            AssertShape("slider-track-end", Number + "%");
        }

        //A CSS channel is a byte, while Unity's is a float; the alpha stays the 0..1 number CSS wants.
        [Test]
        public void AColour_IsEmittedAsRgbaWithByteChannels()
        {
            AssertShape("picker-background", @"rgba\(\d{1,3}, \d{1,3}, \d{1,3}, " + Number + @"\)");
            AssertShape("control-normal", @"rgba\(\d{1,3}, \d{1,3}, \d{1,3}, " + Number + @"\)");

            var channels = Regex.Match(ValueOf("picker-background"), @"rgba\((\d+), (\d+), (\d+), ");
            Assert.AreEqual(Mathf.RoundToInt(GenUIStyle.PickerBackground.r * 255f),
                int.Parse(channels.Groups[1].Value), "The red channel is not the colour's, in bytes.");
        }

        [Test]
        public void AFadeDuration_IsEmittedInSeconds()
        {
            AssertShape("control-fade-duration", Number + "s");
        }

        //Read-only leaves no frame in the panel, which is a fully transparent disabled tint - whatever
        //colour that tint is otherwise given.
        [Test]
        public void TheDisabledControlState_IsTransparent()
        {
            Assert.IsTrue(ValueOf("control-disabled").EndsWith(", 0)"),
                "The disabled tint is not transparent: " + ValueOf("control-disabled"));
        }

        //The bar colour is per-controllable and arrives with the schema, so only the alpha is a token -
        //an opacity, which CSS takes with no unit at all.
        [Test]
        public void ThePanelTitleTint_IsEmittedAsAPlainNumber()
        {
            AssertShape("panel-title-background-alpha", Number);
        }

        //Derived rather than a number of its own, so resizing one part cannot leave a gap or clip the
        //bottom row; the parts are what this adds up, so tuning any of them keeps it true.
        [Test]
        public void ThePickerHeight_IsDerivedFromItsParts()
        {
            var parts = 2f * GenUIStyle.PickerPadding + GenUIStyle.PickerSvHeight
                + 2f * (GenUIStyle.PickerBarSpacing + GenUIStyle.PickerBarThickness)
                + 2f * (GenUIStyle.PickerBarSpacing + GenUIStyle.PickerFieldRowHeight);

            Assert.AreEqual(parts.ToString("0.###", CultureInfo.InvariantCulture) + "px",
                ValueOf("picker-height"));
        }

        //The one place a locale can break the output: a French editor formats a float as "0,5", which
        //is not a CSS number. Compared against the same call under the invariant culture rather than
        //against written-out values, so nothing here has to be revisited when a colour is tuned.
        [Test]
        public void AFrenchLocale_StillEmitsCssNumbers()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                var invariant = Declarations(GenUIStyle.ToCss());

                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                var french = Declarations(GenUIStyle.ToCss());

                CollectionAssert.AreEquivalent(invariant, french, "The output depends on the editor's locale.");

                //rgba() separates its channels with commas, so only a comma between two digits is the
                //locale leaking through.
                foreach (var declaration in french)
                    Assert.IsFalse(Regex.IsMatch(declaration.Value, @"\d,\d"), "Decimal comma in "
                        + declaration.Key + ": " + declaration.Value);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }
    }
}
