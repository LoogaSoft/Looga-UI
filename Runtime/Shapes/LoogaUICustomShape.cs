using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [AddComponentMenu("LoogaSoft/UI FX/Shapes/Looga UI Custom Shape")]
    public sealed class LoogaUICustomShape : LoogaUIShapeGraphic
    {
        static readonly List<Vector2> LocalPoints = new();
        static readonly List<Vector2> RoundedPoints = new();

        [Header("Shape")]
        [SerializeField, Tooltip("Shape points in normalized RectTransform space. (-0.5, -0.5) is bottom-left and (0.5, 0.5) is top-right.")]
        List<Vector2> _points = new()
        {
            new(-0.5f, -0.5f),
            new(0f, 0.5f),
            new(0.5f, -0.5f)
        };

        [SerializeField, Tooltip("Connects the last point back to the first point. Filled shapes are always treated as closed.")]
        bool _closed = true;

        [Header("Corners")]
        [SerializeField, Min(0f), Tooltip("Default corner radius in local UI units.")]
        float _cornerRadius = 0f;

        [SerializeField, Tooltip("Override each point's corner radius individually. The list syncs to the point count.")]
        List<float> _cornerRadii = new();

        [SerializeField, Range(2, 16), Tooltip("How many points are added per rounded corner.")]
        int _cornerSegments = 6;

        public IReadOnlyList<Vector2> Points => _points;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            BuildLocalPoints(rect, LocalPoints);
            LoogaUIShapeMeshUtility.AddRoundedPolygonPath(LocalPoints, _cornerRadii, _cornerRadius, _cornerSegments, RoundedPoints);
            IReadOnlyList<Vector2> renderPoints = RoundedPoints.Count > 0 ? RoundedPoints : LocalPoints;

            if (_fill && renderPoints.Count >= 3)
            {
                LoogaUIShapeMeshUtility.AddPolygonFill(vh, rect, renderPoints, FillColor);
            }

            if (_stroke && _strokeWidth > 0f && renderPoints.Count >= 2)
            {
                LoogaUIShapeMeshUtility.AddPolyline(vh, renderPoints, _strokeWidth, StrokeColor(), _closed || _fill, LoogaUILineCap.Butt, LoogaUILineJoin.Round, 12);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _points ??= new List<Vector2>();
            _cornerRadius = Mathf.Max(0f, _cornerRadius);
            _cornerSegments = Mathf.Clamp(_cornerSegments, 2, 16);
            SyncCornerRadii();
            SetVerticesDirty();
        }

        void SyncCornerRadii()
        {
            _cornerRadii ??= new List<float>();
            while (_cornerRadii.Count < _points.Count)
            {
                _cornerRadii.Add(_cornerRadius);
            }

            while (_cornerRadii.Count > _points.Count)
            {
                _cornerRadii.RemoveAt(_cornerRadii.Count - 1);
            }

            for (int i = 0; i < _cornerRadii.Count; i++)
            {
                _cornerRadii[i] = Mathf.Max(0f, _cornerRadii[i]);
            }
        }

        void BuildLocalPoints(Rect rect, List<Vector2> localPoints)
        {
            localPoints.Clear();
            if (_points == null)
            {
                return;
            }

            for (int i = 0; i < _points.Count; i++)
            {
                localPoints.Add(LoogaUIShapeMeshUtility.RectPoint(rect, _points[i]));
            }
        }
    }
}
