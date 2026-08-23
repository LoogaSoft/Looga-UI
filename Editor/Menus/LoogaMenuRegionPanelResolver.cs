using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    /// <summary>Collects region panels for editor previews without creating transient runtime assets.</summary>
    internal static class LoogaMenuRegionPanelResolver
    {
        private static readonly List<LoogaMenuRegionContent> Contents = new();

        public static void Collect(
            LoogaMenuRoot root,
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuRegionDefinition region,
            List<LoogaMenuPanelDefinition> destination)
        {
            Collect(root, ResolveContext(root), screen, layout, region, destination);
        }

        public static void Collect(
            LoogaMenuRoot root,
            LoogaMenuContextDefinition context,
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuRegionDefinition region,
            List<LoogaMenuPanelDefinition> destination)
        {
            destination.Clear();
            if (region == null)
                return;

            context ??= ResolveContext(root);
            CollectContents(context, screen, layout, region);

            foreach (LoogaMenuRegionContent content in Contents)
                content.CollectPanels(destination);

            Contents.Clear();
        }

        public static LoogaMenuNavigationRegionContent CreateNavigationPreview(
            LoogaMenuRoot root,
            LoogaMenuContextDefinition context,
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuRegionDefinition region)
        {
            if (region == null || !typeof(LoogaMenuNavigationRegionContent).IsAssignableFrom(region.ContentType))
                return null;

            context ??= ResolveContext(root);
            layout = screen?.ResolveLayout(layout);
            CollectContents(context, screen, layout, region);

            List<LoogaMenuNavigationEntry> entries = new();
            int selectedIndex = 0;
            foreach (LoogaMenuRegionContent content in Contents)
            {
                if (content is not LoogaMenuNavigationRegionContent navigation)
                    continue;

                int inheritedCount = entries.Count;
                if (inheritedCount == 0)
                    selectedIndex = Mathf.Max(0, navigation.DefaultEntryIndex);

                foreach (LoogaMenuNavigationEntry entry in navigation.Entries)
                {
                    if (entry != null && !ContainsEquivalent(entries, entry))
                        entries.Add(entry);
                }
            }

            Contents.Clear();
            if (screen != null
                && screen.NavigationSlot == region
                && !IsHidden(screen.RegionOverrides, region)
                && !IsHidden(layout?.RegionOverrides, region))
            {
                AddGeneratedEntries(screen, layout, entries, ref selectedIndex);
            }

            return entries.Count > 0
                ? LoogaMenuNavigationRegionContent.CreatePreview(entries, selectedIndex)
                : null;
        }

        private static void CollectContents(
            LoogaMenuContextDefinition context,
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuRegionDefinition region)
        {
            Contents.Clear();
            Add(region.DefaultContent);
            Apply(context?.RegionOverrides, region);
            Apply(screen?.RegionOverrides, region);
            Apply(screen?.ResolveLayout(layout)?.RegionOverrides, region);
        }

        private static void AddGeneratedEntries(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout activeLayout,
            List<LoogaMenuNavigationEntry> entries,
            ref int selectedIndex)
        {
            int generatedStart = entries.Count;
            bool useGeneratedSelection = generatedStart == 0;
            if (screen.IncludeLayoutsInNavigation)
            {
                foreach (LoogaMenuScreenLayout layout in screen.Layouts
                    ?? Array.Empty<LoogaMenuScreenLayout>())
                {
                    if (layout == null || !layout.IncludeInNavigation)
                        continue;

                    LoogaMenuNavigationEntry entry = LoogaMenuNavigationEntry.Create(
                        layout.DisplayName,
                        LoogaMenuDestination.Create(screen, layout, LoogaMenuOpenMode.Replace),
                        layout.NavigationRequirements);
                    if (ContainsEquivalent(entries, entry))
                        continue;

                    entries.Add(entry);
                    if (useGeneratedSelection && layout == activeLayout)
                        selectedIndex = entries.Count - 1;
                }
            }

            foreach (LoogaMenuNavigationEntry entry in screen.NavigationLinks
                ?? Array.Empty<LoogaMenuNavigationEntry>())
            {
                if (entry != null && !ContainsEquivalent(entries, entry))
                    entries.Add(entry);
            }

            if (entries.Count > generatedStart)
                selectedIndex = Mathf.Clamp(selectedIndex, 0, entries.Count - 1);
        }

        private static bool ContainsEquivalent(
            List<LoogaMenuNavigationEntry> entries,
            LoogaMenuNavigationEntry candidate)
        {
            foreach (LoogaMenuNavigationEntry entry in entries)
            {
                if (entry == null)
                    continue;

                LoogaMenuDestination left = entry.Destination;
                LoogaMenuDestination right = candidate.Destination;
                if (entry.DisplayName == candidate.DisplayName
                    && entry.Requirements == candidate.Requirements
                    && left?.Screen == right?.Screen
                    && left?.Layout == right?.Layout
                    && left?.OpenMode == right?.OpenMode)
                {
                    return true;
                }
            }

            return false;
        }

        private static LoogaMenuContextDefinition ResolveContext(LoogaMenuRoot root)
        {
            return root != null ? root.ActiveContext ?? root.DefaultContext : null;
        }

        private static bool IsHidden(
            LoogaMenuRegionOverride[] overrides,
            LoogaMenuRegionDefinition region)
        {
            foreach (LoogaMenuRegionOverride regionOverride in overrides
                ?? Array.Empty<LoogaMenuRegionOverride>())
            {
                if (regionOverride != null && regionOverride.Region == region)
                    return regionOverride.Mode == LoogaMenuRegionMode.Hide;
            }

            return false;
        }

        private static void Apply(
            LoogaMenuRegionOverride[] overrides,
            LoogaMenuRegionDefinition region)
        {
            foreach (LoogaMenuRegionOverride regionOverride in overrides
                ?? Array.Empty<LoogaMenuRegionOverride>())
            {
                if (regionOverride == null || regionOverride.Region != region)
                    continue;

                switch (regionOverride.Mode)
                {
                    case LoogaMenuRegionMode.Override:
                        Contents.Clear();
                        Add(regionOverride.Content);
                        break;
                    case LoogaMenuRegionMode.Hide:
                        Contents.Clear();
                        break;
                    case LoogaMenuRegionMode.Add:
                        Add(regionOverride.Content);
                        break;
                }

                return;
            }
        }

        private static void Add(LoogaMenuRegionContent content)
        {
            if (content != null)
                Contents.Add(content);
        }
    }
}
