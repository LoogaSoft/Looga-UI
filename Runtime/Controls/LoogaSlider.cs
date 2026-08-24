using LoogaSoft.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Provides Unity slider behavior with explicit Looga style ownership.</summary>
    [ExecuteAlways]
    [AddComponentMenu("LoogaSoft/UI/Controls/Slider")]
    public sealed class LoogaSlider : Slider, ILoogaStyledControl
    {
        [SerializeField] private LoogaControlStyleSource _styleSource = LoogaControlStyleSource.DesignSystem;
        [SerializeField] private LoogaSliderStyle _style;
        [SerializeField] private Graphic _fillGraphic;
        [SerializeField] private Graphic _handleGraphic;

        private LoogaControlStyleContext _styleContext;

        /// <summary>Gets the source that controls this slider's appearance.</summary>
        public LoogaControlStyleSource StyleSource => _styleSource;

        /// <summary>Gets the assigned design-system style.</summary>
        public LoogaSliderStyle Style => _style;

        Transform ILoogaStyledControl.StyleTransform => transform;
        bool ILoogaStyledControl.IsStyleActive => isActiveAndEnabled;
        LoogaControlStyleSource ILoogaStyledControl.StyleSource => _styleSource;

        private LoogaControlStyleContext StyleContext =>
            _styleContext ??= new LoogaControlStyleContext(this);

        /// <summary>Assigns a style and reapplies the slider appearance.</summary>
        public void SetStyle(LoogaSliderStyle style)
        {
            _style = style;
            ApplyStyle();
        }

        /// <summary>Changes which system controls the slider appearance.</summary>
        public void SetStyleSource(LoogaControlStyleSource source)
        {
            _styleSource = source;
            ApplyStyle();
        }

        /// <summary>Resolves missing visual targets from the slider hierarchy.</summary>
        public void ResolveTargets()
        {
            if (targetGraphic == null && TryGetComponent(out Graphic background))
            {
                targetGraphic = background;
            }

            _fillGraphic ??= fillRect != null ? fillRect.GetComponent<Graphic>() : null;
            _handleGraphic ??= handleRect != null ? handleRect.GetComponent<Graphic>() : null;
        }

        /// <summary>Applies the assigned design-system style.</summary>
        public void ApplyStyle()
        {
            StyleContext.RefreshScope();
            ResolveTargets();
            if (_styleSource == LoogaControlStyleSource.DesignSystem && _style != null)
            {
                _style.Apply(StyleContext.ActiveTheme, this, _fillGraphic, _handleGraphic);
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
