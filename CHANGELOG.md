# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

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