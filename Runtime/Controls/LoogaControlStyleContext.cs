using LoogaSoft.UI.Design;
using UnityEngine;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Defines which system controls a Looga control's appearance.</summary>
    public enum LoogaControlStyleSource
    {
        /// <summary>The assigned Looga style controls appearance.</summary>
        DesignSystem,

        /// <summary>The serialized Unity control settings control appearance.</summary>
        Local
    }

    /// <summary>Provides common style operations for Looga controls.</summary>
    public interface ILoogaStyledControl
    {
        /// <summary>Gets the transform used to resolve the nearest theme scope.</summary>
        Transform StyleTransform { get; }

        /// <summary>Gets whether the control can receive theme notifications.</summary>
        bool IsStyleActive { get; }

        /// <summary>Gets the active appearance source.</summary>
        LoogaControlStyleSource StyleSource { get; }

        /// <summary>Returns true when this control uses the supplied design asset.</summary>
        bool UsesDesignAsset(Object source);

        /// <summary>Applies the control's current style.</summary>
        void ApplyStyle();
    }

    internal sealed class LoogaControlStyleContext
    {
        private readonly ILoogaStyledControl _owner;
        private LoogaThemeScope _scope;

        public LoogaControlStyleContext(ILoogaStyledControl owner)
        {
            _owner = owner;
        }

        public LoogaTheme ActiveTheme => _scope != null ? _scope.ActiveTheme : null;

        public void Enable()
        {
            LoogaDesignSystemEvents.Changed += HandleDesignChanged;
            RefreshScope();
        }

        public void Disable()
        {
            LoogaDesignSystemEvents.Changed -= HandleDesignChanged;
            UnsubscribeFromScope();
            _scope = null;
        }

        public void RefreshScope()
        {
            LoogaThemeScope resolvedScope = LoogaThemeScope.FindNearest(_owner.StyleTransform);
            if (_scope == resolvedScope)
            {
                return;
            }

            UnsubscribeFromScope();
            _scope = resolvedScope;
            SubscribeToScope();
        }

        private void SubscribeToScope()
        {
            if (_owner.IsStyleActive && _scope != null)
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
            if (_owner.StyleSource == LoogaControlStyleSource.DesignSystem &&
                _owner.UsesDesignAsset(change.Source))
            {
                _owner.ApplyStyle();
            }
        }

        private void HandleThemeChanged(LoogaTheme theme)
        {
            _owner.ApplyStyle();
        }
    }
}
