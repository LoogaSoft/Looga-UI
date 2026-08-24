using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines reusable visual settings for one semantic UI role.</summary>
    [CreateAssetMenu(fileName = "New Element Style", menuName = "LoogaSoft/UI/Design System/Element Style")]
    public sealed class LoogaElementStyle : LoogaDesignAsset
    {
        [SerializeField, Tooltip("Keep this style in the public design-system library even when only one object uses it.")]
        private bool _keepPublic;
        [SerializeField] private LoogaGraphicStyleSettings _graphic;
        [SerializeField] private LoogaTextStyleSettings _text;
        [SerializeField] private LoogaSelectableStyleSettings _selectable;

        /// <summary>Gets whether this style must remain in the public library.</summary>
        public bool KeepPublic => _keepPublic;

        /// <summary>Applies this style to the supplied UGUI and TextMeshPro targets.</summary>
        public void Apply(LoogaTheme theme, Graphic graphic, TMP_Text text, Selectable selectable)
        {
            _graphic.Apply(theme, graphic);
            _text.Apply(theme, text);
            _selectable.Apply(theme, selectable);
        }

        /// <summary>Returns true when this style uses the supplied design asset.</summary>
        public bool Uses(UnityEngine.Object source)
        {
            return source == this ||
                   _graphic.Uses(source) ||
                   _text.Uses(source) ||
                   _selectable.Uses(source);
        }

        private static Color Resolve(LoogaTheme theme, LoogaColorToken token)
        {
            if (theme != null)
            {
                return theme.Resolve(token);
            }

            return token != null ? token.DefaultColor : Color.white;
        }

        [System.Serializable]
        private struct LoogaGraphicStyleSettings
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private LoogaColorToken _color;
            [SerializeField] private bool _overrideMaterial;
            [SerializeField] private Material _material;

            public void Apply(LoogaTheme theme, Graphic graphic)
            {
                if (!_enabled || graphic == null)
                {
                    return;
                }

                graphic.color = Resolve(theme, _color);

                if (_overrideMaterial)
                {
                    graphic.material = _material;
                }
            }

            public bool Uses(UnityEngine.Object source)
            {
                return source == _color;
            }
        }

        [System.Serializable]
        private struct LoogaTextStyleSettings
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private LoogaTypographyToken _typography;
            [SerializeField] private LoogaColorToken _color;
            [SerializeField] private bool _overrideAlignment;
            [SerializeField] private TextAlignmentOptions _alignment;

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
                text.color = Resolve(theme, _color);

                if (_overrideAlignment)
                {
                    text.alignment = _alignment;
                }
            }

            public bool Uses(UnityEngine.Object source)
            {
                return source == _typography || source == _color;
            }
        }

        [System.Serializable]
        private struct LoogaSelectableStyleSettings
        {
            [SerializeField] private bool _enabled;
            [SerializeField] private Selectable.Transition _transition;
            [SerializeField] private LoogaColorToken _normal;
            [SerializeField] private LoogaColorToken _highlighted;
            [SerializeField] private LoogaColorToken _pressed;
            [SerializeField] private LoogaColorToken _selected;
            [SerializeField] private LoogaColorToken _disabled;
            [SerializeField, Min(0f)] private float _colorMultiplier;
            [SerializeField, Min(0f)] private float _fadeDuration;

            public void Apply(LoogaTheme theme, Selectable selectable)
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
                colors.normalColor = Resolve(theme, _normal);
                colors.highlightedColor = Resolve(theme, _highlighted);
                colors.pressedColor = Resolve(theme, _pressed);
                colors.selectedColor = Resolve(theme, _selected);
                colors.disabledColor = Resolve(theme, _disabled);
                colors.colorMultiplier = _colorMultiplier > 0f ? _colorMultiplier : 1f;
                colors.fadeDuration = _fadeDuration;
                selectable.colors = colors;
            }

            public bool Uses(UnityEngine.Object source)
            {
                return source == _normal ||
                       source == _highlighted ||
                       source == _pressed ||
                       source == _selected ||
                       source == _disabled;
            }
        }
    }
}
