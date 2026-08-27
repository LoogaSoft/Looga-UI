using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Applies persistent menu-region content for one gameplay or application context.</summary>
    [CreateAssetMenu(fileName = "New Menu Context", menuName = "LoogaSoft/UI/Menus/Context")]
    public sealed class LoogaMenuContextDefinition : ScriptableObject
    {
        [SerializeField, TextArea] private string _description;
        [SerializeField] private LoogaMenuRegionOverride[] _regionOverrides =
            Array.Empty<LoogaMenuRegionOverride>();

        public string Description => _description;
        public LoogaMenuRegionOverride[] RegionOverrides => _regionOverrides;

        private void OnValidate()
        {
            _regionOverrides ??= Array.Empty<LoogaMenuRegionOverride>();
        }
    }
}
