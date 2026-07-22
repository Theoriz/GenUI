# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## [1.7.1] - 2026-07-22

### Fixed

- `UIMaster` clears its `Instance` between play sessions, so a destroyed panel is no longer read as a live one when Reload Domain is off.

### Removed

- The unused `FCP_Persistence` and `FlexibleDraggableObject` scripts from the bundled FlexibleColorPicker and FlexibleUISystems plugins.

## [1.7.0] - 2026-07-21

### Added

- Ctrl+Z undoes the last value changed in the UI, with a whole slider drag, label scrub or colour pick undoing in one press.

### Fixed

- Checkboxes no longer write their value back out when a change arrives from the target script or over OSC.
- Ctrl +/- finds the `=` and `-` keys by what they print on the current keyboard layout, so zooming works on any layout rather than QWERTY and AZERTY only.
- Keyboard navigation no longer moves the selection while Ctrl is held, so Ctrl shortcuts leave the focused field alone.

## [1.6.2] - 2026-07-21

### Added

- Drag a numeric member's label left or right to scrub its value, with Shift for coarse steps and Ctrl for fine ones.

## [1.6.1] - 2026-07-21

### Added

- Tab and Shift+Tab move between input fields, selecting the field's text and scrolling it into view.
- The README's Requirements section now lists OCF and UnityOSC, with the minimum version of each.

## [1.6.0] - 2026-07-21

**Requires com.theoriz.ocf 1.5.1 or later**, which adds the `GlobalActionMethodNames` array this reads.
The packages declare no UPM dependencies, so update OCF yourself.

### Added

- Buttons named in `ControllableMasterControllable.GlobalActionMethodNames` get their own row under Save All / Save As All, instead of being squeezed in beside them.

## [1.5.1] - 2026-07-21

### Fixed

- The setup steps no longer tell you to choose `Add Controllable` a second time, which OCF 1.4.1 made unnecessary.

## [1.5.0] - 2026-07-21

**Requires com.theoriz.ocf 1.4.0 or later**, which raises the change event on OSC and preset writes.
The packages declare no UPM dependencies, so update OCF yourself.

### Changed

- `SliderUI` and `ColorUI` no longer refresh their display by hand after writing a value, since OCF now raises the change event itself.

## [1.4.0] - 2026-07-21

### Changed

- The GenUI prefab no longer contains an EventSystem, which conflicted with the one in any scene that already drives UI. Existing scenes keep theirs as an override.
- `Theoriz > GenUI > Add GenUI to Scene` now also adds an EventSystem when the scene has none, and is safe to re-run.
- Minimum Unity is now 2022.3, matching the Input System package GenUI requires.

### Added

- A warning at play time when the scene has no EventSystem.
- `Tests/` folder with EditMode and PlayMode tests.

### Removed

- The unused `UICanvas.prefab`.

### Fixed

- The F1 shortcut threw when `EventSystem.current` was null.

## [1.3.1] - 2026-07-21

### Fixed

- Tooltips were clipped to a single line: the panel's layout group leaves child heights alone, so anything that wrapped or followed a line break was cut off. `TooltipUI` now sizes itself to the text it holds.

### Changed

- Tooltips keep a gap underneath, so they read as belonging to the widget above rather than the one below. The amount is `bottomSpacing` on `Tooltip.prefab`.
- Tooltip text is dimmed relative to the widget labels.

## [1.3.0] - 2026-07-20

**Requires com.theoriz.ocf 1.3.0 or later.** Preset auto-load and the removal of the Load / Load All
buttons rely on `[OSCMethod(showInUI = false)]`, added in that release. The packages declare no UPM
dependencies, so update OCF yourself.

### Added

- Vector4 support: a four-field widget, added to the supported types.
- `keywords` in package.json, matching the other Theoriz packages.

### Changed

- Selecting a preset in the dropdown now loads it immediately; the per-panel **Load** and global **Load All** buttons were removed (loading is automatic — both remain OSC methods).
- UIMaster no longer holds serialized wiring references: every prefab (widgets, right-click menu, color picker) comes from a `UIPrefabs` ScriptableObject loaded from Resources, and the panel link is resolved from the hierarchy at runtime. Adding a supported type only touches that asset.

### Fixed

- The global preset panel stacked all preset buttons into its top row. The global preset buttons now sit on top and the per-controllable buttons at the bottom.

### Removed

- Preset tweening: removed the `TweenCurves` component from the sample prefab and the tween docs from the README. Requires the matching OCF change.

## [1.2.1] - 2026-07-20

### Removed

- ShouldNotExist, an internal MonoBehaviour on the panel prefab that forced localScale to one every frame, and a matching one-shot localScale reset in InputFieldUI. Both were vestigial from a pre-CanvasScaler scaling approach; UI scale is driven entirely by the canvas scaler's scaleFactor.

## [1.2.0] - 2026-07-17

**Requires com.theoriz.ocf 1.2.0 or later.** UIMaster now uses the preset method name constants added
in that release. The packages declare no UPM dependencies, so update OCF yourself.

### Changed

- Preset buttons are grouped by the name of the method they invoke instead of by their displayed label, which was derived from that name by a regex.
- README documents enum dropdowns, and links to OCF for the attribute reference and the reserved names.

### Fixed

- Ctrl + Minus did not zoom out on US QWERTY keyboards: zoom-out was bound only to the physical key that prints "-" on an AZERTY layout. Both are now bound, so the shortcut works on either.
- A controllable whose id was Save, Load or Show had its panel title moved into the preset holder, because preset buttons were matched on displayed text and the panel title shares that subtree.
- A target script exposing its own SaveAll, SaveAsAll or LoadAll had the button moved into a global preset holder created on its own panel. Those buttons now only group on the ControllableMaster panel.
- README listed Vector4 as a supported type and documented an OSCMaster.MessageAvailable event; neither exists. Vector4 support is tracked separately; the way to consume other OSC messages is OSCMaster.Receivers[name].messageReceived.
- README named the preset OSC method LoadPresetWithName; the method is LoadWithName, so the documented address matched nothing.
- README documented the preset buttons as "Save preset" and "Load Preset"; they are labelled "Save" and "Load".
- README's targetList example did not compile, and referred to the id field as ID.

## [1.1.0] - 2026-07-16

### Added

- Warning when a property type has no matching widget, instead of silently drawing nothing.

### Changed

- The color picker only writes a value when the picked color actually changes.
- Slider input is clamped to its [Range] and the field stays in sync with the stored value.

### Removed

- Unused SliderValue script.

### Fixed

- Operator precedence in the widget type dispatch let floats bypass the already-drawn guard, and a dead System.Float branch was removed.
- Dropdowns open on the current value instead of always the first entry.
- Vector inputs no longer throw on empty or partial input.
- Adding or removing a panel no longer throws on a duplicate or missing id.
- ParseNameString no longer throws on an empty name.
- Keyboard.current and EventSystem.current are null-guarded in the input paths.


## [1.0.2] - 2026-06-03

### Fixed

- Fixed unsubscription to ControllableMaster from UIMaster.


## [1.0.1] - 2026-04-22

### Added

- Added Theoriz/GenUI/Add GenUI to Scene menu item for prefab setup.


## [1.0.0] - 2026-04-21

### Added

- Set up repository and files for UPM support.