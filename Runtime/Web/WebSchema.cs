using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

/// <summary>
/// What the browser is told to draw: every registered <c>Controllable</c>, its members as
/// <see cref="MemberDescriptor"/> already decided them, its method buttons, and the panel appearance
/// <c>GenUIPanelSettings</c> resolves.
/// </summary>
/// <remarks>
/// Nothing here decides which widget a member gets - that is <c>MemberDescriptor.Describe</c>, the
/// same call <c>UIMaster.CreateUI</c> builds from, so the two interfaces cannot disagree. The two
/// checks that are not a member's own (a member's <c>showInUI</c>, and a method's <c>showInUI</c> and
/// parameter count) are repeated here because methods get no descriptor, exactly as
/// <c>UIMaster.CreateButton</c> does.
///
/// A member the panel draws nothing for is still listed, as kind <c>None</c>: its header and tooltip
/// are drawn either way, and leaving it out would move every row that follows it.
/// </remarks>
public static class WebSchema
{
    /// <summary>
    /// The whole registry as one <c>schema</c> message, style tokens included.
    /// </summary>
    /// <remarks>
    /// The CSS block travels with the schema rather than being served as a file, so a browser
    /// reconnecting after a recompile cannot end up drawing yesterday's tokens over today's rows.
    /// </remarks>
    public static string SchemaMessage()
    {
        var json = new StringBuilder();
        json.Append("{\"t\":\"schema\",\"css\":").Append(WebJson.Quote(GenUIStyle.ToCss()));
        json.Append(",\"oscRoot\":").Append(WebJson.Quote(OscRoot()));
        json.Append(",\"scrub\":").Append(ScrubJson());
        json.Append(",\"controllables\":[");

        //Ordered here rather than in the browser, through the rule the panel stack itself uses.
        var panels = new List<Controllable>();
        foreach (var registered in ControllableMaster.RegisteredControllables)
        {
            var controllable = registered.Value;
            if (controllable == null || !GenUIPanelSettings.UsePanel(controllable))
                continue;

            panels.Add(controllable);
        }

        panels.Sort(GenUIPanelSettings.ComparePanels);

        var first = true;
        foreach (var controllable in panels)
        {
            if (!first) json.Append(',');
            first = false;

            json.Append(ControllableJson(controllable));
        }

        return json.Append("]}").ToString();
    }

    /// <summary>One controllable: how its panel looks, what it holds, and its current values.</summary>
    /// <remarks>
    /// Values ride along with the schema so a browser draws the right thing on the frame it connects,
    /// rather than showing zeros until each member next changes.
    /// </remarks>
    public static string ControllableJson(Controllable controllable)
    {
        var barColor = GenUIPanelSettings.BarColorFor(controllable);

        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"id\":").Append(WebJson.Quote(controllable.controllableId));
        json.Append(",\"barColor\":").Append(WebJson.Array(barColor.r, barColor.g, barColor.b, barColor.a));
        json.Append(",\"closeAtStart\":").Append(WebJson.Bool(GenUIPanelSettings.ClosePanelAtStart(controllable)));

        json.Append(",\"members\":[");
        var first = true;
        if (controllable.controllableFields != null)
        {
            foreach (var member in controllable.controllableFields)
            {
                var attribute = Attribute.GetCustomAttribute(member.Value, typeof(OCFProperty)) as OCFProperty;
                if (attribute == null || !attribute.showInUI)
                    continue;

                if (!first) json.Append(',');
                first = false;

                json.Append(MemberJson(controllable, member.Value, attribute));
            }
        }

        json.Append("],\"methods\":[");
        first = true;
        if (controllable.controllableMethods != null)
        {
            foreach (var method in controllable.controllableMethods)
            {
                if (!IsButton(method.Value))
                    continue;

                if (!first) json.Append(',');
                first = false;

                json.Append(MethodJson(controllable, method.Value.methodInfo));
            }
        }

        return json.Append("]}").ToString();
    }

    /// <summary>
    /// The first segment of every OSC address, which the browser needs to show one: the rest of the
    /// address is the "id/member" key it already holds.
    /// </summary>
    static string OscRoot()
    {
        return ControllableMaster.instance != null ? ControllableMaster.instance.RootOSCAddress : null;
    }

    /// <summary>
    /// What a label drag is worth per pixel, straight from <c>DragValueUI</c>.
    /// </summary>
    /// <remarks>
    /// Sent rather than named again in the client, so a rate changed in the panel changes in the
    /// browser with it: the client has no test harness to catch the two drifting apart.
    /// </remarks>
    static string ScrubJson()
    {
        var json = new StringBuilder();
        json.Append("{\"rangePixels\":").Append(WebJson.Number(DragValueUI.RangeDragPixels));
        json.Append(",\"floatPerPixel\":").Append(WebJson.Number(DragValueUI.FloatUnitsPerPixel));
        json.Append(",\"pixelsPerIntStep\":").Append(WebJson.Number(DragValueUI.PixelsPerIntStep));
        json.Append(",\"coarse\":").Append(WebJson.Number(DragValueUI.CoarseMultiplier));
        json.Append(",\"fine\":").Append(WebJson.Number(DragValueUI.FineMultiplier));
        return json.Append('}').ToString();
    }

    /// <summary>Whether a method gets a button, on the same terms as <c>UIMaster.CreateButton</c>.</summary>
    public static bool IsButton(ClassMethodInfo method)
    {
        var attribute = method.Options;
        if (attribute != null && !attribute.showInUI)
            return false;

        //A method taking arguments has no control to supply them from.
        return method.methodInfo.GetParameters().Length == 0;
    }

    static string MemberJson(Controllable controllable, FieldInfo field, OCFProperty attribute)
    {
        var descriptor = MemberDescriptor.Describe(controllable, field, attribute);

        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"name\":").Append(WebJson.Quote(descriptor.Name));
        json.Append(",\"label\":").Append(WebJson.Quote(descriptor.Label));
        json.Append(",\"kind\":").Append(WebJson.Quote(descriptor.Kind.ToString()));
        json.Append(",\"readOnly\":").Append(WebJson.Bool(descriptor.ReadOnly));

        if (descriptor.Header != null)
            json.Append(",\"header\":").Append(WebJson.Quote(descriptor.Header));

        if (descriptor.Tooltip != null)
            json.Append(",\"tooltip\":").Append(WebJson.Quote(descriptor.Tooltip));

        if (descriptor.Kind == WidgetKind.Slider || descriptor.Kind == WidgetKind.Input)
            json.Append(",\"isFloat\":").Append(WebJson.Bool(descriptor.IsFloat));

        if (descriptor.Kind == WidgetKind.Slider)
        {
            json.Append(",\"min\":").Append(WebJson.Number(descriptor.Min));
            json.Append(",\"max\":").Append(WebJson.Number(descriptor.Max));
        }

        if (descriptor.Kind == WidgetKind.Dropdown)
        {
            json.Append(",\"options\":[");
            var options = Options(controllable, descriptor);
            for (var i = 0; i < options.Count; i++)
            {
                if (i != 0) json.Append(',');
                json.Append(WebJson.Quote(options[i]));
            }

            json.Append(']');
        }

        var value = WebValueCodec.ToJson(field.FieldType, field.GetValue(controllable));
        if (value != null)
            json.Append(",\"value\":").Append(value);

        return json.Append('}').ToString();
    }

    /// <summary>
    /// What a dropdown offers: the entries of the named list, read live, or the enum's member names.
    /// </summary>
    public static List<string> Options(Controllable controllable, MemberDescriptor descriptor)
    {
        if (descriptor.TargetList != null)
            return controllable.GetTargetList(descriptor.TargetList) ?? new List<string>();

        if (descriptor.EnumType != null)
            return new List<string>(Enum.GetNames(descriptor.EnumType));

        return new List<string>();
    }

    static string MethodJson(Controllable controllable, MethodInfo method)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append("\"name\":").Append(WebJson.Quote(method.Name));
        json.Append(",\"label\":").Append(WebJson.Quote(ControllableUI.ParseNameString(method.Name)));

        var group = MethodGroup(controllable, method);
        if (group != null)
            json.Append(",\"group\":").Append(WebJson.Quote(group));

        return json.Append('}').ToString();
    }

    /// <summary>
    /// The block a button belongs to, or null for one that stays among the member rows.
    /// </summary>
    /// <remarks>
    /// The same grouping <c>UIMaster.CleanGeneratedUI</c> makes, named here rather than left to the
    /// client: which methods are the preset ones is OCF's to say, not the browser's to hardcode.
    /// </remarks>
    static string MethodGroup(Controllable controllable, MethodInfo method)
    {
        if (Array.IndexOf(Controllable.PresetMethodNames, method.Name) >= 0)
            return "preset";

        //Only the GenUI panel owns the global buttons: a target script may expose its own SaveAll.
        if (controllable is ControllableMasterControllable)
        {
            if (Array.IndexOf(ControllableMasterControllable.AllPresetMethodNames, method.Name) >= 0)
                return "allPreset";

            if (Array.IndexOf(ControllableMasterControllable.GlobalActionMethodNames, method.Name) >= 0)
                return "globalAction";
        }

        return null;
    }
}
