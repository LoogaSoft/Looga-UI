using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Defines the visual roles for one reusable input-field style.</summary>
    [CreateAssetMenu(fileName = "New Input Field Style", menuName = "LoogaSoft/UI/Design System/Input Field Style")]
    public sealed class LoogaInputFieldStyle : LoogaDesignAsset
    {
        [SerializeField] private LoogaElementStyle _inputField;
        [SerializeField] private LoogaElementStyle _text;
        [SerializeField] private LoogaElementStyle _placeholder;

        /// <summary>Applies this style to one input field and its visual targets.</summary>
        public void Apply(LoogaTheme theme, TMP_InputField inputField, TMP_Text text, TMP_Text placeholder)
        {
            _inputField?.Apply(theme, inputField != null ? inputField.targetGraphic : null, null, inputField);
            _text?.Apply(theme, text, text, null);
            _placeholder?.Apply(theme, placeholder, placeholder, null);
        }

        /// <summary>Returns true when this style uses the supplied design asset.</summary>
        public bool Uses(Object source)
        {
            return source == this || Uses(_inputField, source) || Uses(_text, source) || Uses(_placeholder, source);
        }

        private static bool Uses(LoogaElementStyle style, Object source)
        {
            return style != null && (source == style || style.Uses(source));
        }
    }
}
