using System;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Provides common change tracking for one style binding.</summary>
    [ExecuteAlways]
    public abstract class LoogaStyleBindingBase : MonoBehaviour
    {
        /// <summary>Occurs after this binding applies its style.</summary>
        public event Action StyleApplied;

        /// <summary>Applies the assigned style to its target.</summary>
        public void ApplyStyle()
        {
            ApplyStyleInternal();
            StyleApplied?.Invoke();
        }

        /// <summary>Returns true when this binding uses the supplied design asset.</summary>
        protected abstract bool Uses(LoogaDesignAsset asset);

        /// <summary>Applies the assigned style to its target.</summary>
        protected abstract void ApplyStyleInternal();

        protected virtual void OnEnable()
        {
            LoogaDesignSystemEvents.Changed += HandleDesignChanged;
            ApplyStyle();
        }

        protected virtual void OnDisable()
        {
            LoogaDesignSystemEvents.Changed -= HandleDesignChanged;
        }

        protected virtual void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                ApplyStyle();
            }
        }

        private void HandleDesignChanged(LoogaDesignSystemChange change)
        {
            if (change.Source is LoogaDesignAsset asset && Uses(asset))
            {
                ApplyStyle();
            }
        }
    }
}
