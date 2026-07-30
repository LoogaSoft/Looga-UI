using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions
{
    [AddComponentMenu("LoogaSoft/UI/Shapes/Looga UI Circle")]
    public sealed class LoogaUICircle : LoogaUIShapeGraphic
    {
        [Header("Circle")]
        [SerializeField, Range(0.01f, 1f), Tooltip("How much of the RectTransform's smallest dimension the circle uses.")]
        float _radiusScale = 1f;

        [SerializeField, Range(0f, 0.99f), Tooltip("Creates a ring by removing this normalized amount from the center.")]
        float _innerRadius = 0f;

        [SerializeField, Range(3, 256), Tooltip("Number of segments used to draw the circle. Higher values are smoother.")]
        int _segments = 64;

        [SerializeField, Range(0f, 360f), Tooltip("Angle where the disc begins.")]
        float _startAngle = 0f;

        [SerializeField, Range(0f, 360f), Tooltip("How much of the circle is drawn.")]
        float _arc = 360f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f * _radiusScale;
            if (radius <= 0f)
            {
                return;
            }

            if (_fill)
            {
                LoogaUIShapeMeshUtility.AddDisc(vh, rect, rect.center, radius, radius * _innerRadius, _startAngle, _arc, _segments, FillColor);
            }

            if (_stroke && _strokeWidth > 0f)
            {
                float strokeRadius = radius;
                float strokeWidth = _strokeWidth;
                if (_strokeAlignment == LoogaUIStrokeAlignment.Inside)
                {
                    strokeRadius -= strokeWidth * 0.5f;
                }
                else if (_strokeAlignment == LoogaUIStrokeAlignment.Outside)
                {
                    strokeRadius += strokeWidth * 0.5f;
                }

                float outerRadius = Mathf.Max(0f, strokeRadius + strokeWidth * 0.5f);
                float innerRadius = Mathf.Max(0f, strokeRadius - strokeWidth * 0.5f);
                LoogaUIShapeMeshUtility.AddDisc(vh, rect, rect.center, outerRadius, innerRadius, _startAngle, _arc, _segments, (_, _) => StrokeColor());
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _segments = Mathf.Clamp(_segments, 3, 256);
            SetVerticesDirty();
        }
    }
}
