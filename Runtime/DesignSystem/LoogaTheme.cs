using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Overrides semantic token values for one visual theme.</summary>
    [CreateAssetMenu(fileName = "New UI Theme", menuName = "LoogaSoft/UI/Design System/Theme")]
    public sealed class LoogaTheme : LoogaDesignAsset
    {
        [SerializeField] private List<LoogaColorTokenValue> _colors = new List<LoogaColorTokenValue>();
        [SerializeField] private List<LoogaTypographyTokenValue> _typography = new List<LoogaTypographyTokenValue>();

        /// <summary>Gets the theme color overrides.</summary>
        public IReadOnlyList<LoogaColorTokenValue> Colors => _colors;

        /// <summary>Gets the theme typography overrides.</summary>
        public IReadOnlyList<LoogaTypographyTokenValue> Typography => _typography;

        /// <summary>Resolves a color token through this theme.</summary>
        public Color Resolve(LoogaColorToken token)
        {
            if (token == null)
            {
                return Color.white;
            }

            for (int i = 0; i < _colors.Count; i++)
            {
                if (_colors[i].Token == token)
                {
                    return _colors[i].Color;
                }
            }

            return token.DefaultColor;
        }

        /// <summary>Resolves a typography token through this theme.</summary>
        public LoogaTypographySettings Resolve(LoogaTypographyToken token)
        {
            if (token == null)
            {
                return LoogaTypographySettings.Default;
            }

            for (int i = 0; i < _typography.Count; i++)
            {
                if (_typography[i].Token == token)
                {
                    return _typography[i].Settings;
                }
            }

            return token.Defaults;
        }

        /// <summary>Adds or updates a color override.</summary>
        public void SetColor(LoogaColorToken token, Color color)
        {
            if (token == null)
            {
                return;
            }

            for (int i = 0; i < _colors.Count; i++)
            {
                if (_colors[i].Token != token)
                {
                    continue;
                }

                if (_colors[i].Color == color)
                {
                    return;
                }

                _colors[i] = new LoogaColorTokenValue(token, color);
                NotifyChanged();
                return;
            }

            _colors.Add(new LoogaColorTokenValue(token, color));
            NotifyChanged();
        }

        /// <summary>Adds or updates a typography override.</summary>
        public void SetTypography(LoogaTypographyToken token, LoogaTypographySettings settings)
        {
            if (token == null)
            {
                return;
            }

            for (int i = 0; i < _typography.Count; i++)
            {
                if (_typography[i].Token != token)
                {
                    continue;
                }

                _typography[i] = new LoogaTypographyTokenValue(token, settings);
                NotifyChanged();
                return;
            }

            _typography.Add(new LoogaTypographyTokenValue(token, settings));
            NotifyChanged();
        }
    }

    /// <summary>Pairs a semantic color token with one theme value.</summary>
    [System.Serializable]
    public struct LoogaColorTokenValue
    {
        [SerializeField] private LoogaColorToken _token;
        [SerializeField] private Color _color;

        /// <summary>Creates one color override.</summary>
        public LoogaColorTokenValue(LoogaColorToken token, Color color)
        {
            _token = token;
            _color = color;
        }

        /// <summary>Gets the overridden token.</summary>
        public LoogaColorToken Token => _token;

        /// <summary>Gets the theme color.</summary>
        public Color Color => _color;
    }

    /// <summary>Pairs a semantic typography token with one theme value.</summary>
    [System.Serializable]
    public struct LoogaTypographyTokenValue
    {
        [SerializeField] private LoogaTypographyToken _token;
        [SerializeField] private LoogaTypographySettings _settings;

        /// <summary>Creates one typography override.</summary>
        public LoogaTypographyTokenValue(LoogaTypographyToken token, LoogaTypographySettings settings)
        {
            _token = token;
            _settings = settings;
        }

        /// <summary>Gets the overridden token.</summary>
        public LoogaTypographyToken Token => _token;

        /// <summary>Gets the theme typography.</summary>
        public LoogaTypographySettings Settings => _settings;
    }
}
