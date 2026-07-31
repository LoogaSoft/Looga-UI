using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Looga UI/Layout/Looga Layout")]
    public sealed class LoogaLayout : LayoutGroup, ILayoutSelfController
    {
        [SerializeField, Tooltip("How children are arranged inside this container.")]
        LoogaLayoutMode _mode = LoogaLayoutMode.Horizontal;

        [SerializeField, Tooltip("How this container determines its own width.")]
        LoogaLayoutSizeMode _width = LoogaLayoutSizeMode.Authored;

        [SerializeField, Tooltip("How this container determines its own height.")]
        LoogaLayoutSizeMode _height = LoogaLayoutSizeMode.Authored;

        [SerializeField, Tooltip("Width and height used by Fixed sizing.")]
        Vector2 _fixedSize = new(100f, 100f);

        [SerializeField, Min(0f), Tooltip("Smallest dimensions produced by Clamped Content sizing.")]
        Vector2 _minimumSize;

        [SerializeField, Tooltip("Largest dimensions produced by Clamped Content sizing. Zero means unlimited.")]
        Vector2 _maximumSize;

        [SerializeField, Tooltip("How child widths are chosen.")]
        LoogaLayoutChildSizeMode _childWidth = LoogaLayoutChildSizeMode.Content;

        [SerializeField, Tooltip("How child heights are chosen.")]
        LoogaLayoutChildSizeMode _childHeight = LoogaLayoutChildSizeMode.Content;

        [SerializeField, Tooltip("Width and height assigned when the matching child size mode is Fixed.")]
        Vector2 _fixedChildSize = new(100f, 30f);

        [SerializeField, Min(0f), Tooltip("Space between consecutive children.")]
        float _spacing;

        [SerializeField, Min(0f), Tooltip("Vertical space between rows in Flow mode.")]
        float _lineSpacing;

        [SerializeField, Tooltip("Arranges children in reverse hierarchy order.")]
        bool _reverseOrder;

        [SerializeField, Tooltip("How Grid mode determines its row and column count.")]
        LoogaGridConstraint _gridConstraint = LoogaGridConstraint.FixedColumns;

        [SerializeField, Min(1), Tooltip("Column or row count used by fixed Grid constraints.")]
        int _gridConstraintCount = 2;

        [SerializeField, Tooltip("How Grid mode determines its cell dimensions.")]
        LoogaGridCellMode _gridCellMode = LoogaGridCellMode.Fixed;

        [SerializeField, Tooltip("Cell dimensions used by Fixed Grid sizing.")]
        Vector2 _gridCellSize = new(100f, 100f);

        [SerializeField, Tooltip("Horizontal and vertical space between Grid cells.")]
        Vector2 _gridSpacing;

        readonly List<Component> _componentBuffer = new();
        readonly List<float> _childSizes = new();
        readonly List<float> _childMinimums = new();
        readonly List<float> _childPreferreds = new();
        readonly List<float> _childFlexible = new();
        readonly List<int> _orderedIndices = new();
        readonly List<int> _flowRowStarts = new();
        readonly List<int> _flowRowCounts = new();
        readonly List<float> _flowRowWidths = new();
        readonly List<float> _flowRowHeights = new();
        readonly List<float> _flowChildWidths = new();
        readonly List<float> _flowChildHeights = new();
        readonly List<float> _overlayWidths = new();

        Vector2 _contentMinimum;
        Vector2 _contentPreferred;
        Vector2 _reportedMinimum;
        Vector2 _reportedPreferred;

        public LoogaLayoutMode Mode => _mode;
        public Vector2 ContentMinimum => _contentMinimum;
        public Vector2 ContentPreferred => _contentPreferred;
        public Vector2 ReportedMinimum => _reportedMinimum;
        public Vector2 ReportedPreferred => _reportedPreferred;

        // Explicit layout policy must outrank decorative UI components such as
        // Image, whose native sprite dimensions are not the container's size.
        public override int layoutPriority => 1;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalculateContentMetrics(0, out float minimum, out float preferred, out float flexible);

            _contentMinimum.x = minimum;
            _contentPreferred.x = preferred;
            ApplySizePolicy(0, minimum, preferred, flexible, out minimum, out preferred, out flexible);
            _reportedMinimum.x = minimum;
            _reportedPreferred.x = preferred;

            SetLayoutInputForAxis(minimum, preferred, flexible, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalculateContentMetrics(1, out float minimum, out float preferred, out float flexible);

            _contentMinimum.y = minimum;
            _contentPreferred.y = preferred;
            ApplySizePolicy(1, minimum, preferred, flexible, out minimum, out preferred, out flexible);
            _reportedMinimum.y = minimum;
            _reportedPreferred.y = preferred;

            SetLayoutInputForAxis(minimum, preferred, flexible, 1);
        }

        public override void SetLayoutHorizontal()
        {
            ApplySelfSize(0);

            if (_mode is LoogaLayoutMode.Horizontal or LoogaLayoutMode.Vertical)
            {
                ArrangeLinearAxis(0);
            }
        }

        public override void SetLayoutVertical()
        {
            ApplySelfSize(1);

            switch (_mode)
            {
                case LoogaLayoutMode.Horizontal:
                case LoogaLayoutMode.Vertical:
                    ArrangeLinearAxis(1);
                    break;

                case LoogaLayoutMode.Grid:
                    ArrangeGrid();
                    break;

                case LoogaLayoutMode.Flow:
                    ArrangeFlow();
                    break;

                case LoogaLayoutMode.Overlay:
                    ArrangeOverlay();
                    break;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            _fixedSize = Vector2.Max(Vector2.zero, _fixedSize);
            _minimumSize = Vector2.Max(Vector2.zero, _minimumSize);
            _fixedChildSize = Vector2.Max(Vector2.zero, _fixedChildSize);
            _spacing = Mathf.Max(0f, _spacing);
            _lineSpacing = Mathf.Max(0f, _lineSpacing);
            _gridConstraintCount = Mathf.Max(1, _gridConstraintCount);
            _gridCellSize = Vector2.Max(Vector2.zero, _gridCellSize);
            _gridSpacing = Vector2.Max(Vector2.zero, _gridSpacing);

            for (int axis = 0; axis < 2; axis++)
            {
                if (_maximumSize[axis] > 0f)
                {
                    _maximumSize[axis] = Mathf.Max(_minimumSize[axis], _maximumSize[axis]);
                }
            }
        }
#endif

        void CalculateContentMetrics(int axis, out float minimum, out float preferred, out float flexible)
        {
            switch (_mode)
            {
                case LoogaLayoutMode.Horizontal:
                    CalculateLinearMetrics(axis, 0, out minimum, out preferred, out flexible);
                    return;

                case LoogaLayoutMode.Vertical:
                    CalculateLinearMetrics(axis, 1, out minimum, out preferred, out flexible);
                    return;

                case LoogaLayoutMode.Grid:
                    CalculateGridMetrics(axis, out minimum, out preferred, out flexible);
                    return;

                case LoogaLayoutMode.Flow:
                    CalculateFlowMetrics(axis, out minimum, out preferred, out flexible);
                    return;

                default:
                    CalculateOverlayMetrics(axis, out minimum, out preferred, out flexible);
                    return;
            }
        }

        void CalculateLinearMetrics(int axis, int mainAxis, out float minimum, out float preferred, out float flexible)
        {
            float paddingSize = axis == 0 ? padding.horizontal : padding.vertical;
            LoogaLayoutChildSizeMode sizeMode = axis == 0 ? _childWidth : _childHeight;
            float uniformSize = sizeMode == LoogaLayoutChildSizeMode.Uniform
                ? LargestChildPreferred(axis)
                : 0f;
            minimum = paddingSize;
            preferred = paddingSize;
            flexible = 0f;

            if (rectChildren.Count == 0)
            {
                return;
            }

            bool main = axis == mainAxis;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                ReadConfiguredChildMetrics(
                    rectChildren[i],
                    axis,
                    sizeMode,
                    uniformSize,
                    out float childMinimum,
                    out float childPreferred,
                    out float childFlexible);

                if (main)
                {
                    minimum += childMinimum;
                    preferred += childPreferred;
                    flexible += Mathf.Max(0f, childFlexible);
                }
                else
                {
                    minimum = Mathf.Max(minimum, childMinimum + paddingSize);
                    preferred = Mathf.Max(preferred, childPreferred + paddingSize);
                    flexible = Mathf.Max(flexible, childFlexible);
                }
            }

            if (main)
            {
                float totalSpacing = _spacing * Mathf.Max(0, rectChildren.Count - 1);
                minimum += totalSpacing;
                preferred += totalSpacing;
            }

            preferred = Mathf.Max(minimum, preferred);
        }

        void CalculateGridMetrics(int axis, out float minimum, out float preferred, out float flexible)
        {
            GetGridDimensions(out int columns, out int rows, out Vector2 cellSize);
            float paddingSize = axis == 0 ? padding.horizontal : padding.vertical;
            int count = axis == 0 ? columns : rows;
            float cell = cellSize[axis];
            float spacing = _gridSpacing[axis];

            minimum = paddingSize + count * cell + Mathf.Max(0, count - 1) * spacing;
            preferred = minimum;
            flexible = 0f;
        }

        void CalculateFlowMetrics(int axis, out float minimum, out float preferred, out float flexible)
        {
            LoogaLayoutChildSizeMode sizeMode = axis == 0 ? _childWidth : _childHeight;
            float uniformSize = sizeMode == LoogaLayoutChildSizeMode.Uniform
                ? LargestChildPreferred(axis)
                : 0f;

            if (axis == 0)
            {
                minimum = padding.horizontal;
                preferred = padding.horizontal;

                for (int i = 0; i < rectChildren.Count; i++)
                {
                    ReadConfiguredChildMetrics(
                        rectChildren[i],
                        0,
                        sizeMode,
                        uniformSize,
                        out float childMinimum,
                        out float childPreferred,
                        out _);
                    minimum = Mathf.Max(minimum, childMinimum + padding.horizontal);
                    preferred += childPreferred;
                }

                preferred += _spacing * Mathf.Max(0, rectChildren.Count - 1);
                preferred = Mathf.Max(minimum, preferred);
                flexible = 0f;
                return;
            }

            BuildFlowRows();
            float minimumHeight = 0f;
            float preferredHeight = padding.vertical;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                ReadConfiguredChildMetrics(
                    rectChildren[i],
                    1,
                    sizeMode,
                    uniformSize,
                    out float childMinimum,
                    out _,
                    out _);
                minimumHeight = Mathf.Max(minimumHeight, childMinimum);
            }

            for (int i = 0; i < _flowRowHeights.Count; i++)
            {
                preferredHeight += _flowRowHeights[i];
            }

            preferredHeight += _lineSpacing * Mathf.Max(0, _flowRowHeights.Count - 1);
            minimum = padding.vertical + minimumHeight;
            preferred = Mathf.Max(minimum, preferredHeight);
            flexible = 0f;
        }

        void CalculateOverlayMetrics(int axis, out float minimum, out float preferred, out float flexible)
        {
            float paddingSize = axis == 0 ? padding.horizontal : padding.vertical;
            LoogaLayoutChildSizeMode sizeMode = axis == 0 ? _childWidth : _childHeight;
            float uniformSize = sizeMode == LoogaLayoutChildSizeMode.Uniform
                ? LargestChildPreferred(axis)
                : 0f;
            minimum = paddingSize;
            preferred = paddingSize;
            flexible = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                ReadConfiguredChildMetrics(
                    rectChildren[i],
                    axis,
                    sizeMode,
                    uniformSize,
                    out float childMinimum,
                    out float childPreferred,
                    out float childFlexible);
                minimum = Mathf.Max(minimum, childMinimum + paddingSize);
                preferred = Mathf.Max(preferred, childPreferred + paddingSize);
                flexible = Mathf.Max(flexible, childFlexible);
            }

            preferred = Mathf.Max(minimum, preferred);
        }

        void ApplySizePolicy(
            int axis,
            float contentMinimum,
            float contentPreferred,
            float contentFlexible,
            out float minimum,
            out float preferred,
            out float flexible)
        {
            LoogaLayoutSizeMode mode = axis == 0 ? _width : _height;
            float current = rectTransform.rect.size[axis];

            switch (mode)
            {
                case LoogaLayoutSizeMode.Authored:
                    minimum = current;
                    preferred = current;
                    flexible = 0f;
                    break;

                case LoogaLayoutSizeMode.FillParent:
                    minimum = contentMinimum;
                    preferred = contentPreferred;
                    flexible = Mathf.Max(1f, contentFlexible);
                    break;

                case LoogaLayoutSizeMode.Fixed:
                    minimum = _fixedSize[axis];
                    preferred = minimum;
                    flexible = 0f;
                    break;

                case LoogaLayoutSizeMode.ClampedContent:
                    float maximum = _maximumSize[axis] > 0f ? _maximumSize[axis] : float.PositiveInfinity;
                    minimum = Mathf.Clamp(contentMinimum, _minimumSize[axis], maximum);
                    preferred = Mathf.Clamp(contentPreferred, minimum, maximum);
                    flexible = 0f;
                    break;

                default:
                    minimum = contentMinimum;
                    preferred = contentPreferred;
                    flexible = 0f;
                    break;
            }
        }

        void ApplySelfSize(int axis)
        {
            LoogaLayoutSizeMode mode = axis == 0 ? _width : _height;
            if (mode == LoogaLayoutSizeMode.Authored || ParentControlsRect())
            {
                return;
            }

            float target = mode switch
            {
                LoogaLayoutSizeMode.Fixed => _fixedSize[axis],
                LoogaLayoutSizeMode.FillParent => ParentSize(axis),
                LoogaLayoutSizeMode.ClampedContent => ClampedContentSize(axis),
                _ => _contentPreferred[axis]
            };

            DrivenTransformProperties property = axis == 0
                ? DrivenTransformProperties.SizeDeltaX
                : DrivenTransformProperties.SizeDeltaY;

            m_Tracker.Add(this, rectTransform, property);
            rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, target);
        }

        void ArrangeLinearAxis(int axis)
        {
            int mainAxis = _mode == LoogaLayoutMode.Horizontal ? 0 : 1;
            bool main = axis == mainAxis;
            LoogaLayoutChildSizeMode sizeMode = axis == 0 ? _childWidth : _childHeight;
            float available = rectTransform.rect.size[axis] - (axis == 0 ? padding.horizontal : padding.vertical);

            PopulateOrder();
            if (!main && sizeMode == LoogaLayoutChildSizeMode.Fill)
            {
                BuildCrossAxisFillSizes(axis, available);
            }
            else
            {
                BuildChildSizes(
                    axis,
                    sizeMode,
                    main ? available - _spacing * Mathf.Max(0, rectChildren.Count - 1) : available);
            }

            if (main)
            {
                float total = Sum(_childSizes) + _spacing * Mathf.Max(0, _childSizes.Count - 1);
                float position = AxisPaddingStart(axis) + Mathf.Max(0f, available - total) * Alignment(axis);

                for (int i = 0; i < _orderedIndices.Count; i++)
                {
                    RectTransform child = rectChildren[_orderedIndices[i]];
                    float size = _childSizes[i];
                    SetChildAlongAxis(child, axis, position, size);
                    position += size + _spacing;
                }

                return;
            }

            float alignment = Alignment(axis);
            for (int i = 0; i < _orderedIndices.Count; i++)
            {
                RectTransform child = rectChildren[_orderedIndices[i]];
                float size = _childSizes[i];
                float position = AxisPaddingStart(axis) + Mathf.Max(0f, available - size) * alignment;
                SetChildAlongAxis(child, axis, position, size);
            }
        }

        void ArrangeGrid()
        {
            GetGridDimensions(out int columns, out int rows, out Vector2 cellSize);
            if (rectChildren.Count == 0)
            {
                return;
            }

            PopulateOrder();

            float totalWidth = columns * cellSize.x + Mathf.Max(0, columns - 1) * _gridSpacing.x;
            float totalHeight = rows * cellSize.y + Mathf.Max(0, rows - 1) * _gridSpacing.y;
            float availableWidth = rectTransform.rect.width - padding.horizontal;
            float availableHeight = rectTransform.rect.height - padding.vertical;
            float startX = padding.left + Mathf.Max(0f, availableWidth - totalWidth) * Alignment(0);
            float startY = padding.top + Mathf.Max(0f, availableHeight - totalHeight) * Alignment(1);

            for (int i = 0; i < _orderedIndices.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                RectTransform child = rectChildren[_orderedIndices[i]];
                float x = startX + column * (cellSize.x + _gridSpacing.x);
                float y = startY + row * (cellSize.y + _gridSpacing.y);

                SetChildAlongAxis(child, 0, x, cellSize.x);
                SetChildAlongAxis(child, 1, y, cellSize.y);
            }
        }

        void ArrangeFlow()
        {
            BuildFlowRows();
            float availableWidth = rectTransform.rect.width - padding.horizontal;
            float availableHeight = rectTransform.rect.height - padding.vertical;
            float totalHeight = Sum(_flowRowHeights) + _lineSpacing * Mathf.Max(0, _flowRowHeights.Count - 1);
            float y = padding.top + Mathf.Max(0f, availableHeight - totalHeight) * Alignment(1);

            for (int row = 0; row < _flowRowStarts.Count; row++)
            {
                int start = _flowRowStarts[row];
                int count = _flowRowCounts[row];
                float rowWidth = _flowRowWidths[row];
                float rowHeight = _flowRowHeights[row];
                float x = padding.left + Mathf.Max(0f, availableWidth - rowWidth) * Alignment(0);

                for (int item = 0; item < count; item++)
                {
                    int orderedIndex = start + item;
                    RectTransform child = rectChildren[_orderedIndices[orderedIndex]];
                    float width = _flowChildWidths[orderedIndex];
                    float height = _flowChildHeights[orderedIndex];
                    float childY = y + Mathf.Max(0f, rowHeight - height) * Alignment(1);

                    SetChildAlongAxis(child, 0, x, width);
                    SetChildAlongAxis(child, 1, childY, height);
                    x += width + _spacing;
                }

                y += rowHeight + _lineSpacing;
            }
        }

        void ArrangeOverlay()
        {
            PopulateOrder();
            float availableWidth = rectTransform.rect.width - padding.horizontal;
            float availableHeight = rectTransform.rect.height - padding.vertical;

            BuildChildSizes(0, _childWidth, availableWidth);
            _overlayWidths.Clear();
            _overlayWidths.AddRange(_childSizes);
            BuildChildSizes(1, _childHeight, availableHeight);

            for (int i = 0; i < _orderedIndices.Count; i++)
            {
                RectTransform child = rectChildren[_orderedIndices[i]];
                float width = _overlayWidths[i];
                float height = _childSizes[i];
                float x = padding.left + Mathf.Max(0f, availableWidth - width) * Alignment(0);
                float y = padding.top + Mathf.Max(0f, availableHeight - height) * Alignment(1);

                SetChildAlongAxis(child, 0, x, width);
                SetChildAlongAxis(child, 1, y, height);
            }
        }

        void BuildChildSizes(int axis, LoogaLayoutChildSizeMode mode, float available)
        {
            _childMinimums.Clear();
            _childPreferreds.Clear();
            _childFlexible.Clear();
            _childSizes.Clear();

            for (int i = 0; i < _orderedIndices.Count; i++)
            {
                RectTransform child = rectChildren[_orderedIndices[i]];
                ReadChildMetrics(child, axis, out float minimum, out float preferred, out float flexible);
                float maximum = ChildMaximum(child, axis);
                minimum = Mathf.Min(minimum, maximum);
                preferred = Mathf.Clamp(preferred, minimum, maximum);

                _childMinimums.Add(minimum);
                _childPreferreds.Add(preferred);
                _childFlexible.Add(Mathf.Max(0f, flexible));
            }

            switch (mode)
            {
                case LoogaLayoutChildSizeMode.Fixed:
                    for (int i = 0; i < _orderedIndices.Count; i++)
                    {
                        RectTransform child = rectChildren[_orderedIndices[i]];
                        _childSizes.Add(Mathf.Clamp(
                            _fixedChildSize[axis],
                            _childMinimums[i],
                            ChildMaximum(child, axis)));
                    }

                    return;

                case LoogaLayoutChildSizeMode.Fill:
                    BuildFillSizes(axis, available);
                    return;

                case LoogaLayoutChildSizeMode.Uniform:
                    float uniform = 0f;
                    for (int i = 0; i < _childPreferreds.Count; i++)
                    {
                        uniform = Mathf.Max(uniform, _childPreferreds[i]);
                    }

                    if (_orderedIndices.Count > 0 && uniform * _orderedIndices.Count > available)
                    {
                        uniform = Mathf.Max(0f, available / _orderedIndices.Count);
                    }

                    for (int i = 0; i < _orderedIndices.Count; i++)
                    {
                        RectTransform child = rectChildren[_orderedIndices[i]];
                        _childSizes.Add(Mathf.Clamp(
                            uniform,
                            _childMinimums[i],
                            ChildMaximum(child, axis)));
                    }

                    return;

                default:
                    _childSizes.AddRange(_childPreferreds);
                    ShrinkTowardMinimum(available);
                    return;
            }
        }

        void BuildCrossAxisFillSizes(int axis, float available)
        {
            _childSizes.Clear();

            for (int i = 0; i < _orderedIndices.Count; i++)
            {
                RectTransform child = rectChildren[_orderedIndices[i]];
                _childSizes.Add(Mathf.Clamp(
                    available,
                    ChildMinimum(child, axis),
                    ChildMaximum(child, axis)));
            }
        }

        void BuildFillSizes(int axis, float available)
        {
            for (int i = 0; i < _childMinimums.Count; i++)
            {
                _childSizes.Add(_childMinimums[i]);
            }

            float remaining = Mathf.Max(0f, available - Sum(_childSizes));
            const float Epsilon = 0.01f;

            // Redistribute after capped children reach their maximum so available
            // space is not silently stranded between otherwise flexible children.
            while (remaining > Epsilon)
            {
                float totalWeight = 0f;
                for (int i = 0; i < _childSizes.Count; i++)
                {
                    RectTransform child = rectChildren[_orderedIndices[i]];
                    if (_childSizes[i] + Epsilon < ChildMaximum(child, axis))
                    {
                        totalWeight += _childFlexible[i] > 0f ? _childFlexible[i] : 1f;
                    }
                }

                if (totalWeight <= 0f)
                {
                    break;
                }

                float distributed = 0f;
                for (int i = 0; i < _childSizes.Count; i++)
                {
                    RectTransform child = rectChildren[_orderedIndices[i]];
                    float maximum = ChildMaximum(child, axis);
                    if (_childSizes[i] + Epsilon >= maximum)
                    {
                        continue;
                    }

                    float weight = _childFlexible[i] > 0f ? _childFlexible[i] : 1f;
                    float addition = Mathf.Min(remaining * weight / totalWeight, maximum - _childSizes[i]);
                    _childSizes[i] += addition;
                    distributed += addition;
                }

                if (distributed <= Epsilon)
                {
                    break;
                }

                remaining -= distributed;
            }
        }

        void ShrinkTowardMinimum(float available)
        {
            float total = Sum(_childSizes);
            if (total <= available)
            {
                return;
            }

            float capacity = 0f;
            for (int i = 0; i < _childSizes.Count; i++)
            {
                capacity += Mathf.Max(0f, _childSizes[i] - _childMinimums[i]);
            }

            if (capacity <= 0f)
            {
                return;
            }

            float excess = Mathf.Min(total - available, capacity);
            for (int i = 0; i < _childSizes.Count; i++)
            {
                float childCapacity = Mathf.Max(0f, _childSizes[i] - _childMinimums[i]);
                _childSizes[i] -= excess * childCapacity / capacity;
            }
        }

        void BuildFlowRows()
        {
            PopulateOrder();
            _flowRowStarts.Clear();
            _flowRowCounts.Clear();
            _flowRowWidths.Clear();
            _flowRowHeights.Clear();
            _flowChildWidths.Clear();
            _flowChildHeights.Clear();

            float availableWidth = Mathf.Max(0f, rectTransform.rect.width - padding.horizontal);
            float uniformWidth = LargestChildPreferred(0);
            float uniformHeight = LargestChildPreferred(1);
            int rowStart = 0;
            int rowCount = 0;
            float rowWidth = 0f;
            float rowHeight = 0f;

            for (int i = 0; i < _orderedIndices.Count; i++)
            {
                RectTransform child = rectChildren[_orderedIndices[i]];
                float width = ChildSize(child, 0, _childWidth, availableWidth, uniformWidth);
                float height = ChildSize(
                    child,
                    1,
                    _childHeight,
                    rectTransform.rect.height - padding.vertical,
                    uniformHeight);
                float proposed = rowCount == 0 ? width : rowWidth + _spacing + width;

                if (rowCount > 0 && proposed > availableWidth)
                {
                    AddFlowRow(rowStart, rowCount, rowWidth, rowHeight);
                    rowStart = i;
                    rowCount = 0;
                    rowWidth = 0f;
                    rowHeight = 0f;
                    proposed = width;
                }

                _flowChildWidths.Add(width);
                _flowChildHeights.Add(height);
                rowWidth = proposed;
                rowHeight = Mathf.Max(rowHeight, height);
                rowCount++;
            }

            if (rowCount > 0)
            {
                AddFlowRow(rowStart, rowCount, rowWidth, rowHeight);
            }
        }

        void AddFlowRow(int start, int count, float width, float height)
        {
            _flowRowStarts.Add(start);
            _flowRowCounts.Add(count);
            _flowRowWidths.Add(width);
            _flowRowHeights.Add(height);
        }

        void GetGridDimensions(out int columns, out int rows, out Vector2 cellSize)
        {
            cellSize = _gridCellMode == LoogaGridCellMode.Fixed
                ? _gridCellSize
                : LargestChildSize();

            int count = rectChildren.Count;
            if (count == 0)
            {
                columns = 0;
                rows = 0;
                return;
            }

            switch (_gridConstraint)
            {
                case LoogaGridConstraint.FixedRows:
                    rows = Mathf.Min(_gridConstraintCount, count);
                    columns = Mathf.CeilToInt(count / (float)rows);
                    break;

                case LoogaGridConstraint.FixedColumns:
                    columns = Mathf.Min(_gridConstraintCount, count);
                    rows = Mathf.CeilToInt(count / (float)columns);
                    break;

                default:
                    float available = Mathf.Max(0f, rectTransform.rect.width - padding.horizontal);
                    columns = Mathf.Max(1, Mathf.FloorToInt((available + _gridSpacing.x) / Mathf.Max(1f, cellSize.x + _gridSpacing.x)));
                    rows = Mathf.CeilToInt(count / (float)columns);
                    break;
            }
        }

        Vector2 LargestChildSize()
        {
            Vector2 largest = Vector2.zero;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                ReadChildMetrics(rectChildren[i], 0, out _, out float width, out _);
                ReadChildMetrics(rectChildren[i], 1, out _, out float height, out _);
                largest.x = Mathf.Max(largest.x, width);
                largest.y = Mathf.Max(largest.y, height);
            }

            return largest;
        }

        float LargestChildPreferred(int axis)
        {
            float largest = 0f;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                ReadChildMetrics(rectChildren[i], axis, out _, out float preferred, out _);
                largest = Mathf.Max(largest, preferred);
            }

            return largest;
        }

        float ChildSize(
            RectTransform child,
            int axis,
            LoogaLayoutChildSizeMode mode,
            float available,
            float uniform)
        {
            ReadChildMetrics(child, axis, out float minimum, out float preferred, out _);
            float size = mode switch
            {
                LoogaLayoutChildSizeMode.Fill => available,
                LoogaLayoutChildSizeMode.Fixed => _fixedChildSize[axis],
                LoogaLayoutChildSizeMode.Uniform => uniform,
                _ => preferred
            };

            return Mathf.Clamp(size, minimum, ChildMaximum(child, axis));
        }

        void ReadChildMetrics(RectTransform child, int axis, out float minimum, out float preferred, out float flexible)
        {
            RefreshNestedLayoutMetrics(child, axis);
            minimum = LayoutUtility.GetMinSize(child, axis);
            preferred = Mathf.Max(minimum, LayoutUtility.GetPreferredSize(child, axis));
            flexible = LayoutUtility.GetFlexibleSize(child, axis);
        }

        static void RefreshNestedLayoutMetrics(RectTransform child, int axis)
        {
            if (!child.TryGetComponent(out LoogaLayout nestedLayout) || !nestedLayout.isActiveAndEnabled)
            {
                return;
            }

            // Unity can measure a parent before a nested layout has refreshed its own
            // preferred size. Resolve nested content from the leaves upward so a
            // content-sized chain never falls back to stale RectTransform dimensions.
            nestedLayout.CalculateLayoutInputHorizontal();

            if (axis == 1)
            {
                nestedLayout.CalculateLayoutInputVertical();
            }
        }

        void ReadConfiguredChildMetrics(
            RectTransform child,
            int axis,
            LoogaLayoutChildSizeMode mode,
            float uniformSize,
            out float minimum,
            out float preferred,
            out float flexible)
        {
            ReadChildMetrics(child, axis, out minimum, out preferred, out flexible);
            float maximum = ChildMaximum(child, axis);

            switch (mode)
            {
                case LoogaLayoutChildSizeMode.Fixed:
                    minimum = Mathf.Clamp(_fixedChildSize[axis], minimum, maximum);
                    preferred = minimum;
                    flexible = 0f;
                    break;

                case LoogaLayoutChildSizeMode.Uniform:
                    minimum = Mathf.Clamp(uniformSize, minimum, maximum);
                    preferred = minimum;
                    flexible = 0f;
                    break;

                case LoogaLayoutChildSizeMode.Fill:
                    // Fill is parent-owned. A previously driven RectTransform size
                    // must not become a minimum or nested layouts can never shrink.
                    minimum = ChildMinimum(child, axis);
                    preferred = minimum;
                    flexible = Mathf.Max(1f, flexible);
                    break;

                default:
                    minimum = Mathf.Min(minimum, maximum);
                    preferred = Mathf.Clamp(preferred, minimum, maximum);
                    flexible = Mathf.Max(0f, flexible);
                    break;
            }
        }

        static float ChildMinimum(RectTransform child, int axis)
        {
            if (!child.TryGetComponent(out LoogaLayoutElement element))
            {
                return 0f;
            }

            float minimum = axis == 0 ? element.minWidth : element.minHeight;
            return Mathf.Max(0f, minimum);
        }

        static float ChildMaximum(RectTransform child, int axis)
        {
            if (!child.TryGetComponent(out LoogaLayoutElement element))
            {
                return float.PositiveInfinity;
            }

            return axis == 0 ? element.MaxWidth : element.MaxHeight;
        }

        void PopulateOrder()
        {
            _orderedIndices.Clear();

            if (_reverseOrder)
            {
                for (int i = rectChildren.Count - 1; i >= 0; i--)
                {
                    _orderedIndices.Add(i);
                }

                return;
            }

            for (int i = 0; i < rectChildren.Count; i++)
            {
                _orderedIndices.Add(i);
            }
        }

        bool ParentControlsRect()
        {
            _componentBuffer.Clear();
            GetComponents(_componentBuffer);
            for (int i = 0; i < _componentBuffer.Count; i++)
            {
                if (_componentBuffer[i] is ILayoutIgnorer ignorer && ignorer.ignoreLayout)
                {
                    return false;
                }
            }

            Transform parent = transform.parent;
            if (parent == null)
            {
                return false;
            }

            _componentBuffer.Clear();
            parent.GetComponents(_componentBuffer);

            for (int i = 0; i < _componentBuffer.Count; i++)
            {
                if (_componentBuffer[i] is ILayoutGroup && _componentBuffer[i] is Behaviour behaviour && behaviour.isActiveAndEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        float ParentSize(int axis)
        {
            return transform.parent is RectTransform parent
                ? Mathf.Max(0f, parent.rect.size[axis])
                : rectTransform.rect.size[axis];
        }

        float ClampedContentSize(int axis)
        {
            float maximum = _maximumSize[axis] > 0f ? _maximumSize[axis] : float.PositiveInfinity;
            return Mathf.Clamp(_contentPreferred[axis], _minimumSize[axis], maximum);
        }

        float AxisPaddingStart(int axis)
        {
            return axis == 0 ? padding.left : padding.top;
        }

        float Alignment(int axis)
        {
            if (axis == 0)
            {
                return childAlignment switch
                {
                    TextAnchor.UpperCenter or TextAnchor.MiddleCenter or TextAnchor.LowerCenter => 0.5f,
                    TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight => 1f,
                    _ => 0f
                };
            }

            return childAlignment switch
            {
                TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight => 0.5f,
                TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => 1f,
                _ => 0f
            };
        }

        static float Sum(List<float> values)
        {
            float total = 0f;
            for (int i = 0; i < values.Count; i++)
            {
                total += values[i];
            }

            return total;
        }
    }
}
