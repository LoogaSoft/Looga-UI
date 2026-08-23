using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Provides destination entries for a navigation presenter.</summary>
    public sealed class LoogaMenuNavigationRegionContent : LoogaMenuRegionContent
    {
        [SerializeField, Min(0)] private int _defaultEntryIndex;
        [SerializeField] private LoogaMenuNavigationEntry[] _entries = Array.Empty<LoogaMenuNavigationEntry>();

        public override bool SupportsAdd => true;
        public int DefaultEntryIndex => _entries.Length == 0
            ? -1
            : Mathf.Clamp(_defaultEntryIndex, 0, _entries.Length - 1);
        public IReadOnlyList<LoogaMenuNavigationEntry> Entries => _entries;

        internal static LoogaMenuNavigationRegionContent CreateRuntime(
            IReadOnlyList<LoogaMenuNavigationEntry> entries,
            int defaultEntryIndex)
        {
            LoogaMenuNavigationRegionContent content =
                CreateInstance<LoogaMenuNavigationRegionContent>();
            int count = entries?.Count ?? 0;
            content._entries = new LoogaMenuNavigationEntry[count];
            for (int i = 0; i < count; i++)
                content._entries[i] = entries[i];

            content._defaultEntryIndex = count == 0
                ? 0
                : Mathf.Clamp(defaultEntryIndex, 0, count - 1);
            return content;
        }

        /// <summary>Creates transient navigation content for editor presentation.</summary>
        public static LoogaMenuNavigationRegionContent CreatePreview(
            IReadOnlyList<LoogaMenuNavigationEntry> entries,
            int defaultEntryIndex)
        {
            LoogaMenuNavigationRegionContent content = CreateRuntime(entries, defaultEntryIndex);
            content.hideFlags = HideFlags.HideAndDontSave;
            return content;
        }

        public override void CollectPanels(List<LoogaMenuPanelDefinition> panels)
        {
        }

        internal override bool AddFrom(LoogaMenuRegionContent addition)
        {
            if (addition is not LoogaMenuNavigationRegionContent navigationAddition)
                return false;

            int inheritedCount = _entries?.Length ?? 0;
            List<LoogaMenuNavigationEntry> combined = new(
                _entries ?? Array.Empty<LoogaMenuNavigationEntry>());
            foreach (LoogaMenuNavigationEntry entry in navigationAddition._entries
                ?? Array.Empty<LoogaMenuNavigationEntry>())
            {
                if (entry != null && !ContainsEquivalent(combined, entry))
                    combined.Add(entry);
            }

            _entries = combined.ToArray();
            if (inheritedCount == 0 && _entries.Length > 0)
            {
                _defaultEntryIndex = Mathf.Clamp(
                    navigationAddition._defaultEntryIndex,
                    0,
                    _entries.Length - 1);
            }

            return true;
        }

        protected override void CopyTo(LoogaMenuRegionContent copy)
        {
            LoogaMenuNavigationRegionContent navigationCopy =
                (LoogaMenuNavigationRegionContent)copy;
            navigationCopy._defaultEntryIndex = _defaultEntryIndex;
            navigationCopy._entries = (LoogaMenuNavigationEntry[])(_entries?.Clone()
                ?? Array.Empty<LoogaMenuNavigationEntry>());
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
    }
}
