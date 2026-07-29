# Looga UI FX

Looga UI FX contains reusable UGUI visual effects for LoogaSoft projects.

## Procedural UI Shapes

Looga UI FX includes mesh-based UGUI shape graphics for authoring common UI primitives without baking sprite assets.
All shape components inherit normal Unity UI behavior through `MaskableGraphic`, so they can live under canvases, be tinted, raycasted, masked, and animated like other UI graphics.

### Components

- `LoogaUICircle` draws a disc, ring, or arc using a configurable segment count.
- `LoogaUIPolygon` draws regular equal-sided polygons with a configurable side count, radius, rotation, and per-corner rounding.
- `LoogaUICustomShape` draws designer-authored point shapes, including concave polygons through ear-clipping triangulation and per-point corner rounding.
- `LoogaUILineRenderer` draws straight lines or multi-point paths with configurable width, caps, joins, closed paths, and dashed strokes.

### Recommended Usage

1. Add the desired component from `LoogaSoft/UI FX/Shapes`.
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
6. Leave `Clip Outer Shadow Behind Source` enabled when transparent sprites should not be darkened by their own shadow.

### Notes

The first implementation targets sprite-backed UGUI graphics. Source sprite alpha is read directly when possible, then via a temporary render texture fallback for non-readable textures. If both paths fail, the component falls back to a rectangular alpha mask and logs a warning once per rebuild path.

### Optional UniTask Support

Use `LoogaSoft > UI FX > Enable UniTask Support` after installing UniTask. When enabled, `LoogaUIShadow` exposes an async rebuild option that snapshots Unity object data on the main thread, builds the expensive blur pixels on UniTask's thread pool, and applies the generated texture back on the main thread.

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
