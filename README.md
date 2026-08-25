# GenUI
Generative UI and OSC Control for Unity.

This plugin allows you to simply create a UI for your application, exposing script sliders, input fields and methods. This UI is also fully controllable via OSC.

![Demo](https://github.com/Theoriz/GenUI-Demo/blob/master/gif/genui.gif) 

## Requirements

| Requirement | Notes |
|---|---|
| **Unity 2022.3** or later | Set by the Input System dependency below. |
| **com.unity.inputsystem** | No fallback to the legacy input backend, so set **Project Settings > Player > Active Input Handling** to *Input System Package* or *Both*. |
| [**com.theoriz.ocf**](https://github.com/Theoriz/OCF) 2.4.0 or later | GenUI is the UI layer on top of OCF; the control model, OSC addressing and presets all live there. |
| [**com.theoriz.unityosc**](https://github.com/Theoriz/UnityOSC) 1.3.0 or later | OCF's transport. Earlier versions still work but declare Unity 2019.4. |

The packages declare no UPM `dependencies`, so nothing installs them for you and nothing warns you when a version is too old — install all three, in the order below.

## Installation

Add the following line to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.theoriz.unityosc": "https://github.com/Theoriz/UnityOSC.git",
    "com.theoriz.ocf": "https://github.com/Theoriz/OCF.git",
    "com.theoriz.genui": "https://github.com/Theoriz/GenUI.git"
  }
}
```

Or in the Unity Editor, go to **Window > Package Manager > + > Add package from git URL** and enter:

```
https://github.com/Theoriz/UnityOSC.git
```
then
```
https://github.com/Theoriz/OCF.git
```
then
```
https://github.com/Theoriz/GenUI.git
```

## Default Shortcuts

- F1 : Toggle the UI.
- PageUp / PageDown, or Ctrl + Plus/Minus (numpad included) : Scale up/down the UI, only when the UI is visible.
- Ctrl + Left/Right/Up/Down arrow : Move the UI, only when the UI is visible.
- F2 : Reset the UI, only when the UI is visible.
- Tab / Shift + Tab : Move to the next/previous input field, selecting its text so you can type over it.
- Ctrl + Z : Undo the last value you changed in the UI. A whole slider drag, label scrub or colour pick undoes in one press. Values arriving over OSC, and members restored by loading a preset, are not undone.
- Drag a numeric member's label left/right : Scrub its value, one label per vector axis, with Shift for coarse steps and Ctrl for fine ones.

Scaling is ignored while you are typing in an input field.

## Setup
1. In the toolbar go to Theoriz -> GenUI -> Add GenUI to Scene. It adds the GenUI prefab, plus an EventSystem if the scene has none.

> [!NOTE]
> The UI needs an EventSystem to receive input, but GenUI does not provide one itself. If you add the GenUI prefab from the Samples folder manually instead, add an EventSystem yourself via GameObject -> UI -> Event System.

2. Generate controllables for the scripts you want to control using the controllable generation described below.
3. Run the scene, press F1 to toggle the UI.

### Automatic Controllable Generation

1. In your MonoBehaviour script, add the [OCFExposed] attribute to the fields, properties and methods you want to expose to the UI and OSC.

> [!TIP]
> You can set some fields or properties as read only by using [OCFExposed(readOnly = true)].

2. On the script component of your script in your scene, click on the three dots on the top right and choose Add Controllable. It will prompt you to generate a Controllable script, click Generate. Once compilation finishes, the Controllable component is added automatically.

> [!TIP]
> You can also generate a controllable directly from the project window by right-clicking on a script and choosing Generate Controllable Script.

## Panel settings

To control the look of a Controllable's panel in GenUI, add a **GenUI Panel Settings** component next to the Controllable. You can do this by clicking the three dots on the Controllable and choose **Add GenUI Panel Settings**.

| Field | Default | Effect |
|---|---|---|
| `barColor` | a color derived from the controllable's ID | Color of the panel's title bar. |
| `usePanel` | on | Uncheck to give this controllable no panel at all. It stays controllable over OSC. |
| `closePanelAtStart` | on | Uncheck to have the panel start open. |

The component is optional: a Controllable without one draws its panel with the defaults above, already colored from its ID.

## Supported types
You can expose the following types :
- bool
- int
- float
- string
- Vector2
- Vector2Int
- Vector3
- Vector3Int
- Vector4
- Color
- any enum

An enum renders as a dropdown of its members — see [Exposing an enum](https://github.com/Theoriz/OCF#exposing-an-enum) in the OCF documentation. A `[Flags]` enum is the one exception: it logs a warning and draws no widget, because one dropdown cannot represent a combination of members. It is controllable over OSC.

The Header, Range, and Tooltip attributes are also supported in Controllables.

### The color picker

A Color member is drawn as a swatch; left-clicking the swatch opens the picker. It holds a saturation/value square, a hue bar, an alpha bar, an R/G/B/A row and a hex field.

The channel boxes and the hex field are both 0–255. The hex field accepts `#RGB`, `#RRGGBB` and `#RRGGBBAA`, with or without the leading `#`.

## Read-only members

A member marked [OCFExposed(readOnly = true)] is drawn as a display: its value with no box around it, nothing to click or type into. Read-only members are also left out of presets, and have no **Copy OSC Control Address** menu, since that address cannot control them.

## Exposing methods

A method without parameters shows as a button in the UI. A method with parameters gets no button, but is still callable over OSC.

## Exposing a list

To pick a value from a list of strings, keep the `List<string>` on your script and point a string member at it by name with [OCFExposed(targetList = "myList")]. It renders as a dropdown that writes the selected entry into that member.

See [Exposing a list](https://github.com/Theoriz/OCF#exposing-a-list) in the OCF documentation for a full example.

## OSC Control
To access a property or launch a method, use its address.

For example : "/OCF/id/method" or "/OCF/id/floatProperty 1.5". By default the id corresponds to the script type name, but this can be changed by setting the public variable `controllableId` on your script extending "Controllable".

> [!TIP]
> You can copy the OSC Control Address of any exposed parameter by right clicking anywhere on its row.

## Presets
This plugin comes with a preset system, you can save the state of a "Controllable" script. It saves each property to a file that can be loaded later so that you can create different settings for your script. To use it, click "Save", then simply select a preset in the dropdown menu — selecting it loads it immediately.

Each panel has "Save", "Save As", "Load" and "Show" buttons plus the preset dropdown, at the bottom of the panel. The GenUI panel has "Save All", "Save As All" and "Load All" to apply the same action to every controllable at once, plus "Open Presets Folder" to reveal the presets root in your file browser.

It is also possible to load a specific file via the OSC method "ControllableLoadWithName", giving it the case-sensitive file name as its argument :

```
/OCF/id/ControllableLoadWithName "myPreset.pst"
```

## Web mirror

The panel can also be served to a browser, so a phone or a laptop on the same network drives the same values. On the GenUI object, tick **Enable Web Server** on the **GenUI Web Server** component, press Play, and open `http://<the machine's IP>:6080` — the port is printed in the Console at start. The page needs no internet connection. Ticking the option during Play starts and stops the server there and then, and editing the port restarts it on the new one — connected browsers have to be reloaded.

The same two options sit in the GenUI panel and answer to OSC as `/OCF/GenUI/enableWebServer` and `/OCF/GenUI/webServerPort`, so the server can be switched on while the app runs. They are saved in the GenUI panel's presets like any other value — loading a preset that has the server on will start it. They also appear in the browser, where switching the server off ends that browser's own connection.

Panel, browsers and the target script stay in sync in all directions: whatever changes anywhere shows everywhere.

> [!WARNING]
> There is no password and no HTTPS. Anyone who can reach that port can change every exposed value and press every button, so leave the option off unless you are on a network you trust.

The browser is a mirror, not a copy. It draws the same rows from the same style values, with these differences:

- No label scrubbing, Ctrl + Z or Tab traversal.
- Right-clicking a row shows its OSC control address ready to copy, rather than copying it.
- Each browser keeps its own folded/unfolded panels.
- Text falls back to your system font unless Roboto is installed, and a long member name is cut short with an ellipsis rather than shrunk.

## Advanced

### Changing the look

The interface is built from code, not from prefabs. Every size and colour comes from `GenUIStyle`, and the sprites and fonts come from the `GenUIAssets` asset in `Resources`. Changing a row height, a tint or the font in one of those two places applies to every widget at once, and to the web mirror with it.

### Handling your own OSC messages

To handle your own OSC messages — anything not addressed to /OCF/ — subscribe to the receiver directly :

```C#
using UnityOSC;

OSCMaster.Receivers["myReceiver"].messageReceived += (OSCMessage m) => Debug.Log(m.Address);
```

### Reserved names

Do not reuse a name that "Controllable" already declares. The generated Controllable inherits from "Controllable", so a member of the same name shadows the real one and breaks it. The generator refuses these and tells you which member to rename. See [Reserved names](https://github.com/Theoriz/OCF#reserved-names) in the OCF documentation.

## License

GenUI is GPL-3.0; see `LICENSE`. The fonts it ships are third-party — see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).


