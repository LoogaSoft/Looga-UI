using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions
{
    [ExecuteAlways]
    public abstract class LoogaUIShapeGraphic : MaskableGraphic
    {
        [Header("Fill")]
        [SerializeField, Tooltip("Draws the filled body of the shape.")]
        protected bool _fill = true;

        [SerializeField, Tooltip("How the fill color is evaluated across the shape.")]
        protected LoogaUIShapeFillMode _fillMode = LoogaUIShapeFillMode.Solid;

        [SerializeField, Tooltip("Main fill color. The Graphic color still tints the final result.")]
        protected Color _fillColor = Color.white;

        [SerializeField, Tooltip("Second color used by linear and radial gradients.")]
        protected Color _gradientColor = Color.gray;

        [SerializeField, Range(0f, 360f), Tooltip("Direction for linear gradient fills.")]
        protected float _gradientAngle = 90f;

        [Header("Stroke")]
        [SerializeField, Tooltip("Draws an outline around the shape.")]
        protected bool _stroke;

        [SerializeField, Min(0f), Tooltip("Stroke thickness in local UI units. In overlay UI this roughly maps to pixels.")]
        protected float _strokeWidth = 2f;

        [SerializeField, Tooltip("Stroke color. The Graphic color still tints the final result.")]
        protected Color _strokeColor = Color.white;

        [SerializeField, Tooltip("Where the stroke is placed relative to the shape edge. Center is the most stable for complex shapes.")]
        protected LoogaUIStrokeAlignment _strokeAlignment = LoogaUIStrokeAlignment.Center;

        protected Color32 FillColor(Vector2 position, Rect rect)
        {
            Color sampled = _fillMode switch
            {
                LoogaUIShapeFillMode.LinearGradient => Color.Lerp(_fillColor, _gradientColor, LinearGradientT(position, rect)),
                LoogaUIShapeFillMode.RadialGradient => Color.Lerp(_fillColor, _gradientColor, RadialGradientT(position, rect)),
                _ => _fillColor
            };

            return sampled * color;
        }

        protected Color32 StrokeColor()
        {
            return _strokeColor * color;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _strokeWidth = Mathf.Max(0f, _strokeWidth);
            SetVerticesDirty();
        }

        float LinearGradientT(Vector2 position, Rect rect)
        {
            float radians = _gradientAngle * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 normalized = new(
                rect.width > 0f ? (position.x - rect.center.x) / rect.width : 0f,
                rect.height > 0f ? (position.y - rect.center.y) / rect.height : 0f);

            return Mathf.Clamp01(Vector2.Dot(normalized, direction) + 0.5f);
        }

        static float RadialGradientT(Vector2 position, Rect rect)
        {
            Vector2 half = rect.size * 0.5f;
            if (half.x <= 0f || half.y <= 0f)
            {
                return 0f;
            }

            Vector2 normalized = new((position.x - rect.center.x) / half.x, (position.y - rect.center.y) / half.y);
            return Mathf.Clamp01(normalized.magnitude);
        }
    }
}
