using TMPro;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines one semantic typography role and its fallback values.</summary>
    [CreateAssetMenu(fileName = "New Typography Token", menuName = "LoogaSoft/UI/Design System/Typography Token")]
    public sealed class LoogaTypographyToken : LoogaDesignAsset
    {
        [SerializeField] private LoogaTypographySettings _defaults = LoogaTypographySettings.Default;

        /// <summary>Gets the typography used when the active theme has no override.</summary>
        public LoogaTypographySettings Defaults => _defaults;

        /// <summary>Sets the fallback typography.</summary>
        public void SetDefaults(LoogaTypographySettings settings)
        {
            _defaults = settings;
            NotifyChanged();
        }
    }

    /// <summary>Stores the TextMeshPro values for one typography role.</summary>
    [System.Serializable]
    public struct LoogaTypographySettings
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField, Min(0f)] private float _fontSize;
        [SerializeField] private FontStyles _fontStyle;
        [SerializeField] private float _characterSpacing;
        [SerializeField] private float _wordSpacing;
        [SerializeField] private float _lineSpacing;
        [SerializeField] private float _paragraphSpacing;

        /// <summary>Gets safe fallback typography.</summary>
        public static LoogaTypographySettings Default => new LoogaTypographySettings
        {
            _fontSize = 18f,
            _fontStyle = FontStyles.Normal
        };

        /// <summary>Gets the assigned TextMeshPro font.</summary>
        public TMP_FontAsset Font => _font;

        /// <summary>Gets the font size.</summary>
        public float FontSize => _fontSize;

        /// <summary>Gets the font style.</summary>
        public FontStyles FontStyle => _fontStyle;

        /// <summary>Gets the character spacing.</summary>
        public float CharacterSpacing => _characterSpacing;

        /// <summary>Gets the word spacing.</summary>
        public float WordSpacing => _wordSpacing;

        /// <summary>Gets the line spacing.</summary>
        public float LineSpacing => _lineSpacing;

        /// <summary>Gets the paragraph spacing.</summary>
        public float ParagraphSpacing => _paragraphSpacing;

        /// <summary>Applies these values to a TextMeshPro component.</summary>
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

            if (_fontSize > 0f)
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
