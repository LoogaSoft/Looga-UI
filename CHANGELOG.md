# Changelog

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
