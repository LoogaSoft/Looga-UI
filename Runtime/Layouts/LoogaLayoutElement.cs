using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions
{
    /// <summary>Overrides the size rules that a parent layout uses for one child.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Looga UI/Layout/Looga Layout Element")]
    public sealed class LoogaLayoutElement : UIBehaviour, ILayoutElement, ILayoutIgnorer
    {
        [SerializeField, Tooltip("Excludes this object from its parent layout.")]
        private bool _ignoreLayout;

        [SerializeField, Tooltip("Overrides the minimum width reported to the parent layout.")]
        private bool _overrideMinWidth;

        [SerializeField, Min(0f)]
        private float _minWidth;

        [SerializeField, Tooltip("Overrides the preferred width reported to the parent layout.")]
        private bool _overridePreferredWidth;

        [SerializeField, Min(0f)]
        private float _preferredWidth = 100f;

        [SerializeField, Tooltip("Caps the width assigned by Looga Layout. Unity layout groups do not consume maximum values.")]
        private bool _useMaxWidth;

        [SerializeField, Min(0f)]
        private float _maxWidth = 1000f;

        [SerializeField, Tooltip("Overrides how strongly this element grows into extra horizontal space.")]
        private bool _overrideFlexibleWidth;

        [SerializeField, Min(0f)]
        private float _flexibleWidth;

        [SerializeField, Tooltip("Overrides the minimum height reported to the parent layout.")]
        private bool _overrideMinHeight;

        [SerializeField, Min(0f)]
        private float _minHeight;

        [SerializeField, Tooltip("Overrides the preferred height reported to the parent layout.")]
        private bool _overridePreferredHeight;

        [SerializeField, Min(0f)]
        private float _preferredHeight = 100f;

        [SerializeField, Tooltip("Caps the height assigned by Looga Layout. Unity layout groups do not consume maximum values.")]
        private bool _useMaxHeight;

        [SerializeField, Min(0f)]
        private float _maxHeight = 1000f;

        [SerializeField, Tooltip("Overrides how strongly this element grows into extra vertical space.")]
        private bool _overrideFlexibleHeight;

        [SerializeField, Min(0f)]
        private float _flexibleHeight;

        [SerializeField, Tooltip("Higher-priority layout values override lower-priority values on the same object.")]
        private int _layoutPriority = 1;

        public bool ignoreLayout => _ignoreLayout;
        public float minWidth => _overrideMinWidth ? _minWidth : -1f;
        public float preferredWidth => _overridePreferredWidth ? Mathf.Max(_preferredWidth, minWidth) : -1f;
        public float flexibleWidth => _overrideFlexibleWidth ? _flexibleWidth : -1f;
        public float minHeight => _overrideMinHeight ? _minHeight : -1f;
        public float preferredHeight => _overridePreferredHeight ? Mathf.Max(_preferredHeight, minHeight) : -1f;
        public float flexibleHeight => _overrideFlexibleHeight ? _flexibleHeight : -1f;
        public int layoutPriority => _layoutPriority;
        /// <summary>Gets the optional maximum width consumed by Looga Layout.</summary>
        public float MaxWidth => _useMaxWidth ? Mathf.Max(_maxWidth, 0f) : float.PositiveInfinity;

        /// <summary>Gets the optional maximum height consumed by Looga Layout.</summary>
        public float MaxHeight => _useMaxHeight ? Mathf.Max(_maxHeight, 0f) : float.PositiveInfinity;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            _minWidth = Mathf.Max(0f, _minWidth);
            _preferredWidth = Mathf.Max(0f, _preferredWidth);
            float requiredWidth = _overridePreferredWidth
                ? Mathf.Max(_overrideMinWidth ? _minWidth : 0f, _preferredWidth)
                : (_overrideMinWidth ? _minWidth : 0f);
            _maxWidth = Mathf.Max(requiredWidth, _maxWidth);
            _flexibleWidth = Mathf.Max(0f, _flexibleWidth);
            _minHeight = Mathf.Max(0f, _minHeight);
            _preferredHeight = Mathf.Max(0f, _preferredHeight);
            float requiredHeight = _overridePreferredHeight
                ? Mathf.Max(_overrideMinHeight ? _minHeight : 0f, _preferredHeight)
                : (_overrideMinHeight ? _minHeight : 0f);
            _maxHeight = Mathf.Max(requiredHeight, _maxHeight);
            _flexibleHeight = Mathf.Max(0f, _flexibleHeight);

            SetDirty();
        }
#endif

        private void SetDirty()
        {
            if (!IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }
    }
}
