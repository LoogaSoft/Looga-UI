# Looga UI

Looga UI contains reusable UGUI layout tools, effects, and procedural graphics for LoogaSoft projects.

## Layout

The layout system replaces common chains of Unity layout groups, layout elements, and content-size fitters with three focused components. It uses Unity's normal `ILayoutElement` and layout rebuild contracts, so nested layouts propagate size changes upward without polling or per-frame reactive subscriptions.

### Components

- `LoogaLayout` arranges children and optionally sizes its own RectTransform. Its mode can be Horizontal, Vertical, Grid, Flow, or Overlay.
- `LoogaLayoutElement` supplies per-child minimum, preferred, maximum, flexible, and ignore-layout overrides.
- `LoogaContentFitter` sizes a leaf or wrapper from itself, its first child, or an explicitly assigned RectTransform. Containers normally do not need it because `LoogaLayout` already measures and reports its content.

### Nested Content-Sized Buttons

For a text-sized button:

1. Add `LoogaLayout` to the button.
2. Use Horizontal mode, add the desired padding, and set Width and Height to Content.
3. Leave the text child on its normal preferred size. TextMeshPro and Unity text components already report layout metrics.

For a navigation row containing those buttons:

1. Add another `LoogaLayout` to the row.
2. Use Horizontal mode and Content width if the row should wrap tightly around its buttons.
3. Use Authored, Fill Parent, or Clamped Content width when the row must respect a larger screen region.

Text changes mark Unity's layout chain dirty. The button recalculates from the text, the row recalculates from the buttons, and higher parents receive the new preferred size during the same layout rebuild.

### Sizing Modes

Container axes support:

- `Authored`: preserve the RectTransform's authored size.
- `Content`: use the children's reported size.
- `Fill Parent`: request available space from the parent layout.
- `Fixed`: use the configured dimension.
- `Clamped Content`: fit content between configured minimum and maximum dimensions.

Child axes support:

- `Content`: use each child's preferred size.
- `Fill`: distribute available space, respecting child minimums, maximums, and flexible weights.
- `Uniform`: use the largest preferred child size for every child.
- `Fixed`: use the configured child dimension, still respecting child minimums and maximums.

Grid uses its own fixed or largest-content cell sizing. Flow wraps children into rows according to the container's available width.

### Guidance

- Use `LoogaLayout` on containers instead of combining a Unity layout group with `ContentSizeFitter`.
- Use `LoogaContentFitter` for leaf graphics or wrappers that must mirror one source, not for normal layout containers.
- Avoid making an axis Content-sized while its children Fill that same axis. The custom inspector warns about this sizing cycle.
- Add `LoogaLayoutElement` only when a child needs limits, flexible weight, explicit sizing, or exclusion from its parent layout.
- Core layout has no `Update`, LINQ, or R3 dependency. Reusable buffers keep steady-state rebuilds allocation-conscious.

## Procedural UI Shapes

Looga UI includes mesh-based UGUI shape graphics for authoring common UI primitives without baking sprite assets.
All shape components inherit normal Unity UI behavior through `MaskableGraphic`, so they can live under canvases, be tinted, raycasted, masked, and animated like other UI graphics.

### Components

- `LoogaUICircle` draws a disc, ring, or arc using a configurable segment count.
- `LoogaUIPolygon` draws regular equal-sided polygons with a configurable side count, radius, rotation, and per-corner rounding.
- `LoogaUICustomShape` draws designer-authored point shapes, including concave polygons through ear-clipping triangulation and per-point corner rounding.
- `LoogaUILineRenderer` draws straight lines or multi-point paths with configurable width, caps, joins, closed paths, and dashed strokes.

### Recommended Usage

1. Add the desired component from `LoogaSoft/UI/Shapes`.
2. Size the object with its `RectTransform`.
3. Use normalized point lists for custom shapes and line paths. `(-0.5, -0.5)` maps to bottom-left and `(0.5, 0.5)` maps to top-right.
4. Select custom shapes or line renderers in the Scene view to drag existing points. Green midpoint handles insert new points between existing ones.
5. Use fill/stroke settings for closed shapes, and line settings for paths.
6. For dashed paths, `Dash Length`, `Gap Length`, and `Dash Offset` are measured in local UI units, so they scale naturally with the RectTransform.

## UI Shadow

`LoogaUIShadow` adds a cached, shape-based shadow to a normal Unity UI `Graphic`.
The component generates a blurred alpha texture from the source sprite, draws it with a hidden `RawImage`, and only regenerates when the source or shadow settings change. Sprite-backed Unity `Image` components are sampled with awareness of Simple, Sliced, Tiled, and Filled image modes.

The effect can be used as an outer shadow/glow or an inner shadow/glow by changing `Mode` and `Color`.

### Recommended Usage

1. Add `LoogaUIShadow` to a UI `Image`.
2. Set `Mode` to `Outer` for shadows/glows behind the graphic, or `Inner` for an inward edge glow/shadow.
3. Tune `Color`, `Offset`, `Softness`, `Spread`, `Quality`, and `Resolution Scale`.
4. For inner effects, `Offset` biases where the inward edge appears instead of moving the renderer.
5. Keep `Resolution Scale` below `1` for large/soft shadows unless the shape needs extra precision.
6. Leave `Clip Source` enabled when transparent sprites should not be darkened by their own shadow.
7. Leave `Dither` enabled for wide soft shadows to reduce visible alpha banding.

### Notes

The first implementation targets sprite-backed UGUI graphics. Source sprite alpha is read directly when possible, then via a temporary render texture fallback for non-readable textures. If both paths fail, the component falls back to a rectangular alpha mask and logs a warning once per rebuild path.
`Deallocate On Disable` should usually stay enabled. Disable it only for UI that is hidden and shown frequently enough that keeping the generated texture cached is worth the extra memory.

### Optional UniTask Support

Install UniTask through Unity Package Manager, then use `LoogaSoft > UI > Enable UniTask Support`.
Looga UI detects the package through a separate optional assembly, so this works for Git, registry,
embedded, and local package installations without modifying Looga UI's cached `.asmdef` files.
When enabled, `LoogaUIShadow` can snapshot Unity object data on the main thread, build expensive
blur pixels on UniTask's thread pool, and apply the generated texture back on the main thread.

## UI Soft Mask

`LoogaUISoftMask` masks child UGUI graphics using the alpha or color channel of a parent mask graphic, sprite, or texture.
It works like a soft version of Unity's standard UI Mask: masked children use a replacement material that samples the mask texture and multiplies their alpha by the sampled value. Nested Looga soft masks are supported up to four active parent masks per target.

### Recommended Usage

1. Add `LoogaUISoftMask` to the parent mask object.
2. Use the object's own `Image`/`RawImage` as the default mask source, or assign a sprite/texture explicitly.
3. Leave Target Mode as `Automatic Children` for normal UI. Use `Manual Targets` for performance-sensitive hierarchies where only specific children need masking.
4. Add `LoogaUISoftMaskTarget` manually only when using `Manual Targets`.

### Current Scope

The first pass supports standard UGUI graphics and the default UI shader replacement. TMP/custom shader support should be added intentionally in a later pass.

## UI Outline

`LoogaUIOutline` adds a color/alpha based outline to a UGUI `Graphic`. It uses a replacement UI shader that samples the source alpha around the current pixel and can expand the mesh so the outline is not clipped by the original rect.

### Recommended Usage

1. Add `LoogaUIOutline` to an `Image` or other UGUI `Graphic`.
2. Set `Color`, `Thickness`, `Softness`, and `Quality`.
3. Leave `Expand Mesh` enabled for outlines that need to extend outside the original graphic bounds.

### Current Scope

The first pass targets standard UGUI graphics and default UI-style materials. Complex custom shaders or TMP-specific outline support should be added as explicit integrations later.

## UI Gradient Overlay

`LoogaUIGradientOverlay` adds a directional color overlay to a UGUI `Graphic`.
It is intended for common UI styling passes such as top-to-bottom shade, edge tinting, rarity panels, or subtle button depth.
The gradient is evaluated in RectTransform space, so sliced and tiled image UVs do not cause the gradient to repeat.

### Recommended Usage

1. Add `LoogaUIGradientOverlay` to an `Image` or other UGUI `Graphic`.
2. Set `Start Color`, `End Color`, `Angle`, and `Intensity`.
3. Use transparent alpha on either gradient color when you only want a subtle edge or directional tint.

## UI Shine

`LoogaUIShine` adds a configurable highlight band to a UGUI `Graphic`.
It can be used as a looping sheen, a one-shot attention pulse, or a manually driven highlight by calling `Play`, `Stop`, or `SetPosition`.
The shine band is evaluated in RectTransform space, so it sweeps across the UI element as a whole even when the source image is sliced or tiled.

### Recommended Usage

1. Add `LoogaUIShine` to an `Image` or other UGUI `Graphic`.
2. Tune `Color`, `Angle`, `Width`, and `Softness`.
3. Enable `Play On Enable` and `Loop` for ambient sheen effects.
4. Disable `Loop` and call `Play()` for one-shot reward, unlock, or hover feedback.

### Notes

Gradient and shine use the same styled UI shader, so they can be stacked on the same UGUI graphic without replacing each other.
