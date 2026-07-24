using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [AddComponentMenu("LoogaSoft/UI FX/Shapes/Looga UI Polygon")]
    public sealed class LoogaUIPolygon : LoogaUIShapeGraphic
    {
        static readonly List<Vector2> Points = new();
        static readonly List<Vector2> RoundedPoints = new();

        [Header("Polygon")]
        [SerializeField, Min(3), Tooltip("Number of equal-length sides in the regular polygon.")]
        int _sides = 6;

        [SerializeField, Range(0.01f, 1f), Tooltip("How much of the RectTransform's smallest dimension the polygon uses.")]
        float _radiusScale = 1f;

        [SerializeField, Range(0f, 360f), Tooltip("Rotates the polygon around its center.")]
        float _rotation = 0f;

        [Header("Corners")]
        [SerializeField, Min(0f), Tooltip("Default corner radius in local UI units.")]
        float _cornerRadius = 0f;

        [SerializeField, Tooltip("Override each polygon corner radius individually. The list syncs to the current side count.")]
        List<float> _cornerRadii = new();

        [SerializeField, Range(2, 16), Tooltip("How many points are added per rounded corner.")]
        int _cornerSegments = 6;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            BuildPoints(rect, Points);
            LoogaUIShapeMeshUtility.AddRoundedPolygonPath(Points, _cornerRadii, _cornerRadius, _cornerSegments, RoundedPoints);
            IReadOnlyList<Vector2> renderPoints = RoundedPoints.Count > 0 ? RoundedPoints : Points;

            if (_fill)
            {
                LoogaUIShapeMeshUtility.AddPolygonFill(vh, rect, renderPoints, FillColor);
            }

            if (_stroke && _strokeWidth > 0f)
            {
                float strokeWidth = _strokeWidth;
                if (_strokeAlignment != LoogaUIStrokeAlignment.Center)
                {
                    // Regular polygon offsets are intentionally approximated by width. Exact inset/outset
                    // beveling belongs in a later precision pass if designers need it.
                    strokeWidth *= _strokeAlignment == LoogaUIStrokeAlignment.Inside ? 0.9f : 1.1f;
                }

                LoogaUIShapeMeshUtility.AddPolyline(vh, renderPoints, strokeWidth, StrokeColor(), true, LoogaUILineCap.Butt, LoogaUILineJoin.Round, 12);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _sides = Mathf.Max(3, _sides);
            _cornerRadius = Mathf.Max(0f, _cornerRadius);
            _cornerSegments = Mathf.Clamp(_cornerSegments, 2, 16);
            SyncCornerRadii();
            SetVerticesDirty();
        }

        void SyncCornerRadii()
        {
            _cornerRadii ??= new List<float>();
            while (_cornerRadii.Count < _sides)
            {
                _cornerRadii.Add(_cornerRadius);
            }

            while (_cornerRadii.Count > _sides)
            {
                _cornerRadii.RemoveAt(_cornerRadii.Count - 1);
            }

            for (int i = 0; i < _cornerRadii.Count; i++)
            {
                _cornerRadii[i] = Mathf.Max(0f, _cornerRadii[i]);
            }
        }

        void BuildPoints(Rect rect, List<Vector2> points)
        {
            points.Clear();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f * _radiusScale;
            float angleStep = 360f / _sides;
            for (int i = 0; i < _sides; i++)
            {
                float angle = (_rotation + angleStep * i) * Mathf.Deg2Rad;
                points.Add(rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
    }
}
