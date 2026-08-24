using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Provides TextMeshPro input behavior with explicit Looga style ownership.</summary>
    [ExecuteAlways]
    [AddComponentMenu("LoogaSoft/UI/Controls/Input Field")]
    public sealed class LoogaInputField : TMP_InputField, ILoogaStyledControl
    {
        [SerializeField] private LoogaControlStyleSource _styleSource = LoogaControlStyleSource.DesignSystem;
        [SerializeField] private LoogaInputFieldStyle _style;
        [SerializeField] private TMP_Text _placeholderText;

        private LoogaControlStyleContext _styleContext;

        /// <summary>Gets the source that controls this input field's appearance.</summary>
        public LoogaControlStyleSource StyleSource => _styleSource;

        /// <summary>Gets the assigned design-system style.</summary>
        public LoogaInputFieldStyle Style => _style;

        Transform ILoogaStyledControl.StyleTransform => transform;
        bool ILoogaStyledControl.IsStyleActive => isActiveAndEnabled;
        LoogaControlStyleSource ILoogaStyledControl.StyleSource => _styleSource;

        private LoogaControlStyleContext StyleContext =>
            _styleContext ??= new LoogaControlStyleContext(this);

        /// <summary>Assigns a style and reapplies the input-field appearance.</summary>
        public void SetStyle(LoogaInputFieldStyle style)
        {
            _style = style;
            ApplyStyle();
        }

        /// <summary>Changes which system controls the input-field appearance.</summary>
        public void SetStyleSource(LoogaControlStyleSource source)
        {
            _styleSource = source;
            ApplyStyle();
        }

        /// <summary>Resolves missing visual targets from the input-field hierarchy.</summary>
        public void ResolveTargets()
        {
            if (targetGraphic == null && TryGetComponent(out Graphic background))
            {
                targetGraphic = background;
            }

            _placeholderText = placeholder as TMP_Text;
        }

        /// <summary>Applies the assigned design-system style.</summary>
        public void ApplyStyle()
        {
            StyleContext.RefreshScope();
            ResolveTargets();
            if (_styleSource == LoogaControlStyleSource.DesignSystem && _style != null)
            {
                _style.Apply(StyleContext.ActiveTheme, this, textComponent, _placeholderText);
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
