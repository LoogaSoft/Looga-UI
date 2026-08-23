using System;
using System.Collections.Generic;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Owns screen history, layout state, panel visibility, navigation, and action collection.</summary>
    public sealed class LoogaMenuManager
    {
        private readonly Dictionary<LoogaMenuPanelDefinition, LoogaMenuPanel> _panels = new();
        private readonly List<LoogaMenuScreenDefinition> _openScreens = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, LoogaMenuScreenLayout> _activeLayouts = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, LoogaMenuOpenMode> _openModes = new();
        private readonly List<LoogaMenuPanel> _visiblePanels = new();
        private readonly HashSet<LoogaBlackboardKey> _ownedParameterKeys = new();
        private readonly ILoogaBlackboardReader _blackboardReader;
        private readonly ILoogaBlackboardWriter _blackboardWriter;
        private readonly LoogaMenuStructureProfile _structure;
        private readonly Dictionary<LoogaMenuRegionDefinition, NavigationRuntime> _navigation = new();
        private readonly Dictionary<LoogaMenuRegionDefinition, ActionBarRuntime> _actionBars = new();
        private readonly List<LoogaMenuRegionContent> _resolvedRegionContents = new();
        private readonly Dictionary<LoogaMenuRegionDefinition, LoogaMenuRegionContent>
            _resolvedRegionContentByRegion = new();
        private readonly List<LoogaMenuRegionContent> _runtimeRegionContents = new();
        private readonly List<LoogaMenuPanelDefinition> _regionPanels = new();

        private ILoogaMenuTransitionHandler _transitionHandler;
        private ILoogaMenuAudioHandler _audioHandler;
        private LoogaMenuContextDefinition _activeContext;

        public LoogaMenuManager(
            ILoogaBlackboardReader blackboardReader,
            ILoogaBlackboardWriter blackboardWriter,
            LoogaMenuStructureProfile structure)
        {
            _blackboardReader = blackboardReader;
            _blackboardWriter = blackboardWriter;
            _structure = structure;

            if (_structure == null)
                return;

            foreach (LoogaMenuRegionDefinition region in _structure.Regions)
            {
                if (region == null)
                    continue;

                if (typeof(LoogaMenuNavigationRegionContent).IsAssignableFrom(region.ContentType))
                    _navigation[region] = new NavigationRuntime(this, region);
                else if (typeof(LoogaMenuActionRegionContent).IsAssignableFrom(region.ContentType))
                    _actionBars[region] = new ActionBarRuntime(this, region);
            }
        }

        public event Action<LoogaMenuState> StateChanged;

        public IReadOnlyList<LoogaMenuScreenDefinition> OpenScreens => _openScreens;
        public IReadOnlyList<LoogaMenuPanel> VisiblePanels => _visiblePanels;
        public LoogaMenuInputPolicy ActiveInputPolicy => TopScreen != null ? TopScreen.InputPolicy : null;
        public LoogaMenuContextDefinition ActiveContext => _activeContext;

        internal ILoogaBlackboardReader BlackboardReader => _blackboardReader;
        internal LoogaMenuScreenDefinition TopScreen => _openScreens.Count > 0 ? _openScreens[^1] : null;

        public LoogaMenuScreenLayout GetActiveLayout(LoogaMenuScreenDefinition screen)
        {
            return screen != null && _activeLayouts.TryGetValue(screen, out LoogaMenuScreenLayout layout)
                ? layout
                : screen?.ResolveLayout(null);
        }

        public bool TryGetNavigation(LoogaMenuRegionDefinition region, out ILoogaMenuNavigation navigation)
        {
            if (region != null
                && _navigation.TryGetValue(region, out NavigationRuntime runtime)
                && runtime.HasEntries)
            {
                navigation = runtime;
                return true;
            }

            navigation = null;
            return false;
        }

        public bool TryGetActionBar(LoogaMenuRegionDefinition region, out ILoogaMenuActionBar actionBar)
        {
            if (region != null
                && _actionBars.TryGetValue(region, out ActionBarRuntime runtime)
                && runtime.IsVisible)
            {
                actionBar = runtime;
                return true;
            }

            actionBar = null;
            return false;
        }

        /// <summary>Resolves the active content for a configured region.</summary>
        public LoogaMenuRegionContent ResolveRegionContent(LoogaMenuRegionDefinition region)
        {
            if (region == null || _structure == null || !_structure.Contains(region))
                return null;

            return _resolvedRegionContentByRegion.TryGetValue(
                region,
                out LoogaMenuRegionContent content)
                ? content
                : null;
        }

        /// <summary>Changes the persistent presentation context without opening a screen.</summary>
        public void SetContext(LoogaMenuContextDefinition context)
        {
            if (_activeContext == context)
                return;

            _activeContext = context;
            RebuildPresentation();
            NotifyStateChanged();
        }

        /// <summary>Rebuilds region and panel presentation after scene registration changes.</summary>
        public void RefreshPresentation()
        {
            RebuildPresentation();
        }

        /// <summary>Releases transient content and subscriptions owned by this manager.</summary>
        public void Dispose()
        {
            foreach (ActionBarRuntime actionBar in _actionBars.Values)
                actionBar.ReleasePanelSubscriptions();

            ClearRuntimeRegionContents();
            _resolvedRegionContents.Clear();
            _resolvedRegionContentByRegion.Clear();
        }

        public void SetTransitionHandler(ILoogaMenuTransitionHandler transitionHandler)
        {
            _transitionHandler = transitionHandler;
        }

        public void SetAudioHandler(ILoogaMenuAudioHandler audioHandler)
        {
            _audioHandler = audioHandler;
        }

        public void RegisterPanel(LoogaMenuPanel panel)
        {
            if (panel == null || panel.Panel == null)
                return;

            _panels[panel.Panel] = panel;
            panel.Hide();
        }

        public void UnregisterPanel(LoogaMenuPanel panel)
        {
            if (panel == null || panel.Panel == null)
                return;

            if (_panels.TryGetValue(panel.Panel, out LoogaMenuPanel current) && current == panel)
                _panels.Remove(panel.Panel);
        }

        public bool Open(LoogaMenuDestination destination, UnityEngine.Object requester = null, object payload = null)
        {
            return destination != null
                && Open(destination.Screen, destination.Layout, destination.OpenMode, requester, payload);
        }

        public bool Open(LoogaMenuScreenDefinition screen, UnityEngine.Object requester = null, object payload = null)
        {
            return screen != null && Open(screen, null, screen.DefaultOpenMode, requester, payload);
        }

        public bool Open(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            UnityEngine.Object requester = null, object payload = null)
        {
            return screen != null && Open(screen, layout, screen.DefaultOpenMode, requester, payload);
        }

        public bool Open(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout, LoogaMenuOpenMode mode,
            UnityEngine.Object requester = null, object payload = null)
        {
            if (screen == null || (layout != null && !screen.ContainsLayout(layout)))
                return false;

            layout = screen.ResolveLayout(layout);
            if (_openScreens.Contains(screen))
                return SetLayout(screen, layout, requester);

            if (screen.Rules != null && !screen.Rules.CanOpen(_blackboardReader, out string failedReason))
            {
                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}'. {failedReason}", requester);
                return false;
            }

            if (!CanOpen(screen, layout, requester))
                return false;

            HashSet<LoogaMenuPanel> previouslyVisiblePanels = new(_visiblePanels);
            if (mode == LoogaMenuOpenMode.Replace)
                CloseAll(false, false);

            _openScreens.Add(screen);
            _activeLayouts[screen] = layout;
            _openModes[screen] = mode;
            RebuildPresentation();

            LoogaMenuPanel[] openedPanels = ResolvePanels(screen, layout, includeSharedPresentation: true);
            _transitionHandler?.PlayOpen(screen, ExcludePreviouslyVisible(openedPanels, previouslyVisiblePanels));
            _audioHandler?.PlayOpen(screen, openedPanels);
            NotifyStateChanged();
            return true;
        }

        private static LoogaMenuPanel[] ExcludePreviouslyVisible(
            LoogaMenuPanel[] panels,
            HashSet<LoogaMenuPanel> previouslyVisiblePanels)
        {
            if (panels == null || panels.Length == 0 || previouslyVisiblePanels == null ||
                previouslyVisiblePanels.Count == 0)
            {
                return panels;
            }

            List<LoogaMenuPanel> newlyVisiblePanels = new(panels.Length);
            for (int i = 0; i < panels.Length; i++)
            {
                LoogaMenuPanel panel = panels[i];
                if (panel != null && !previouslyVisiblePanels.Contains(panel))
                    newlyVisiblePanels.Add(panel);
            }

            return newlyVisiblePanels.ToArray();
        }

        /// <summary>Changes a screen composition without adding a history entry.</summary>
        public bool SetLayout(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            UnityEngine.Object requester = null)
        {
            if (screen == null || !_openScreens.Contains(screen) || !screen.ContainsLayout(layout))
                return false;

            if (GetActiveLayout(screen) == layout)
                return true;

            if (!CanOpen(screen, layout, requester))
                return false;

            HashSet<LoogaMenuPanel> previouslyVisiblePanels = new(_visiblePanels);
            _activeLayouts[screen] = layout;
            RebuildPresentation();

            LoogaMenuPanel[] openingPanels = Except(_visiblePanels, previouslyVisiblePanels);
            LoogaMenuPanel[] closingPanels = Except(previouslyVisiblePanels, _visiblePanels);
            if (_transitionHandler != null && closingPanels.Length > 0)
            {
                foreach (LoogaMenuPanel panel in closingPanels)
                {
                    panel.Show();
                }

                _transitionHandler.PlayClose(screen, closingPanels, () =>
                {
                    foreach (LoogaMenuPanel panel in closingPanels)
                    {
                        if (panel != null)
                        {
                            panel.Hide();
                        }
                    }
                });
            }

            _transitionHandler?.PlayOpen(screen, openingPanels);

            NotifyStateChanged();
            return true;
        }

        private static LoogaMenuPanel[] Except(
            IEnumerable<LoogaMenuPanel> candidates,
            ICollection<LoogaMenuPanel> exclusions)
        {
            List<LoogaMenuPanel> result = new();
            foreach (LoogaMenuPanel panel in candidates)
            {
                if (panel != null && (exclusions == null || !exclusions.Contains(panel)))
                {
                    result.Add(panel);
                }
            }

            return result.ToArray();
        }

        public bool Back()
        {
            if (_openScreens.Count == 0)
                return false;

            LoogaMenuScreenDefinition screen = _openScreens[^1];
            LoogaMenuScreenLayout layout = GetActiveLayout(screen);
            LoogaMenuPanel[] closingPanels = ResolvePanels(screen, layout, includeSharedPresentation: true);
            _openScreens.RemoveAt(_openScreens.Count - 1);
            _activeLayouts.Remove(screen);
            _openModes.Remove(screen);
            _audioHandler?.PlayClose(screen, closingPanels);

            void CompleteClose()
            {
                RebuildPresentation();
                NotifyStateChanged();
            }

            if (_transitionHandler != null)
                _transitionHandler.PlayClose(screen, closingPanels, CompleteClose);
            else
                CompleteClose();

            return true;
        }

        public void CloseAll()
        {
            CloseAll(true);
        }

        internal void CollectVisiblePanels(List<LoogaMenuPanel> panels, bool includeCovered)
        {
            if (panels == null)
                return;

            panels.Clear();
            foreach (LoogaMenuPanel panel in _visiblePanels)
            {
                if (panel != null && (includeCovered || !panel.IsCovered))
                    panels.Add(panel);
            }
        }

        private void CloseAll(bool notify)
        {
            CloseAll(notify, true);
        }

        private void CloseAll(bool notify, bool rebuild)
        {
            _openScreens.Clear();
            _activeLayouts.Clear();
            _openModes.Clear();
            if (rebuild)
                RebuildPresentation();
            if (notify)
                NotifyStateChanged();
        }

        private bool CanOpen(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            UnityEngine.Object requester)
        {
            if (layout == null)
            {
                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}' because it has no layout.", requester);
                return false;
            }

            if (screen.MissingPanelBehavior != LoogaMenuMissingPanelBehavior.BlockOpen)
                return true;

            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
            {
                if (entry?.Panel != null && _panels.TryGetValue(entry.Panel, out LoogaMenuPanel panel) && panel != null)
                    continue;

                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}' because a required panel is missing.",
                    requester);
                return false;
            }

            return true;
        }

        private void RebuildPresentation()
        {
            foreach (ActionBarRuntime actionBar in _actionBars.Values)
                actionBar.ReleasePanelSubscriptions();

            ClearOwnedParameters();
            _visiblePanels.Clear();

            HashSet<LoogaMenuPanel> desired = new();
            foreach (LoogaMenuScreenDefinition screen in _openScreens)
            {
                LoogaMenuScreenLayout layout = GetActiveLayout(screen);
                foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
                {
                    if (entry == null)
                        continue;

                    ApplyParameters(entry.Parameters);
                    AddPanel(entry.Panel, screen, desired);
                }
            }

            CollectResolvedRegionContents();
            foreach (LoogaMenuRegionContent content in _resolvedRegionContents)
            {
                _regionPanels.Clear();
                content.CollectPanels(_regionPanels);
                foreach (LoogaMenuPanelDefinition panel in _regionPanels)
                    AddPanel(panel, TopScreen, desired);
            }

            foreach (LoogaMenuPanel panel in _panels.Values)
            {
                if (panel == null)
                    continue;

                if (desired.Contains(panel))
                {
                    panel.Show();
                    _visiblePanels.Add(panel);
                }
                else
                {
                    panel.Hide();
                }
            }

            ApplyOverlayCoverage();
            foreach (ActionBarRuntime actionBar in _actionBars.Values)
                actionBar.RefreshActions();
        }

        private void CollectResolvedRegionContents()
        {
            ClearRuntimeRegionContents();
            _resolvedRegionContents.Clear();
            _resolvedRegionContentByRegion.Clear();
            if (_structure == null)
                return;

            foreach (LoogaMenuRegionDefinition region in _structure.Regions)
            {
                if (region == null)
                    continue;

                LoogaMenuScreenDefinition screen = TopScreen;
                LoogaMenuRegionContent content = screen != null
                    ? region.DefaultContent
                    : null;
                ApplyRegionOverrides(_activeContext?.RegionOverrides, region, ref content);

                if (screen != null)
                {
                    ApplyRegionOverrides(screen.RegionOverrides, region, ref content);
                    ApplyRegionOverrides(
                        GetActiveLayout(screen)?.RegionOverrides,
                        region,
                        ref content);
                    AddGeneratedNavigation(screen, region, ref content);
                }

                if (content == null)
                    continue;

                _resolvedRegionContents.Add(content);
                _resolvedRegionContentByRegion[region] = content;
            }
        }

        private void AddGeneratedNavigation(
            LoogaMenuScreenDefinition screen,
            LoogaMenuRegionDefinition region,
            ref LoogaMenuRegionContent content)
        {
            if (screen == null
                || screen.NavigationSlot != region
                || !typeof(LoogaMenuNavigationRegionContent).IsAssignableFrom(region.ContentType))
            {
                return;
            }

            if (IsRegionHidden(screen.RegionOverrides, region)
                || IsRegionHidden(GetActiveLayout(screen)?.RegionOverrides, region))
            {
                return;
            }

            List<LoogaMenuNavigationEntry> entries = new();
            if (screen.IncludeLayoutsInNavigation)
            {
                foreach (LoogaMenuScreenLayout layout in screen.Layouts
                    ?? Array.Empty<LoogaMenuScreenLayout>())
                {
                    if (layout == null || !layout.IncludeInNavigation)
                        continue;

                    entries.Add(LoogaMenuNavigationEntry.Create(
                        layout.DisplayName,
                        LoogaMenuDestination.Create(
                            screen,
                            layout,
                            LoogaMenuOpenMode.Replace),
                        layout.NavigationRequirements));
                }
            }

            foreach (LoogaMenuNavigationEntry entry in screen.NavigationLinks
                ?? Array.Empty<LoogaMenuNavigationEntry>())
            {
                if (entry != null)
                    entries.Add(entry);
            }

            if (entries.Count == 0)
                return;

            int selectedIndex = 0;
            LoogaMenuScreenLayout activeLayout = GetActiveLayout(screen);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i]?.Destination?.Matches(screen, activeLayout) == true)
                {
                    selectedIndex = i;
                    break;
                }
            }

            LoogaMenuNavigationRegionContent generated =
                LoogaMenuNavigationRegionContent.CreateRuntime(entries, selectedIndex);
            generated.name = $"{screen.name} Navigation";
            _runtimeRegionContents.Add(generated);
            AddRegionContent(region, generated, ref content);
        }

        private static bool IsRegionHidden(
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

        private void ApplyRegionOverrides(
            LoogaMenuRegionOverride[] overrides,
            LoogaMenuRegionDefinition region,
            ref LoogaMenuRegionContent content)
        {
            foreach (LoogaMenuRegionOverride regionOverride in overrides
                ?? Array.Empty<LoogaMenuRegionOverride>())
            {
                if (regionOverride == null || regionOverride.Region != region)
                    continue;

                switch (regionOverride.Mode)
                {
                    case LoogaMenuRegionMode.Override:
                        content = regionOverride.Content;
                        break;
                    case LoogaMenuRegionMode.Hide:
                        content = null;
                        break;
                    case LoogaMenuRegionMode.Add:
                        AddRegionContent(region, regionOverride.Content, ref content);
                        break;
                }

                return;
            }
        }

        private void AddRegionContent(
            LoogaMenuRegionDefinition region,
            LoogaMenuRegionContent addition,
            ref LoogaMenuRegionContent content)
        {
            if (addition == null)
                return;

            if (content == null)
            {
                content = addition;
                return;
            }

            if (!content.SupportsAdd || content.GetType() != addition.GetType())
            {
                Debug.LogWarning(
                    $"Menu region '{region.DisplayName}' cannot add content of type " +
                    $"'{addition.GetType().Name}'. Use Override for this region.");
                return;
            }

            if (!_runtimeRegionContents.Contains(content))
            {
                content = content.CreateRuntimeCopy();
                _runtimeRegionContents.Add(content);
            }

            if (!content.AddFrom(addition))
            {
                Debug.LogWarning(
                    $"Menu region '{region.DisplayName}' rejected additive content " +
                    $"'{addition.name}'.");
            }
        }

        private void ClearRuntimeRegionContents()
        {
            foreach (LoogaMenuRegionContent content in _runtimeRegionContents)
            {
                if (content == null)
                    continue;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(content);
                else
                    UnityEngine.Object.DestroyImmediate(content);
            }

            _runtimeRegionContents.Clear();
        }

        private void AddPanel(LoogaMenuPanelDefinition definition, LoogaMenuScreenDefinition owner,
            HashSet<LoogaMenuPanel> desired)
        {
            if (definition == null)
                return;

            if (_panels.TryGetValue(definition, out LoogaMenuPanel panel) && panel != null)
            {
                desired.Add(panel);
                return;
            }

            if (owner != null && owner.MissingPanelBehavior == LoogaMenuMissingPanelBehavior.Warn)
                Debug.LogWarning($"Menu screen '{owner.DisplayName}' could not find panel '{definition.name}'.");
        }

        private void ApplyOverlayCoverage()
        {
            LoogaMenuScreenDefinition top = TopScreen;
            bool hasOverlay = top != null
                && _openScreens.Count > 1
                && _openModes.TryGetValue(top, out LoogaMenuOpenMode mode)
                && mode == LoogaMenuOpenMode.Overlay;

            HashSet<LoogaMenuPanel> topPanels = hasOverlay
                ? new HashSet<LoogaMenuPanel>(ResolvePanels(
                    top,
                    GetActiveLayout(top),
                    includeSharedPresentation: true))
                : null;

            foreach (LoogaMenuPanel panel in _visiblePanels)
                panel.SetCovered(hasOverlay && !topPanels.Contains(panel));
        }

        private LoogaMenuPanel[] ResolvePanels(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            bool includeSharedPresentation)
        {
            if (screen == null)
                return Array.Empty<LoogaMenuPanel>();

            List<LoogaMenuPanel> result = new();
            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
                AddResolvedPanel(entry?.Panel, result);

            if (includeSharedPresentation && screen == TopScreen)
            {
                foreach (LoogaMenuRegionContent content in _resolvedRegionContents)
                {
                    _regionPanels.Clear();
                    content.CollectPanels(_regionPanels);
                    foreach (LoogaMenuPanelDefinition panel in _regionPanels)
                        AddResolvedPanel(panel, result);
                }
            }

            return result.ToArray();
        }

        private void AddResolvedPanel(LoogaMenuPanelDefinition definition, List<LoogaMenuPanel> panels)
        {
            if (definition != null
                && _panels.TryGetValue(definition, out LoogaMenuPanel panel)
                && panel != null
                && !panels.Contains(panel))
            {
                panels.Add(panel);
            }
        }

        private void ApplyParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            if (parameters == null)
                return;

            foreach (LoogaMenuBlackboardParameter parameter in parameters)
            {
                if (parameter == null || !parameter.TryGetValue(out LoogaBlackboardValue value))
                    continue;

                _blackboardWriter.SetValue(parameter.Key, value);
                _ownedParameterKeys.Add(parameter.Key);
            }
        }

        private void ClearOwnedParameters()
        {
            foreach (LoogaBlackboardKey key in _ownedParameterKeys)
            {
                if (key != null)
                    _blackboardWriter.RemoveValue(key);
            }

            _ownedParameterKeys.Clear();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(new LoogaMenuState(_openScreens.ToArray(), ActiveInputPolicy));
        }

        private sealed class NavigationRuntime : ILoogaMenuNavigation
        {
            private readonly LoogaMenuManager _manager;

            public NavigationRuntime(LoogaMenuManager manager, LoogaMenuRegionDefinition region)
            {
                _manager = manager;
                Region = region;
            }

            public LoogaMenuRegionDefinition Region { get; }
            public IReadOnlyList<LoogaMenuNavigationEntry> Entries => Content?.Entries
                ?? Array.Empty<LoogaMenuNavigationEntry>();
            public bool HasEntries => Entries.Count > 0;
            public int SelectedIndex
            {
                get
                {
                    LoogaMenuNavigationRegionContent content = Content;
                    if (content == null)
                        return -1;

                    for (int i = 0; i < content.Entries.Count; i++)
                    {
                        LoogaMenuDestination destination = content.Entries[i]?.Destination;
                        if (destination != null
                            && destination.Matches(_manager.TopScreen,
                                _manager.GetActiveLayout(_manager.TopScreen)))
                        {
                            return i;
                        }
                    }

                    return content.DefaultEntryIndex;
                }
            }

            private LoogaMenuNavigationRegionContent Content =>
                _manager.ResolveRegionContent(Region) as LoogaMenuNavigationRegionContent;

            public bool Select(int index)
            {
                LoogaMenuNavigationRegionContent content = Content;
                if (content == null || index < 0 || index >= content.Entries.Count)
                    return false;

                LoogaMenuNavigationEntry entry = content.Entries[index];
                if (entry?.Destination == null || !entry.Destination.IsAssigned)
                    return false;

                if (entry.Requirements != null
                    && !entry.Requirements.CanOpen(_manager._blackboardReader, out string failedReason))
                {
                    Debug.LogWarning($"Cannot open '{entry.DisplayName}'. {failedReason}");
                    return false;
                }

                LoogaMenuDestination destination = entry.Destination;
                if (destination.Screen == _manager.TopScreen)
                    return _manager.SetLayout(destination.Screen,
                        destination.Screen.ResolveLayout(destination.Layout));

                return _manager.Open(destination);
            }

            public bool SelectRelative(int direction)
            {
                if (!HasEntries || direction == 0)
                    return false;

                int current = SelectedIndex;
                int next = (Mathf.Max(0, current) + Math.Sign(direction) + Entries.Count) % Entries.Count;
                return Select(next);
            }

        }

        private sealed class ActionBarRuntime : ILoogaMenuActionBar
        {
            private readonly LoogaMenuManager _manager;
            private readonly List<LoogaMenuActionDescriptor> _actions = new();
            private readonly List<LoogaMenuPanel> _sourcePanels = new();

            private readonly LoogaMenuRegionDefinition _region;

            public ActionBarRuntime(LoogaMenuManager manager, LoogaMenuRegionDefinition region)
            {
                _manager = manager;
                _region = region;
            }

            public IReadOnlyList<LoogaMenuActionDescriptor> Actions => _actions;
            public bool IsVisible => Settings != null;
            public event Action ActionsChanged;

            private LoogaMenuActionRegionContent Settings =>
                _manager.ResolveRegionContent(_region) as LoogaMenuActionRegionContent;

            public void RefreshActions()
            {
                ReleasePanelSubscriptions();
                _actions.Clear();
                LoogaMenuActionRegionContent settings = Settings;
                if (settings == null)
                {
                    ActionsChanged?.Invoke();
                    return;
                }

                if (settings.ShowBackAction)
                {
                    _actions.Add(new LoogaMenuActionDescriptor(
                        "menu.back",
                        settings.BackLabel,
                        settings.BackInputAction,
                        settings.BackBindingFallback,
                        () => _manager.Back(),
                        true,
                        settings.BackSortOrder));
                }

                _manager.CollectVisiblePanels(_sourcePanels, settings.IncludeCoveredPanels);
                foreach (LoogaMenuPanel panel in _sourcePanels)
                {
                    if (panel == null || panel.Panel == settings.Panel)
                        continue;

                    panel.ActionsChanged += RefreshActions;
                    panel.CollectMenuActions(_actions);
                }

                _actions.Sort(CompareActions);
                RemoveDuplicateActions();
                ActionsChanged?.Invoke();
            }

            public void ReleasePanelSubscriptions()
            {
                foreach (LoogaMenuPanel panel in _sourcePanels)
                {
                    if (panel != null)
                        panel.ActionsChanged -= RefreshActions;
                }

                _sourcePanels.Clear();
            }

            private static int CompareActions(LoogaMenuActionDescriptor left, LoogaMenuActionDescriptor right)
            {
                int order = left.SortOrder.CompareTo(right.SortOrder);
                return order != 0 ? order : string.CompareOrdinal(left.Label, right.Label);
            }

            private void RemoveDuplicateActions()
            {
                HashSet<string> ids = new(StringComparer.Ordinal);
                for (int i = _actions.Count - 1; i >= 0; i--)
                {
                    string id = _actions[i].Id;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                        _actions.RemoveAt(i);
                }
            }
        }
    }
}
