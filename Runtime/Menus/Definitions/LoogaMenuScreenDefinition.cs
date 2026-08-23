using System;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Defines one menu destination. A screen owns its layouts, navigation entries, and menu behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "New Menu Screen", menuName = "LoogaSoft/UI/Menus/Screen Definition")]
    public sealed class LoogaMenuScreenDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, TextArea] private string _description;

        [Header("Layouts")]
        [FormerlySerializedAs("_configurations")]
        [SerializeField] private LoogaMenuScreenLayout[] _layouts = Array.Empty<LoogaMenuScreenLayout>();
        [FormerlySerializedAs("_defaultConfiguration")]
        [SerializeField] private LoogaMenuScreenLayout _defaultLayout;

        [Header("Regions")]
        [SerializeField] private LoogaMenuRegionOverride[] _regionOverrides = Array.Empty<LoogaMenuRegionOverride>();

        [Header("Navigation")]
        [Tooltip("The shared navigation slot that receives this screen's generated links.")]
        [SerializeField] private LoogaMenuRegionDefinition _navigationSlot;
        [Tooltip("Create one navigation link for each layout that is included in navigation.")]
        [SerializeField] private bool _includeLayoutsInNavigation;
        [Tooltip("Adds links to other screens without authoring a navigation content asset.")]
        [SerializeField, LoogaList] private LoogaMenuNavigationEntry[] _navigationLinks =
            Array.Empty<LoogaMenuNavigationEntry>();

        [Header("Behavior")]
        [InspectorName("Open Requirements")]
        [SerializeField] private LoogaMenuRuleSet _rules;
        [SerializeField] private LoogaMenuInputPolicy _inputPolicy;
        [SerializeField] private LoogaMenuOpenMode _defaultOpenMode = LoogaMenuOpenMode.Replace;
        [SerializeField] private LoogaMenuMissingPanelBehavior _missingPanelBehavior = LoogaMenuMissingPanelBehavior.Warn;

        public string DisplayName => name;
        public string Description => _description;
        public LoogaMenuScreenLayout[] Layouts => _layouts;
        public LoogaMenuScreenLayout DefaultLayout => ResolveLayout(null);
        public LoogaMenuRegionOverride[] RegionOverrides => _regionOverrides;
        public LoogaMenuRegionDefinition NavigationSlot => _navigationSlot;
        public bool IncludeLayoutsInNavigation => _includeLayoutsInNavigation;
        public LoogaMenuNavigationEntry[] NavigationLinks => _navigationLinks;
        public LoogaMenuRuleSet Rules => _rules;
        public LoogaMenuInputPolicy InputPolicy => _inputPolicy;
        public LoogaMenuOpenMode DefaultOpenMode => _defaultOpenMode;
        public LoogaMenuMissingPanelBehavior MissingPanelBehavior => _missingPanelBehavior;

        public LoogaMenuScreenLayout ResolveLayout(LoogaMenuScreenLayout requested)
        {
            if (requested != null && ContainsLayout(requested))
                return requested;

            if (_defaultLayout != null && ContainsLayout(_defaultLayout))
                return _defaultLayout;

            foreach (LoogaMenuScreenLayout layout in _layouts ?? Array.Empty<LoogaMenuScreenLayout>())
            {
                if (layout != null)
                    return layout;
            }

            return null;
        }

        public LoogaMenuScreenPanelEntry[] GetPanels(LoogaMenuScreenLayout layout)
        {
            return ResolveLayout(layout)?.Panels ?? Array.Empty<LoogaMenuScreenPanelEntry>();
        }

        public bool ContainsLayout(LoogaMenuScreenLayout layout)
        {
            if (layout == null)
                return false;

            foreach (LoogaMenuScreenLayout candidate in _layouts ?? Array.Empty<LoogaMenuScreenLayout>())
            {
                if (candidate == layout)
                    return true;
            }

            return false;
        }

        public LoogaMenuRegionContent ResolveRegion(
            LoogaMenuScreenLayout layout,
            LoogaMenuRegionDefinition region)
        {
            LoogaMenuRegionContent content = region != null ? region.DefaultContent : null;
            if (!ApplyRegionOverride(_regionOverrides, region, ref content))
                return null;

            LoogaMenuScreenLayout resolvedLayout = ResolveLayout(layout);
            return ApplyRegionOverride(resolvedLayout?.RegionOverrides, region, ref content)
                ? content
                : null;
        }

        private void OnValidate()
        {
            _layouts ??= Array.Empty<LoogaMenuScreenLayout>();
            _regionOverrides ??= Array.Empty<LoogaMenuRegionOverride>();
            _navigationLinks ??= Array.Empty<LoogaMenuNavigationEntry>();
            if (_defaultLayout != null && !ContainsLayout(_defaultLayout))
                _defaultLayout = null;

            _defaultLayout ??= ResolveLayout(null);
        }

        private static bool ApplyRegionOverride(
            LoogaMenuRegionOverride[] overrides,
            LoogaMenuRegionDefinition region,
            ref LoogaMenuRegionContent content)
        {
            foreach (LoogaMenuRegionOverride regionOverride in overrides ?? Array.Empty<LoogaMenuRegionOverride>())
            {
                if (regionOverride == null || regionOverride.Region != region)
                    continue;

                if (regionOverride.Mode == LoogaMenuRegionMode.Hide)
                    return false;

                if (regionOverride.Mode == LoogaMenuRegionMode.Override)
                    content = regionOverride.Content;

                return true;
            }

            return true;
        }
    }
}
