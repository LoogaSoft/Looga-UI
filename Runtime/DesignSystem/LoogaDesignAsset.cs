using System;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Provides revision and change notifications for design assets.</summary>
    public abstract class LoogaDesignAsset : ScriptableObject
    {
        [NonSerialized] private int _revision;

        /// <summary>Occurs after this asset changes.</summary>
        public event Action Changed;

        /// <summary>Gets the current runtime revision.</summary>
        public int Revision => _revision;

        /// <summary>Increments the revision and publishes a design-system change.</summary>
        public void NotifyChanged()
        {
            unchecked
            {
                _revision++;
            }

            Changed?.Invoke();
            LoogaDesignSystemEvents.Publish(this, _revision);
        }

        protected virtual void OnValidate()
        {
            NotifyChanged();
        }
    }
}
