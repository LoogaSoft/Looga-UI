using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Provides Unity toggle behavior with explicit Looga style ownership.</summary>
    [ExecuteAlways]
    [AddComponentMenu("LoogaSoft/UI/Controls/Toggle")]
    public sealed class LoogaToggle : Toggle, ILoogaStyledControl
    {
        [SerializeField] private LoogaControlStyleSource _styleSource = LoogaControlStyleSource.DesignSystem;
        [SerializeField] private LoogaToggleStyle _style;
        [SerializeField] private bool _resolveTargetsAutomatically = true;
        [SerializeField] private TMP_Text _label;

        private LoogaControlStyleContext _styleContext;

        /// <summary>Gets the source that controls this toggle's appearance.</summary>
        public LoogaControlStyleSource StyleSource => _styleSource;

        /// <summary>Gets the assigned design-system style.</summary>
        public LoogaToggleStyle Style => _style;

        Transform ILoogaStyledControl.StyleTransform => transform;
        bool ILoogaStyledControl.IsStyleActive => isActiveAndEnabled;
        LoogaControlStyleSource ILoogaStyledControl.StyleSource => _styleSource;

        private LoogaControlStyleContext StyleContext =>
            _styleContext ??= new LoogaControlStyleContext(this);

        /// <summary>Assigns a style and reapplies the toggle appearance.</summary>
        public void SetStyle(LoogaToggleStyle style)
        {
            _style = style;
            ApplyStyle();
        }

        /// <summary>Changes which system controls the toggle appearance.</summary>
        public void SetStyleSource(LoogaControlStyleSource source)
        {
            _styleSource = source;
            ApplyStyle();
        }

        /// <summary>Resolves missing visual targets from the toggle hierarchy.</summary>
        public void ResolveTargets()
        {
            if (targetGraphic == null && TryGetComponent(out Graphic background))
            {
                targetGraphic = background;
            }

            if (_label == null)
            {
                _label = GetComponentInChildren<TMP_Text>(true);
            }
        }

        /// <summary>Applies the assigned design-system style.</summary>
        public void ApplyStyle()
        {
            StyleContext.RefreshScope();
            if (_resolveTargetsAutomatically)
            {
                ResolveTargets();
            }

            if (_styleSource == LoogaControlStyleSource.DesignSystem && _style != null)
            {
                _style.Apply(StyleContext.ActiveTheme, this, graphic, _label);
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
