using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Lists the shared UI slots available to every menu screen and layout.</summary>
    [CreateAssetMenu(fileName = "New Menu Project", menuName = "LoogaSoft/UI/Menus/Menu Project")]
    public sealed class LoogaMenuStructureProfile : ScriptableObject
    {
        [SerializeField] private LoogaMenuRegionDefinition[] _regions = Array.Empty<LoogaMenuRegionDefinition>();

        public IReadOnlyList<LoogaMenuRegionDefinition> Regions => _regions;

        public bool Contains(LoogaMenuRegionDefinition region)
        {
            if (region == null)
                return false;

            foreach (LoogaMenuRegionDefinition candidate in _regions ?? Array.Empty<LoogaMenuRegionDefinition>())
            {
                if (candidate == region)
                    return true;
            }

            return false;
        }
    }
}
