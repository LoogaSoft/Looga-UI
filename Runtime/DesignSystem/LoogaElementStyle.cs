using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines reusable visual settings for one semantic UI role.</summary>
    public sealed class LoogaElementStyle : LoogaDesignAsset
    {
        [SerializeField, Tooltip("Keep this style in the public design-system library even when only one object uses it.")]
        private bool _keepPublic;
        [SerializeField] private LoogaGraphicStyleSettings _graphic = LoogaGraphicStyleSettings.Default;
        [SerializeField] private LoogaTextStyleSettings _text = LoogaTextStyleSettings.Default;
        [SerializeField] private LoogaSelectableStyleSettings _selectable = LoogaSelectableStyleSettings.Default;

        /// <summary>Gets whether this style must remain in the public library.</summary>
        public bool KeepPublic => _keepPublic;

        /// <summary>Applies this style to the supplied UGUI and TextMeshPro targets.</summary>
        public void Apply(LoogaTheme theme, Graphic graphic, TMP_Text text, Selectable selectable)
        {
            _graphic.Apply(graphic);
            _text.Apply(theme, text);
            _selectable.Apply(selectable);
        }

        /// <summary>Returns true when this style uses the supplied design asset.</summary>
        public bool Uses(UnityEngine.Object source)
        {
            return source == this ||
                   _text.Uses(source);
        }

        [System.Serializable]
        private struct LoogaGraphicStyleSettings
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private Color _color;
            [SerializeField] private bool _overrideMaterial;
            [SerializeField] private Material _material;

            public static LoogaGraphicStyleSettings Default => new()
            {
                _color = Color.white
            };

            public void Apply(Graphic graphic)
            {
                if (!_enabled || graphic == null)
                {
                    return;
                }

                graphic.color = _color;

                if (_overrideMaterial)
                {
                    graphic.material = _material;
                }
            }

        }

        [System.Serializable]
        private struct LoogaTextStyleSettings
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private LoogaTypographyToken _typography;
            [SerializeField] private Color _color;
            [SerializeField] private bool _overrideAlignment;
            [SerializeField] private TextAlignmentOptions _alignment;

            public static LoogaTextStyleSettings Default => new()
            {
                _color = Color.white
            };

            public void Apply(LoogaTheme theme, TMP_Text text)
            {
                if (!_enabled || text == null)
                {
                    return;
                }

                LoogaTypographySettings settings = theme != null
                    ? theme.Resolve(_typography)
                    : _typography != null
                        ? _typography.Defaults
                        : LoogaTypographySettings.Default;

                settings.Apply(text);
                text.color = _color;

                if (_overrideAlignment)
                {
                    text.alignment = _alignment;
                }
            }

            public bool Uses(UnityEngine.Object source)
            {
                return source == _typography;
            }
        }

        [System.Serializable]
        private struct LoogaSelectableStyleSettings
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private Selectable.Transition _transition;
            [SerializeField] private Color _normal;
            [SerializeField] private Color _highlighted;
            [SerializeField] private Color _pressed;
            [SerializeField] private Color _selected;
            [SerializeField] private Color _disabled;
            [SerializeField, Min(0f)] private float _colorMultiplier;
            [SerializeField, Min(0f)] private float _fadeDuration;

            public static LoogaSelectableStyleSettings Default => new()
            {
                _normal = Color.white,
                _highlighted = new Color(0.882f, 0.882f, 0.882f, 1f),
                _pressed = new Color(0.698f, 0.698f, 0.698f, 1f),
                _selected = new Color(0.882f, 0.882f, 0.882f, 1f),
                _disabled = new Color(0.521f, 0.521f, 0.521f, 0.502f),
                _colorMultiplier = 1f,
                _fadeDuration = 0.1f
            };

            public void Apply(Selectable selectable)
            {
                if (!_enabled || selectable == null)
                {
                    return;
                }

                selectable.transition = _transition;

                if (_transition != Selectable.Transition.ColorTint)
                {
                    return;
                }

                ColorBlock colors = selectable.colors;
                colors.normalColor = _normal;
                colors.highlightedColor = _highlighted;
                colors.pressedColor = _pressed;
                colors.selectedColor = _selected;
                colors.disabledColor = _disabled;
                colors.colorMultiplier = _colorMultiplier > 0f ? _colorMultiplier : 1f;
                colors.fadeDuration = _fadeDuration;
                selectable.colors = colors;
            }

        }
    }
}
