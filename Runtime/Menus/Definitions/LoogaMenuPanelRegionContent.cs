using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Displays one or more authored menu panels in a region.</summary>
    public sealed class LoogaMenuPanelRegionContent : LoogaMenuRegionContent
    {
        [SerializeField] private LoogaMenuPanelDefinition[] _panels = Array.Empty<LoogaMenuPanelDefinition>();

        public override bool SupportsAdd => true;
        public IReadOnlyList<LoogaMenuPanelDefinition> Panels => _panels;

        public override void CollectPanels(List<LoogaMenuPanelDefinition> panels)
        {
            if (panels == null)
                return;

            foreach (LoogaMenuPanelDefinition panel in _panels ?? Array.Empty<LoogaMenuPanelDefinition>())
            {
                if (panel != null && !panels.Contains(panel))
                    panels.Add(panel);
            }
        }

        internal override bool AddFrom(LoogaMenuRegionContent addition)
        {
            if (addition is not LoogaMenuPanelRegionContent panelAddition)
                return false;

            List<LoogaMenuPanelDefinition> combined = new(_panels ?? Array.Empty<LoogaMenuPanelDefinition>());
            foreach (LoogaMenuPanelDefinition panel in panelAddition._panels
                ?? Array.Empty<LoogaMenuPanelDefinition>())
            {
                if (panel != null && !combined.Contains(panel))
                    combined.Add(panel);
            }

            _panels = combined.ToArray();
            return true;
        }

        protected override void CopyTo(LoogaMenuRegionContent copy)
        {
            ((LoogaMenuPanelRegionContent)copy)._panels =
                (LoogaMenuPanelDefinition[])(_panels?.Clone()
                    ?? Array.Empty<LoogaMenuPanelDefinition>());
        }
    }
}
