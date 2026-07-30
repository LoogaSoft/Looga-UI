using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Looga UI/Layout/Looga Content Fitter")]
    public sealed class LoogaContentFitter : UIBehaviour, ILayoutElement, ILayoutSelfController
    {
        [SerializeField, Tooltip("Where the minimum and preferred size are measured from.")]
        LoogaContentSource _contentSource = LoogaContentSource.Self;

        [SerializeField, Tooltip("Content measured when Content Source is Assigned.")]
        RectTransform _assignedContent;

        [SerializeField, Tooltip("How this RectTransform's width responds to measured content.")]
        LoogaContentFitMode _width = LoogaContentFitMode.Preferred;

        [SerializeField, Tooltip("How this RectTransform's height responds to measured content.")]
        LoogaContentFitMode _height = LoogaContentFitMode.Preferred;

        [SerializeField, Min(0f), Tooltip("Smallest size produced by Clamped Preferred.")]
        Vector2 _minimumSize;

        [SerializeField, Tooltip("Largest size produced by Clamped Preferred. Zero means unlimited.")]
        Vector2 _maximumSize;

        [SerializeField, Tooltip("Higher-priority layout values override lower-priority values on the same object.")]
        int _layoutPriority = 1;

        readonly List<Component> _components = new();
        DrivenRectTransformTracker _tracker;
        Vector2 _measuredMinimum;
        Vector2 _measuredPreferred;

        public float minWidth => ReportedSize(0, true);
        public float preferredWidth => ReportedSize(0, false);
        public float flexibleWidth => -1f;
        public float minHeight => ReportedSize(1, true);
        public float preferredHeight => ReportedSize(1, false);
        public float flexibleHeight => -1f;
        public int layoutPriority => _layoutPriority;

        public void CalculateLayoutInputHorizontal()
        {
            _tracker.Clear();
            MeasureAxis(0);
        }

        public void CalculateLayoutInputVertical()
        {
            MeasureAxis(1);
        }

        public void SetLayoutHorizontal()
        {
            FitAxis(0);
        }

        public void SetLayoutVertical()
        {
            FitAxis(1);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            _tracker.Clear();
            SetDirty();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty();
        }

        void OnTransformChildrenChanged()
        {
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _minimumSize = Vector2.Max(Vector2.zero, _minimumSize);

            if (_maximumSize.x > 0f)
            {
                _maximumSize.x = Mathf.Max(_minimumSize.x, _maximumSize.x);
            }

            if (_maximumSize.y > 0f)
            {
                _maximumSize.y = Mathf.Max(_minimumSize.y, _maximumSize.y);
            }

            SetDirty();
        }
#endif

        void MeasureAxis(int axis)
        {
            RectTransform source = ResolveSource();
            float minimum;
            float preferred;

            if (source == null)
            {
                minimum = 0f;
                preferred = 0f;
            }
            else if (source == transform)
            {
                MeasureSelf(axis, out minimum, out preferred);
            }
            else
            {
                minimum = LayoutUtility.GetMinSize(source, axis);
                preferred = LayoutUtility.GetPreferredSize(source, axis);
            }

            _measuredMinimum[axis] = Mathf.Max(0f, minimum);
            _measuredPreferred[axis] = Mathf.Max(_measuredMinimum[axis], preferred);
        }

        void MeasureSelf(int axis, out float minimum, out float preferred)
        {
            minimum = 0f;
            preferred = 0f;
            int minimumPriority = int.MinValue;
            int preferredPriority = int.MinValue;

            _components.Clear();
            GetComponents(_components);

            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is not ILayoutElement element || ReferenceEquals(element, this))
                {
                    continue;
                }

                element.CalculateLayoutInputHorizontal();
                if (axis == 1)
                {
                    element.CalculateLayoutInputVertical();
                }

                int priority = element.layoutPriority;
                float candidateMinimum = axis == 0 ? element.minWidth : element.minHeight;
                float candidatePreferred = axis == 0 ? element.preferredWidth : element.preferredHeight;

                ApplyCandidate(candidateMinimum, priority, ref minimum, ref minimumPriority);
                ApplyCandidate(candidatePreferred, priority, ref preferred, ref preferredPriority);
            }

            preferred = Mathf.Max(minimum, preferred);
        }

        void FitAxis(int axis)
        {
            LoogaContentFitMode mode = axis == 0 ? _width : _height;
            if (mode == LoogaContentFitMode.Authored || ParentControlsRect())
            {
                return;
            }

            float size = mode == LoogaContentFitMode.Minimum
                ? _measuredMinimum[axis]
                : _measuredPreferred[axis];

            if (mode == LoogaContentFitMode.ClampedPreferred)
            {
                float maximum = _maximumSize[axis] > 0f ? _maximumSize[axis] : float.PositiveInfinity;
                size = Mathf.Clamp(size, _minimumSize[axis], maximum);
            }

            DrivenTransformProperties property = axis == 0
                ? DrivenTransformProperties.SizeDeltaX
                : DrivenTransformProperties.SizeDeltaY;

            _tracker.Add(this, transform as RectTransform, property);
            (transform as RectTransform)?.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, size);
        }

        float ReportedSize(int axis, bool minimum)
        {
            LoogaContentFitMode mode = axis == 0 ? _width : _height;
            if (mode == LoogaContentFitMode.Authored)
            {
                return (transform as RectTransform)?.rect.size[axis] ?? 0f;
            }

            float value = minimum ? _measuredMinimum[axis] : _measuredPreferred[axis];
            if (mode == LoogaContentFitMode.Minimum)
            {
                value = _measuredMinimum[axis];
            }
            else if (mode == LoogaContentFitMode.ClampedPreferred)
            {
                float maximum = _maximumSize[axis] > 0f ? _maximumSize[axis] : float.PositiveInfinity;
                value = Mathf.Clamp(value, _minimumSize[axis], maximum);
            }

            return value;
        }

        RectTransform ResolveSource()
        {
            return _contentSource switch
            {
                LoogaContentSource.FirstChild => transform.childCount > 0 ? transform.GetChild(0) as RectTransform : null,
                LoogaContentSource.Assigned => _assignedContent,
                _ => transform as RectTransform
            };
        }

        bool ParentControlsRect()
        {
            _components.Clear();
            GetComponents(_components);
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is ILayoutIgnorer ignorer && ignorer.ignoreLayout)
                {
                    return false;
                }
            }

            Transform parent = transform.parent;
            if (parent == null)
            {
                return false;
            }

            _components.Clear();
            parent.GetComponents(_components);

            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is ILayoutGroup && _components[i] is Behaviour behaviour && behaviour.isActiveAndEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        void SetDirty()
        {
            if (!IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        static void ApplyCandidate(float candidate, int priority, ref float value, ref int currentPriority)
        {
            if (candidate < 0f || priority < currentPriority)
            {
                return;
            }

            if (priority > currentPriority)
            {
                value = candidate;
                currentPriority = priority;
                return;
            }

            value = Mathf.Max(value, candidate);
        }
    }
}
