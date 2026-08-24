# Changelog

## Unreleased

- Consolidated menu and design-system R3 adapters behind one Package Support integration.

## 2.2.0 - 2026-08-23

- Added `LoogaToggle`, `LoogaSlider`, `LoogaScrollbar`, `LoogaInputField`, and `LoogaDropdown` as full Unity-compatible replacement controls.
- Added typed style assets for every Looga control and centralized theme/style lifecycle handling.
- Added focused inspectors that separate design-system ownership from local Unity appearance settings.
- Replaced the button-only migration command with safe in-place conversion for all supported controls.
- Extended design-system scene audits and reapplication to every Looga control type.

## 2.1.0 - 2026-08-23

- Added `LoogaButton`, a Unity-compatible button with explicit design-system or local appearance ownership.
- Added reusable button styles for the button background, label, and icon roles.
- Added a focused button inspector that hides inactive local appearance settings.
- Added an in-place Unity Button conversion command that preserves serialized references and events.

## 2.0.0 - 2026-08-23

- Merged Looga Menu Framework into Looga UI while preserving all `LoogaSoft.Menu.*` assembly names, namespaces, and Unity asset GUIDs.
- Consolidated menu authoring commands under `LoogaSoft > UI > Menus`.
- Grouped menu R3 support with the other Looga UI integrations in Package Support.
- Added menu authoring and migration documentation.

## 1.1.1 - 2026-07-31

- Made optional UniTask support compatible with Git and immutable package installations.
- Isolated UniTask references in version-gated optional assemblies.
- Standardized optional-support messaging on the current package name.

## [1.1.0] - 2026-07-30

### Added

- Added `LoogaLayout` with Horizontal, Vertical, Grid, Flow, and Overlay arrangement modes.
- Added content, authored, fill-parent, fixed, and clamped container sizing.
- Added content, fill, uniform, and fixed child sizing with minimum and maximum constraints.
- Added `LoogaLayoutElement` for per-child sizing, flexible weight, and layout exclusion.
- Added `LoogaContentFitter` for leaf and wrapper content fitting without requiring a layout group.
- Added mode-aware custom inspectors, sizing-cycle validation, and calculated-size diagnostics.
