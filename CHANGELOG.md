# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

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