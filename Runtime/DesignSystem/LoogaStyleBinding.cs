using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Applies one semantic style to UGUI components on this GameObject.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/UI/Design System/Style Binding")]
    public sealed class LoogaStyleBinding : MonoBehaviour
    {
        [SerializeField] private LoogaElementStyle _style;
        [SerializeField] private bool _resolveTargetsAutomatically = true;
        [SerializeField] private Graphic _graphic;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Selectable _selectable;

        private LoogaThemeScope _scope;

        /// <summary>Occurs after this binding applies its style.</summary>
        public event Action StyleApplied;

        /// <summary>Gets the assigned semantic style.</summary>
        public LoogaElementStyle Style => _style;

        /// <summary>Gets the theme resolved from the nearest scope.</summary>
        public LoogaTheme ActiveTheme => _scope != null ? _scope.ActiveTheme : null;

        /// <summary>Assigns and applies a semantic style.</summary>
        public void SetStyle(LoogaElementStyle style)
        {
            if (_style == style)
            {
                return;
            }

            _style = style;
            ApplyStyle();
        }

        /// <summary>Applies the assigned style to this GameObject.</summary>
        public void ApplyStyle()
        {
            RefreshScope();

            if (_resolveTargetsAutomatically)
            {
                ResolveTargets();
            }

            if (_style == null)
            {
                return;
            }

            _style.Apply(ActiveTheme, _graphic, _text, _selectable);
            StyleApplied?.Invoke();
        }

        private void OnEnable()
        {
            LoogaDesignSystemEvents.Changed += HandleDesignChanged;
            ApplyStyle();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnTransformParentChanged()
        {
            ApplyStyle();
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                ApplyStyle();
            }
        }

        private void ResolveTargets()
        {
            if (_graphic == null)
            {
                TryGetComponent(out _graphic);
            }

            if (_text == null)
            {
                TryGetComponent(out _text);
            }

            if (_selectable == null)
            {
                TryGetComponent(out _selectable);
            }
        }

        private void RefreshScope()
        {
            LoogaThemeScope resolvedScope = null;
            Transform current = transform;

            while (current != null)
            {
                if (current.TryGetComponent(out LoogaThemeScope scope))
                {
                    resolvedScope = scope;
                    break;
                }

                current = current.parent;
            }

            if (_scope == resolvedScope)
            {
                return;
            }

            UnsubscribeFromScope();
            _scope = resolvedScope;

            if (isActiveAndEnabled)
            {
                SubscribeToScope();
            }
        }

        private void Unsubscribe()
        {
            LoogaDesignSystemEvents.Changed -= HandleDesignChanged;
            UnsubscribeFromScope();
        }

        private void SubscribeToScope()
        {
            if (_scope != null)
            {
                _scope.ThemeChanged += HandleThemeChanged;
            }
        }

        private void UnsubscribeFromScope()
        {
            if (_scope != null)
            {
                _scope.ThemeChanged -= HandleThemeChanged;
            }
        }

        private void HandleDesignChanged(LoogaDesignSystemChange change)
        {
            if (_style != null && _style.Uses(change.Source))
            {
                ApplyStyle();
            }
        }

        private void HandleThemeChanged(LoogaTheme theme)
        {
            ApplyStyle();
        }
    }
}
