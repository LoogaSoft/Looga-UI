using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Provides TextMeshPro dropdown behavior with explicit Looga style ownership.</summary>
    [ExecuteAlways]
    [AddComponentMenu("LoogaSoft/UI/Controls/Dropdown")]
    public sealed class LoogaDropdown : TMP_Dropdown, ILoogaStyledControl
    {
        [SerializeField] private LoogaControlStyleSource _styleSource = LoogaControlStyleSource.DesignSystem;
        [SerializeField] private LoogaDropdownStyle _style;
        [SerializeField] private Graphic _arrow;

        private LoogaControlStyleContext _styleContext;

        /// <summary>Gets the source that controls this dropdown's appearance.</summary>
        public LoogaControlStyleSource StyleSource => _styleSource;

        /// <summary>Gets the assigned design-system style.</summary>
        public LoogaDropdownStyle Style => _style;

        Transform ILoogaStyledControl.StyleTransform => transform;
        bool ILoogaStyledControl.IsStyleActive => isActiveAndEnabled;
        LoogaControlStyleSource ILoogaStyledControl.StyleSource => _styleSource;

        private LoogaControlStyleContext StyleContext =>
            _styleContext ??= new LoogaControlStyleContext(this);

        /// <summary>Assigns a style and reapplies the dropdown appearance.</summary>
        public void SetStyle(LoogaDropdownStyle style)
        {
            _style = style;
            ApplyStyle();
        }

        /// <summary>Changes which system controls the dropdown appearance.</summary>
        public void SetStyleSource(LoogaControlStyleSource source)
        {
            _styleSource = source;
            ApplyStyle();
        }

        /// <summary>Resolves missing visual targets from the dropdown hierarchy.</summary>
        public void ResolveTargets()
        {
            if (targetGraphic == null && TryGetComponent(out Graphic background))
            {
                targetGraphic = background;
            }

            if (_arrow != null)
            {
                return;
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic candidate = graphics[i];
                if (candidate == targetGraphic || candidate == captionText || candidate == captionImage ||
                    candidate == itemText || candidate == itemImage ||
                    template != null && candidate.transform.IsChildOf(template))
                {
                    continue;
                }

                _arrow = candidate;
                return;
            }
        }

        /// <summary>Applies the assigned design-system style.</summary>
        public void ApplyStyle()
        {
            StyleContext.RefreshScope();
            ResolveTargets();
            if (_styleSource == LoogaControlStyleSource.DesignSystem && _style != null)
            {
                _style.Apply(this, StyleContext.ActiveTheme, _arrow);
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
