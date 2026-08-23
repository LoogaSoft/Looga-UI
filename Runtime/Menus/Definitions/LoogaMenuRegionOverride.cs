using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuRegionMode
    {
        Inherit = 0,
        Override = 1,
        Hide = 2,
        Add = 3
    }

    /// <summary>Changes one configured region for a screen or layout.</summary>
    [Serializable]
    public sealed class LoogaMenuRegionOverride
    {
        [SerializeField] private LoogaMenuRegionDefinition _region;
        [SerializeField] private LoogaMenuRegionMode _mode = LoogaMenuRegionMode.Inherit;
        [SerializeField] private LoogaMenuRegionContent _content;

        public LoogaMenuRegionDefinition Region => _region;
        public LoogaMenuRegionMode Mode => _mode;
        public LoogaMenuRegionContent Content => _content;
    }
}
