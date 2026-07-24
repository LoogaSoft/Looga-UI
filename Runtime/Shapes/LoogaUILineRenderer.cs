using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [AddComponentMenu("LoogaSoft/UI FX/Shapes/Looga UI Line Renderer")]
    public sealed class LoogaUILineRenderer : MaskableGraphic
    {
        static readonly List<Vector2> LocalPoints = new();

        [Header("Path")]
        [SerializeField, Tooltip("Line points in normalized RectTransform space. (-0.5, -0.5) is bottom-left and (0.5, 0.5) is top-right.")]
        List<Vector2> _points = new()
        {
            new(-0.5f, 0f),
            new(0.5f, 0f)
        };

        [SerializeField, Tooltip("Connects the final point back to the first point.")]
        bool _closed;

        [Header("Line")]
        [SerializeField, Min(0f), Tooltip("Line thickness in local UI units. In overlay UI this roughly maps to pixels.")]
        float _width = 4f;

        [SerializeField, Tooltip("Cap style used at the ends of open paths.")]
        LoogaUILineCap _cap = LoogaUILineCap.Round;

        [SerializeField, Tooltip("How corners between path segments are filled.")]
        LoogaUILineJoin _join = LoogaUILineJoin.Round;

        [SerializeField, Range(6, 32), Tooltip("Segment count used for round caps and rounded path joints.")]
        int _roundSegments = 12;

        [Header("Dashes")]
        [SerializeField, Tooltip("Draws the line as repeated dash segments.")]
        bool _dashed;

        [SerializeField, Min(0.001f), Tooltip("Length of each dash in local UI units.")]
        float _dashLength = 18f;

        [SerializeField, Min(0f), Tooltip("Space between dashes in local UI units.")]
        float _gapLength = 8f;

        [SerializeField, Tooltip("Moves the dash pattern along the path.")]
        float _dashOffset;

        public IReadOnlyList<Vector2> Points => _points;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            BuildLocalPoints(rect, LocalPoints);
            LoogaUIShapeMeshUtility.AddPolyline(vh, LocalPoints, _width, color, _closed, _cap, _join, _roundSegments, _dashed, _dashLength, _gapLength, _dashOffset);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _width = Mathf.Max(0f, _width);
            _roundSegments = Mathf.Clamp(_roundSegments, 6, 32);
            _dashLength = Mathf.Max(0.001f, _dashLength);
            _gapLength = Mathf.Max(0f, _gapLength);
            _points ??= new List<Vector2>();
            SetVerticesDirty();
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
