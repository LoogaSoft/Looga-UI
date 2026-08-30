using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Overrides legacy typography token values for one visual theme.</summary>
    public sealed class LoogaTheme : LoogaDesignAsset
    {
        [SerializeField] private List<LoogaTypographyTokenValue> _typography = new List<LoogaTypographyTokenValue>();

        /// <summary>Gets the theme typography overrides.</summary>
        public IReadOnlyList<LoogaTypographyTokenValue> Typography => _typography;

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
