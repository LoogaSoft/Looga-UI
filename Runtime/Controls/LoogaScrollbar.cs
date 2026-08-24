using LoogaSoft.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Provides Unity scrollbar behavior with explicit Looga style ownership.</summary>
    [ExecuteAlways]
    [AddComponentMenu("LoogaSoft/UI/Controls/Scrollbar")]
    public sealed class LoogaScrollbar : Scrollbar, ILoogaStyledControl
    {
        [SerializeField] private LoogaControlStyleSource _styleSource = LoogaControlStyleSource.DesignSystem;
        [SerializeField] private LoogaScrollbarStyle _style;
        [SerializeField] private Graphic _handleGraphic;

        private LoogaControlStyleContext _styleContext;

        /// <summary>Gets the source that controls this scrollbar's appearance.</summary>
        public LoogaControlStyleSource StyleSource => _styleSource;

        /// <summary>Gets the assigned design-system style.</summary>
        public LoogaScrollbarStyle Style => _style;

        Transform ILoogaStyledControl.StyleTransform => transform;
        bool ILoogaStyledControl.IsStyleActive => isActiveAndEnabled;
        LoogaControlStyleSource ILoogaStyledControl.StyleSource => _styleSource;

        private LoogaControlStyleContext StyleContext =>
            _styleContext ??= new LoogaControlStyleContext(this);

        /// <summary>Assigns a style and reapplies the scrollbar appearance.</summary>
        public void SetStyle(LoogaScrollbarStyle style)
        {
            _style = style;
            ApplyStyle();
        }

        /// <summary>Changes which system controls the scrollbar appearance.</summary>
        public void SetStyleSource(LoogaControlStyleSource source)
        {
            _styleSource = source;
            ApplyStyle();
        }

        /// <summary>Resolves missing visual targets from the scrollbar hierarchy.</summary>
        public void ResolveTargets()
        {
            if (targetGraphic == null && TryGetComponent(out Graphic background))
            {
                targetGraphic = background;
            }

            _handleGraphic ??= handleRect != null ? handleRect.GetComponent<Graphic>() : null;
        }

        /// <summary>Applies the assigned design-system style.</summary>
        public void ApplyStyle()
        {
            StyleContext.RefreshScope();
            ResolveTargets();
            if (_styleSource == LoogaControlStyleSource.DesignSystem && _style != null)
            {
                _style.Apply(StyleContext.ActiveTheme, this, _handleGraphic);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StyleContext.Enable();
            ApplyStyle();
        }

        protected override void OnDisable()
        {
            StyleContext.Disable();
            base.OnDisable();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            ApplyStyle();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (isActiveAndEnabled)
            {
                ApplyStyle();
            }
        }

        bool ILoogaStyledControl.UsesDesignAsset(Object source)
        {
            return _style != null && _style.Uses(source);
        }
    }
}
