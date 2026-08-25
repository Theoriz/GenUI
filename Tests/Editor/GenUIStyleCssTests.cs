using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// Covers the CSS custom properties the web mirror draws its rows with. They are generated from
    /// GenUIStyle so the browser cannot drift from the panel, which is only true while every token is
    /// emitted and every number is CSS-legal whatever the editor's locale.
    /// </summary>
    public class GenUIStyleCssTests
    {
        //Every token ToCss is expected to carry. Spelled out rather than derived, so a token that
        //disappears fails here instead of silently leaving the client with no value.
        static readonly string[] ExpectedTokens =
        {
            "row-height", "header-height", "tooltip-height", "panel-title-height", "preset-row-height",
            "checkbox-size", "panel-arrow-size", "panel-arrow-inset", "color-bar-width", "panel-padding",
            "preset-section-padding", "preset-section-gap", "separator-space-above", "separator-thickness",
            "panel-bar-gap", "axis-spacing", "axis-label-gap", "slider-track-start", "slider-track-end",
            "slider-value-start", "slider-track-inset", "slider-handle-width", "slider-band-min",
            "slider-band-max", "label-width-ratio", "input-text-inset-x", "input-text-inset-y",
            "tooltip-bottom-spacing", "method-gap-height", "panel-title-bottom-spacing",
            "label-font-size", "panel-title-font-size", "tooltip-font-size", "label-min-font-size",
            "label-color", "input-text-color", "placeholder-color", "tooltip-color", "panel-background",
            "panel-title-background-alpha", "separator-color", "toggle-on", "toggle-off",
            "dropdown-template-background", "dropdown-item-background", "dropdown-item-label",
            "popup-backdrop", "caret-color", "selection-color",
            "control-normal", "control-highlighted", "control-pressed", "control-selected",
            "control-disabled", "control-fade-duration",
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
            Assert.AreEqual("25px", ValueOf("row-height"));
        }

        [Test]
        public void AFontSize_IsEmittedInPixels()
        {
            Assert.AreEqual("14px", ValueOf("label-font-size"));
        }

        [Test]
        public void ARatio_IsEmittedAsAPercentage()
        {
            Assert.AreEqual("50%", ValueOf("label-width-ratio"));
            Assert.AreEqual("80%", ValueOf("slider-track-end"));
        }

        [Test]
        public void AColour_IsEmittedAsRgbaWithByteChannels()
        {
            Assert.AreEqual("rgba(20, 20, 20, 0.451)", ValueOf("panel-background"));
            Assert.AreEqual("rgba(255, 255, 255, 1)", ValueOf("label-color"));
        }

        [Test]
        public void TheControlStates_CarryTheirTintsAndTheirFade()
        {
            Assert.AreEqual("rgba(58, 58, 58, 1)", ValueOf("control-normal"));
            Assert.AreEqual("rgba(72, 72, 72, 1)", ValueOf("control-highlighted"));
            Assert.AreEqual("rgba(23, 23, 23, 1)", ValueOf("control-pressed"));
            Assert.AreEqual("0.1s", ValueOf("control-fade-duration"));
        }

        //Read-only leaves no frame in the panel, which is a fully transparent disabled tint.
        [Test]
        public void TheDisabledControlState_IsTransparent()
        {
            Assert.AreEqual("rgba(0, 0, 0, 0)", ValueOf("control-disabled"));
        }

        //The bar colour is per-controllable and arrives with the schema, so only the alpha is a token.
        [Test]
        public void ThePanelTitleTint_IsEmittedAsAPlainNumber()
        {
            Assert.AreEqual("0.04", ValueOf("panel-title-background-alpha"));
        }

        [Test]
        public void ThePickerHeight_IsDerivedFromItsParts()
        {
            Assert.AreEqual("302px", ValueOf("picker-height"));
        }

        [Test]
        public void AFrenchLocale_StillEmitsCssNumbers()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                var declarations = Declarations(GenUIStyle.ToCss());
                Assert.AreEqual("rgba(20, 20, 20, 0.451)", declarations["panel-background"]);
                Assert.AreEqual("0.1s", declarations["control-fade-duration"]);
                Assert.AreEqual("0.04", declarations["panel-title-background-alpha"]);
                //rgba() separates its channels with commas, so only a comma between two digits is the
                //locale leaking through.
                foreach (var declaration in declarations)
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
