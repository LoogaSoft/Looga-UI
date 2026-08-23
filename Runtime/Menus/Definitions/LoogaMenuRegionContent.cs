using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Provides the data and panels displayed by one menu region.</summary>
    public abstract class LoogaMenuRegionContent : ScriptableObject
    {
        /// <summary>Gets whether this content can append compatible content from a later authoring layer.</summary>
        public virtual bool SupportsAdd => false;

        /// <summary>Adds every panel required by this content.</summary>
        public abstract void CollectPanels(List<LoogaMenuPanelDefinition> panels);

        internal LoogaMenuRegionContent CreateRuntimeCopy()
        {
            LoogaMenuRegionContent copy = CreateInstance(GetType()) as LoogaMenuRegionContent;
            copy.name = name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            CopyTo(copy);
            return copy;
        }

        internal virtual bool AddFrom(LoogaMenuRegionContent addition)
        {
            return false;
        }

        protected abstract void CopyTo(LoogaMenuRegionContent copy);
    }
}
