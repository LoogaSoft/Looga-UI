using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Provides Unity button behavior with explicit Looga style ownership.</summary>
    [ExecuteAlways]
    [AddComponentMenu("LoogaSoft/UI/Controls/Button")]
    public sealed class LoogaButton : Button, ILoogaStyledControl
    {
        [SerializeField] private LoogaControlStyleSource _styleSource = LoogaControlStyleSource.DesignSystem;
        [SerializeField] private LoogaButtonStyle _style;
        [SerializeField] private bool _resolveTargetsAutomatically = true;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Graphic _icon;

        private LoogaControlStyleContext _styleContext;

        /// <summary>Gets the source that controls this button's appearance.</summary>
        public LoogaControlStyleSource StyleSource => _styleSource;

        /// <summary>Gets the assigned design-system style.</summary>
        public LoogaButtonStyle Style => _style;

        /// <summary>Gets the active theme from the nearest scope.</summary>
        public LoogaTheme ActiveTheme => StyleContext.ActiveTheme;

        Transform ILoogaStyledControl.StyleTransform => transform;

        bool ILoogaStyledControl.IsStyleActive => isActiveAndEnabled;

        LoogaControlStyleSource ILoogaStyledControl.StyleSource => _styleSource;

        private LoogaControlStyleContext StyleContext =>
            _styleContext ??= new LoogaControlStyleContext(this);

        /// <summary>Assigns a design-system style and applies it.</summary>
        public void SetStyle(LoogaButtonStyle style)
        {
            if (_style == style)
            {
                return;
            }

            _style = style;
            ApplyStyle();
        }

        /// <summary>Sets the appearance source and reapplies the current style.</summary>
        public void SetStyleSource(LoogaControlStyleSource source)
        {
            if (_styleSource == source)
            {
                return;
            }

            _styleSource = source;
            ApplyStyle();
        }

        /// <summary>Resolves missing visual targets from the button hierarchy.</summary>
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

            if (_icon != null)
            {
                return;
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == targetGraphic || graphic == _label)
                {
                    continue;
                }

                _icon = graphic;
                return;
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

            if (_styleSource != LoogaControlStyleSource.DesignSystem || _style == null)
            {
                return;
            }

            _style.Apply(ActiveTheme, this, _label, _icon);
        }

        #region Built-in
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
        #endregion

        bool ILoogaStyledControl.UsesDesignAsset(Object source)
        {
            return _style != null && _style.Uses(source);
        }

    }
}
