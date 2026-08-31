# Changelog

## Unreleased

## 4.0.0 - 2026-08-30

- Split shared styling into dedicated text, image, control, and color-only assets and binding components.
- Added control-specific style fields for buttons, dropdowns, input fields, scrollbars, sliders, and toggles.
- Added inline Edit and Save controls for design sub-asset names.
- Preserved existing image, text, and button-color asset GUIDs while migrating them to the new style types.

## 3.1.0 - 2026-08-30

- Replaced nested Color-token references in composite styles with literal color fields.
- Removed legacy theme color-token maps so Color tokens remain color-only binding assets.
- Added expandable inline token editing with sub-asset-aware New and Clone actions to style bindings.

## 3.0.1 - 2026-08-30

- Added color and alpha ownership to Image tokens.
- Changed standalone Color tokens to apply only when no Image token owns the target Image appearance.
- Clarified Image-versus-color ownership in the style-binding inspector.

## 3.0.0 - 2026-08-30

- Replaced composite element and typed-control style registration with four direct token families: color, button colors, image, and TextMeshPro text.
- Changed `LoogaStyleBinding` to apply compatible tokens directly without overriding TextMeshPro alignment.
- Added full Unity UI Image settings and Unity UI button state colors as reusable design assets.
- Simplified Looga controls to use their serialized Unity appearance plus optional token bindings.
- Replaced design-system adoption and consolidation tools with focused token creation, synchronization, renaming, and scene reapplication.
- Updated menu editors to use the unified Looga Toolkit foldout API.
- Cached Menu Preview definitions and scene bindings so visible preview windows no longer rescan the project and hierarchy during every GUI event.
- Kept one-off captured element styles internal while exposing only reusable or explicitly promoted styles in the public design-system library.
- Added loaded-scene style analysis, conservative public-style consolidation, explicit promotion, and selected-binding merge tools.
- Normalized insignificant capture precision so visually equivalent authored elements can share generated styles.

## 2.3.1 - 2026-08-24

- Excluded typed-control internals from the standalone element-style collection.
- Fixed scene audits that reported labels, icons, fills, and handles owned by styled controls as unbound.
- Made repeated UI adoption refresh existing style bindings instead of skipping them.
- Clarified adoption results by separating standalone, control, and internal-part style counts.

## 2.3.0 - 2026-08-24

- Consolidated menu and design-system R3 adapters behind one Package Support integration.
- Added loaded-scene design-system adoption for existing authored UI.
- Added exact visual matching so identical controls and elements share generated styles without changing appearance.
- Added generated color and typography tokens, typed control styles, a default theme, and root-canvas theme scopes.
- Stored the generated theme, tokens, and styles as sub-assets of one project design-system asset.

## 2.2.2 - 2026-08-24

- Removed unsupported Unity Undo recording from control script-type conversions. This prevents type-tree diff errors while retaining component identity and serialized references.

## 2.2.1 - 2026-08-24

- Added a one-click loaded-scene migration for every supported Unity UI control, including inactive objects.
- Added conversion previews, progress reporting, and cached script lookups.
- Preserved authored visuals by placing converted controls in Local style mode.

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
