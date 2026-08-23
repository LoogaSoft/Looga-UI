# Menus

The menu module is organized around screens, layouts, panels, and shared slots.

## Choosing A Concept

Create a screen when Back must return to the previous menu. Create a layout when the destination stays the same but its visible panels change. Create a panel when one UI section needs independent visibility, reuse, or transitions.

Examples:

- Pause and Settings are separate screens.
- Shop Buy and Shop Sell can be layouts of one Shop screen.
- Loadout and Backpack can remain one panel when they always appear together.
- A confirmation dialog is usually an overlay screen.

## Basic Setup

1. Open `LoogaSoft > UI > Menus > Menu Project`.
2. Create one Menu Project asset.
3. Add shared slots for reusable headers, navigation, actions, and backgrounds.
4. Add `LoogaMenuRoot` to the main menu canvas and assign the Menu Project.
5. Add one `LoogaMenuRegionHost` to each shared presenter and assign its shared slot.
6. Create panel definitions and add `LoogaMenuPanel` to their scene objects.
7. Create a screen. The Menu Project window creates its default layout automatically.
8. Add the panels shown by each layout.
9. Use `LoogaMenuButton` for Open Screen, Switch Layout, Back, and Close All operations.

Panel objects can start disabled. The menu root registers and controls them.

## Shared UI

Shared slots keep persistent presenters separate from screen panels. A screen or layout can inherit, add to, replace, or hide each shared slot.

The runtime resolves shared UI in this order:

1. Menu Project default.
2. Active context.
3. Open screen.
4. Active layout.

Contexts are optional assets for persistent application states such as Main Menu, Station, or Raid.

## Open Modes

- `Replace` closes the current destination before opening the new one.
- `AddAlongside` keeps current content visible.
- `Overlay` opens above the current screen and becomes the next Back target.

## Previewing

Open `LoogaSoft > UI > Menus > Menu Preview`. Expand a screen to preview its layouts. Right-click a row to select its definition. Middle-click a row to select matching scene objects.

## Migrating From Looga Menu Framework

Install Looga UI 2.0.0 or newer, then remove `com.loogasoft.loogamenuframework` from `Packages/manifest.json`. Do not install both packages together because both contain the same assembly identities. Existing scenes, prefabs, definitions, namespaces, and asmdef references remain valid.
