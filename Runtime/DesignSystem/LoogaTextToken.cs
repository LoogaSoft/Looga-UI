using TMPro;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines reusable TextMeshPro font settings without changing alignment.</summary>
    [CreateAssetMenu(fileName = "New Text Token", menuName = "LoogaSoft/UI/Design System/Text Token")]
    public sealed class LoogaTextToken : LoogaDesignAsset
    {
        [SerializeField] private LoogaTextSettings _defaults = LoogaTextSettings.Default;

        /// <summary>Gets the default TextMeshPro settings.</summary>
        public LoogaTextSettings Defaults => _defaults;

        /// <summary>Sets the default TextMeshPro settings.</summary>
        public void SetDefaults(LoogaTextSettings settings)
        {
            _defaults = settings;
            NotifyChanged();
        }

        /// <summary>Applies this token to a TextMeshPro component.</summary>
        public void Apply(TMP_Text text)
        {
            _defaults.Apply(text);
        }
    }

    /// <summary>Stores reusable TextMeshPro font settings.</summary>
    [System.Serializable]
    public struct LoogaTextSettings
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Material _fontMaterial;
        [SerializeField, Min(0f)] private float _fontSize;
        [SerializeField] private FontStyles _fontStyle;
        [SerializeField] private bool _enableAutoSizing;
        [SerializeField, Min(0f)] private float _minimumFontSize;
        [SerializeField, Min(0f)] private float _maximumFontSize;
        [SerializeField] private float _characterSpacing;
        [SerializeField] private float _wordSpacing;
        [SerializeField] private float _lineSpacing;
        [SerializeField] private float _paragraphSpacing;

        /// <summary>Gets safe default TextMeshPro settings.</summary>
        public static LoogaTextSettings Default => new()
        {
            _fontSize = 18f,
            _fontStyle = FontStyles.Normal,
            _minimumFontSize = 12f,
            _maximumFontSize = 36f
        };

        /// <summary>Applies these values without changing alignment, color, or wrapping.</summary>
        public void Apply(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            if (_font != null)
            {
                text.font = _font;
            }

            if (_fontMaterial != null)
            {
                text.fontSharedMaterial = _fontMaterial;
            }

            text.enableAutoSizing = _enableAutoSizing;
            if (_enableAutoSizing)
            {
                text.fontSizeMin = _minimumFontSize;
                text.fontSizeMax = Mathf.Max(_minimumFontSize, _maximumFontSize);
            }
            else if (_fontSize > 0f)
            {
                text.fontSize = _fontSize;
            }

            text.fontStyle = _fontStyle;
            text.characterSpacing = _characterSpacing;
            text.wordSpacing = _wordSpacing;
            text.lineSpacing = _lineSpacing;
            text.paragraphSpacing = _paragraphSpacing;
        }
    }
}
