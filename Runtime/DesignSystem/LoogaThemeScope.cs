using System;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Resolves a design system and theme for one nested UI hierarchy.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/UI/Design System/Theme Scope")]
    public sealed class LoogaThemeScope : MonoBehaviour
    {
        [SerializeField] private LoogaDesignSystem _designSystem;
        [SerializeField] private LoogaTheme _themeOverride;

        private LoogaThemeScope _parentScope;

        /// <summary>Occurs after this scope resolves a different theme.</summary>
        public event Action<LoogaTheme> ThemeChanged;

        /// <summary>Gets the design system resolved by this scope.</summary>
        public LoogaDesignSystem DesignSystem => _designSystem != null
            ? _designSystem
            : _parentScope != null
                ? _parentScope.DesignSystem
                : null;

        /// <summary>Gets the active theme resolved by this scope.</summary>
        public LoogaTheme ActiveTheme => _themeOverride != null
            ? _themeOverride
            : _designSystem != null
                ? _designSystem.DefaultTheme
                : _parentScope != null
                    ? _parentScope.ActiveTheme
                    : null;

        /// <summary>Sets the local theme override.</summary>
        public void SetThemeOverride(LoogaTheme theme)
        {
            if (_themeOverride == theme)
            {
                return;
            }

            _themeOverride = theme;
            NotifyThemeChanged();
        }

        private void OnEnable()
        {
            ResolveParentScope();
            LoogaDesignSystemEvents.Changed += HandleDesignChanged;
            SubscribeToParent();
            NotifyThemeChanged();
        }

        private void OnDisable()
        {
            LoogaDesignSystemEvents.Changed -= HandleDesignChanged;
            UnsubscribeFromParent();
        }

        private void OnTransformParentChanged()
        {
            UnsubscribeFromParent();
            ResolveParentScope();
            SubscribeToParent();
            NotifyThemeChanged();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            NotifyThemeChanged();
        }

        private void ResolveParentScope()
        {
            _parentScope = null;
            Transform current = transform.parent;

            while (current != null)
            {
                if (current.TryGetComponent(out LoogaThemeScope scope))
                {
                    _parentScope = scope;
                    return;
                }

                current = current.parent;
            }
        }

        private void SubscribeToParent()
        {
            if (_parentScope != null)
            {
                _parentScope.ThemeChanged += HandleParentThemeChanged;
            }
        }

        private void UnsubscribeFromParent()
        {
            if (_parentScope != null)
            {
                _parentScope.ThemeChanged -= HandleParentThemeChanged;
            }
        }

        private void HandleDesignChanged(LoogaDesignSystemChange change)
        {
            if (change.Source == DesignSystem || change.Source == ActiveTheme)
            {
                NotifyThemeChanged();
            }
        }

        private void HandleParentThemeChanged(LoogaTheme theme)
        {
            if (_designSystem == null && _themeOverride == null)
            {
                ThemeChanged?.Invoke(theme);
            }
        }

        private void NotifyThemeChanged()
        {
            ThemeChanged?.Invoke(ActiveTheme);
        }
    }
}
