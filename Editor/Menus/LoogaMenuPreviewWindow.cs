using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    public sealed class LoogaMenuPreviewWindow : EditorWindow
    {
        private const float ConfigurationTextInsetPixels = 8f;
        private const float ExpandedScreenBottomPaddingPixels = 7f;
        private const float HeaderArrowTextGapPixels = 4f;

        private readonly List<LoogaMenuScreenDefinition> _screens = new();
        private readonly List<LoogaMenuContextDefinition> _contexts = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, bool> _screenFoldouts = new();
        private readonly Dictionary<LoogaMenuPanelDefinition, List<LoogaMenuPanel>> _panelsByDefinition = new();
        private readonly Dictionary<PreviewKey, GameObject[]> _sceneObjectsByPreview = new();
        private static readonly string[] NoContextOption = { "No Context" };
        private static GUIStyle _configurationButtonStyle;
        private string[] _contextNames = NoContextOption;
        private LoogaMenuPanel[] _scenePanels = Array.Empty<LoogaMenuPanel>();
        private LoogaMenuRegionObjectActivator[] _regionActivators = Array.Empty<LoogaMenuRegionObjectActivator>();
        private ILoogaMenuPreviewPresenter[] _previewPresenters = Array.Empty<ILoogaMenuPreviewPresenter>();
        private LoogaMenuRoot _menuRoot;
        private LoogaMenuStructureProfile _structure;
        private Vector2 _scrollPosition;
        private LoogaMenuContextDefinition _previewContext;
        private LoogaMenuScreenDefinition _previewedScreen;
        private LoogaMenuScreenLayout _previewedLayout;
        private bool _includeSharedUi = true;
        private bool _definitionsDirty = true;
        private bool _sceneCacheDirty = true;

        [MenuItem("LoogaSoft/UI/Menus/Menu Preview", priority = 23)]
        public static void Open()
        {
            LoogaMenuPreviewWindow window = GetWindow<LoogaMenuPreviewWindow>("Menu Preview");
            window.minSize = new Vector2(360f, 260f);
            window.RefreshCaches();
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            EditorApplication.projectChanged += HandleProjectChanged;
            RefreshCaches();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            EditorApplication.projectChanged -= HandleProjectChanged;
        }

        private void OnGUI()
        {
            EnsureCaches();

            if (Event.current.type == EventType.MouseMove)
                Repaint();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_scenePanels.Length == 0))
                {
                    if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                        ClearPreview();
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    RefreshCaches();
            }

            bool sharedUiChanged;
            bool contextChanged;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _includeSharedUi = GUILayout.Toggle(
                    _includeSharedUi,
                    new GUIContent(
                        "Shared UI",
                        "Include context, navigation, header, background, and action regions."),
                    EditorStyles.toolbarButton,
                    GUILayout.Width(78f));
                sharedUiChanged = EditorGUI.EndChangeCheck();
                using (new EditorGUI.DisabledScope(!_includeSharedUi))
                    contextChanged = DrawContextSelector();
            }

            if (sharedUiChanged || contextChanged)
            {
                _sceneObjectsByPreview.Clear();
                ReapplyCurrentPreview();
            }

            if (_scenePanels.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Open the UI scene that contains the LoogaMenuPanel objects to preview screens.",
                    MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (LoogaMenuScreenDefinition screen in _screens)
                DrawScreen(screen);
            EditorGUILayout.EndScrollView();
        }

        private void DrawScreen(LoogaMenuScreenDefinition screen)
        {
            if (screen == null)
                return;

            int layoutCount = CountLayouts(screen.Layouts);
            bool hasLayoutFoldout = layoutCount > 1;
            bool expanded = hasLayoutFoldout
                && _screenFoldouts.TryGetValue(screen, out bool current)
                && current;

            using (LoogaEditorFoldouts.BeginLargeFoldoutLayout(
                       expanded,
                       out Rect headerRect,
                       out Rect clickRect))
            {
                string countSuffix = hasLayoutFoldout ? $" ({layoutCount})" : string.Empty;
                GUIContent label = new(
                    screen.DisplayName + countSuffix,
                    BuildRowTooltip(
                        hasLayoutFoldout
                            ? "Expand or collapse this screen's layouts."
                            : "Preview this screen.",
                        "screen definition"));

                bool canPreview = _scenePanels.Length > 0;
                if (DrawScreenHeader(
                        headerRect,
                        clickRect,
                        label,
                        screen,
                        hasLayoutFoldout,
                        expanded,
                        canPreview,
                        canPreview,
                        () => Preview(screen, screen.DefaultLayout),
                        () => SelectSceneObjects(screen, screen.DefaultLayout)))
                {
                    expanded = !expanded;
                }

                if (hasLayoutFoldout)
                    _screenFoldouts[screen] = expanded;

                if (!expanded)
                    return;

                float configurationRowHeight = LoogaEditorFoldouts.GetLargeFoldoutHeaderHeight();
                Rect configurationBlock = EditorGUILayout.GetControlRect(
                    false,
                    configurationRowHeight * layoutCount);
                int configurationIndex = 0;
                foreach (LoogaMenuScreenLayout layout in screen.Layouts)
                {
                    if (layout == null)
                        continue;

                    Rect rowRect = new(
                        configurationBlock.x,
                        configurationBlock.y + configurationRowHeight * configurationIndex,
                        configurationBlock.width,
                        configurationRowHeight);
                    string suffix = layout == screen.DefaultLayout ? " (Default)" : string.Empty;
                    DrawConfigurationRow(
                        rowRect,
                        layout,
                        layout.DisplayName + suffix,
                        canPreview,
                        canPreview,
                        () => Preview(screen, layout),
                        () => SelectSceneObjects(screen, layout));
                    configurationIndex++;
                }

                EditorGUILayout.Space(LoogaEditorStyle.Pixels(ExpandedScreenBottomPaddingPixels));
            }
        }

        private static bool DrawScreenHeader(
            Rect headerRect,
            Rect clickRect,
            GUIContent label,
            ScriptableObject definition,
            bool showFoldout,
            bool expanded,
            bool canPreview,
            bool canSelect,
            Action preview,
            Action selectSceneObjects)
        {
            headerRect = LoogaEditorStyle.PixelSnap(headerRect);
            clickRect = LoogaEditorStyle.PixelSnap(clickRect);

            Rect contentRect = LoogaEditorFoldouts.GetLargeFoldoutHeaderContentRect(headerRect, false);
            Rect arrowRect = LoogaEditorFoldouts.GetLargeFoldoutArrowRect(headerRect);
            if (showFoldout)
            {
                arrowRect.x = contentRect.xMin;
                contentRect.xMin = arrowRect.xMax + LoogaEditorStyle.Pixels(HeaderArrowTextGapPixels);
            }

            Event current = Event.current;
            if (clickRect.Contains(current.mousePosition))
                LoogaEditorFoldouts.DrawHoverRect(clickRect);

            GUI.Label(contentRect, label, EditorStyles.boldLabel);
            if (showFoldout)
                LoogaEditorStyle.DrawFoldoutTriangle(arrowRect, expanded);

            if (HandleRowShortcut(clickRect, definition, canSelect, selectSceneObjects))
                return false;

            if (current.type != EventType.MouseDown
                || current.button != 0
                || !clickRect.Contains(current.mousePosition))
            {
                return false;
            }

            if (showFoldout)
            {
                current.Use();
                return true;
            }

            if (canPreview)
            {
                preview?.Invoke();
                current.Use();
            }

            return false;
        }

        private static void DrawConfigurationRow(
            Rect rowRect,
            LoogaMenuScreenLayout layout,
            string displayName,
            bool canPreview,
            bool canSelect,
            Action preview,
            Action selectSceneObjects)
        {
            rowRect = LoogaEditorStyle.PixelSnap(rowRect);
            if (HandleRowShortcut(rowRect, layout, canSelect, selectSceneObjects))
                return;

            GUIContent content = new(
                string.IsNullOrWhiteSpace(displayName) ? layout.name : displayName,
                BuildRowTooltip("Preview this layout.", "layout definition"));
            GUIStyle buttonStyle = GetConfigurationButtonStyle();
            using (new EditorGUI.DisabledScope(!canPreview))
            {
                if (GUI.Button(rowRect, content, buttonStyle))
                    preview?.Invoke();
            }
        }

        private static bool HandleRowShortcut(
            Rect rowRect,
            UnityEngine.Object definition,
            bool canSelectSceneObjects,
            Action selectSceneObjects)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || !rowRect.Contains(current.mousePosition))
                return false;

            if (current.button == 1 && definition != null)
            {
                Selection.activeObject = definition;
                EditorGUIUtility.PingObject(definition);
                AssetDatabase.OpenAsset(definition);
                current.Use();
                return true;
            }

            if (current.button == 2 && canSelectSceneObjects)
            {
                selectSceneObjects?.Invoke();
                current.Use();
                return true;
            }

            return false;
        }

        private static string BuildRowTooltip(string leftClickAction, string definitionName)
        {
            return $"Left-click: {leftClickAction}\n"
                   + $"Right-click: Open and ping the {definitionName}.\n"
                   + "Middle-click: Select and ping the corresponding hierarchy object(s).";
        }

        private static GUIStyle GetConfigurationButtonStyle()
        {
            _configurationButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedWidth = 0f,
                fixedHeight = 0f,
                stretchWidth = true,
                stretchHeight = true
            };

            _configurationButtonStyle.padding.left = Mathf.CeilToInt(
                LoogaEditorStyle.Pixels(ConfigurationTextInsetPixels));
            return _configurationButtonStyle;
        }

        private static int CountLayouts(LoogaMenuScreenLayout[] layouts)
        {
            if (layouts == null)
                return 0;

            int count = 0;
            foreach (LoogaMenuScreenLayout layout in layouts)
            {
                if (layout != null)
                    count++;
            }

            return count;
        }

        private void RefreshCaches()
        {
            _definitionsDirty = true;
            _sceneCacheDirty = true;
            EnsureCaches();
            Repaint();
        }

        private void EnsureCaches()
        {
            if (_sceneCacheDirty)
                RefreshSceneCache();

            if (_definitionsDirty)
                RefreshDefinitions();
        }

        private void RefreshSceneCache()
        {
            _scenePanels = LoogaMenuEditorUtility.FindScenePanels();
            _menuRoot = LoogaMenuEditorUtility.FindMenuRoot();
            _structure = ResolveStructure(_menuRoot);
            _regionActivators = FindRegionActivators();
            _previewPresenters = FindPreviewPresenters();

            _panelsByDefinition.Clear();
            foreach (LoogaMenuPanel panel in _scenePanels)
            {
                if (panel == null || panel.Panel == null)
                    continue;

                if (!_panelsByDefinition.TryGetValue(panel.Panel, out List<LoogaMenuPanel> matches))
                {
                    matches = new List<LoogaMenuPanel>();
                    _panelsByDefinition.Add(panel.Panel, matches);
                }

                matches.Add(panel);
            }

            _sceneObjectsByPreview.Clear();
            _sceneCacheDirty = false;
        }

        private void RefreshDefinitions()
        {
            _screens.Clear();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LoogaMenuScreenDefinition)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LoogaMenuScreenDefinition screen = AssetDatabase.LoadAssetAtPath<LoogaMenuScreenDefinition>(path);
                if (screen != null)
                    _screens.Add(screen);
            }

            _screens.Sort((left, right) => string.Compare(
                left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty,
                StringComparison.OrdinalIgnoreCase));

            _contexts.Clear();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LoogaMenuContextDefinition)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LoogaMenuContextDefinition context =
                    AssetDatabase.LoadAssetAtPath<LoogaMenuContextDefinition>(path);
                if (context != null)
                    _contexts.Add(context);
            }

            _contexts.Sort((left, right) => string.Compare(
                left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty,
                StringComparison.OrdinalIgnoreCase));

            if (_contexts.Count == 0)
            {
                _contextNames = NoContextOption;
            }
            else
            {
                _contextNames = new string[_contexts.Count];
                for (int index = 0; index < _contexts.Count; index++)
                {
                    _contextNames[index] = _contexts[index] != null
                        ? _contexts[index].name
                        : "Missing Context";
                }
            }

            if (_previewContext == null || !_contexts.Contains(_previewContext))
            {
                LoogaMenuContextDefinition defaultContext =
                    _menuRoot != null ? _menuRoot.DefaultContext : null;
                _previewContext = defaultContext != null && _contexts.Contains(defaultContext)
                    ? defaultContext
                    : _contexts.Count > 0 ? _contexts[0] : null;
            }

            _sceneObjectsByPreview.Clear();
            _definitionsDirty = false;
        }

        private void HandleHierarchyChanged()
        {
            _sceneCacheDirty = true;
            _sceneObjectsByPreview.Clear();
            Repaint();
        }

        private void HandleProjectChanged()
        {
            _definitionsDirty = true;
            _sceneObjectsByPreview.Clear();
            Repaint();
        }

        private bool DrawContextSelector()
        {
            if (_contexts.Count == 0)
            {
                EditorGUILayout.Popup(
                    0,
                    NoContextOption,
                    EditorStyles.toolbarPopup,
                    GUILayout.MinWidth(80f),
                    GUILayout.ExpandWidth(true));
                return false;
            }

            int currentIndex = Mathf.Max(0, _contexts.IndexOf(_previewContext));
            int selectedIndex = EditorGUILayout.Popup(
                currentIndex,
                _contextNames,
                EditorStyles.toolbarPopup,
                GUILayout.MinWidth(80f),
                GUILayout.ExpandWidth(true));
            if (selectedIndex == currentIndex)
                return false;

            _previewContext = _contexts[selectedIndex];
            return true;
        }

        private void ReapplyCurrentPreview()
        {
            if (_previewedScreen != null)
                Preview(_previewedScreen, _previewedLayout);
        }

        private void ClearPreview()
        {
            _previewedScreen = null;
            _previewedLayout = null;
            ResetPreview();
        }

        private void SelectSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout)
        {
            GameObject[] sceneObjects = FindSceneObjects(screen, layout);
            if (sceneObjects.Length == 0)
                return;

            UnityEngine.Object[] selection = new UnityEngine.Object[sceneObjects.Length];
            for (int i = 0; i < sceneObjects.Length; i++)
                selection[i] = sceneObjects[i];

            Selection.objects = selection;
            Selection.activeObject = sceneObjects[0];
            EditorGUIUtility.PingObject(sceneObjects[0]);
        }

        private GameObject[] FindSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout)
        {
            PreviewKey key = new(screen, layout);
            if (_sceneObjectsByPreview.TryGetValue(key, out GameObject[] cached))
                return cached;

            HashSet<LoogaMenuPanelDefinition> definitions = CollectPanelDefinitions(screen, layout);
            List<GameObject> sceneObjects = new();
            HashSet<GameObject> uniqueObjects = new();
            foreach (LoogaMenuPanel panel in _scenePanels)
            {
                if (panel != null
                    && panel.Panel != null
                    && definitions.Contains(panel.Panel)
                    && uniqueObjects.Add(panel.gameObject))
                {
                    sceneObjects.Add(panel.gameObject);
                }
            }

            if (_includeSharedUi && _structure != null)
            {
                Dictionary<LoogaMenuRegionDefinition, bool> contentByRegion = new();
                foreach (LoogaMenuRegionObjectActivator activator in _regionActivators)
                {
                    if (activator == null
                        || activator.Target == null
                        || activator.Region == null)
                    {
                        continue;
                    }

                    if (!contentByRegion.TryGetValue(activator.Region, out bool hasContent))
                    {
                        hasContent = RegionHasPreviewContent(
                            _menuRoot,
                            screen,
                            layout,
                            activator.Region);
                        contentByRegion.Add(activator.Region, hasContent);
                    }

                    if (hasContent && uniqueObjects.Add(activator.Target))
                        sceneObjects.Add(activator.Target);
                }
            }

            cached = sceneObjects.ToArray();
            _sceneObjectsByPreview.Add(key, cached);
            return cached;
        }

        private HashSet<LoogaMenuPanelDefinition> CollectPanelDefinitions(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout)
        {
            HashSet<LoogaMenuPanelDefinition> definitions = new();
            if (screen == null)
                return definitions;

            layout = screen.ResolveLayout(layout);
            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
            {
                if (entry?.Panel != null)
                    definitions.Add(entry.Panel);
            }

            if (!_includeSharedUi || _structure == null)
                return definitions;

            List<LoogaMenuPanelDefinition> regionPanels = new();
            foreach (LoogaMenuRegionDefinition region in _structure.Regions)
            {
                LoogaMenuRegionPanelResolver.Collect(
                    _menuRoot,
                    _previewContext,
                    screen,
                    layout,
                    region,
                    regionPanels);
                foreach (LoogaMenuPanelDefinition panel in regionPanels)
                {
                    if (panel != null)
                        definitions.Add(panel);
                }
            }

            return definitions;
        }

        private void Preview(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout)
        {
            EnsureCaches();
            _previewedScreen = screen;
            _previewedLayout = layout;
            ResetPreview();

            HashSet<LoogaMenuPanelDefinition> definitions = CollectPanelDefinitions(screen, layout);
            foreach (LoogaMenuPanelDefinition definition in definitions)
                ShowPanel(definition);

            if (_includeSharedUi)
                ApplySharedPreview(screen, layout);
        }

        private void ResetPreview()
        {
            foreach (ILoogaMenuPreviewPresenter presenter in _previewPresenters)
            {
                if (IsDestroyedPresenter(presenter))
                    continue;

                presenter.ClearMenuPreview();
            }

            foreach (LoogaMenuRegionObjectActivator activator in _regionActivators)
            {
                if (activator != null && activator.Target != null && activator.Target.activeSelf)
                    activator.Target.SetActive(false);
            }

            foreach (LoogaMenuPanel panel in _scenePanels)
            {
                if (panel == null)
                    continue;

                panel.Hide();
                EditorUtility.SetDirty(panel);
            }
        }

        private void ShowPanel(LoogaMenuPanelDefinition definition)
        {
            if (definition == null
                || !_panelsByDefinition.TryGetValue(definition, out List<LoogaMenuPanel> panels))
                return;

            foreach (LoogaMenuPanel panel in panels)
            {
                if (panel == null)
                    continue;

                panel.Show();
                EditorUtility.SetDirty(panel);
            }
        }

        private void ApplySharedPreview(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout)
        {
            if (_structure == null)
                return;

            foreach (LoogaMenuRegionDefinition region in _structure.Regions)
            {
                if (region == null)
                    continue;

                List<LoogaMenuPanelDefinition> panels = new();
                LoogaMenuRegionPanelResolver.Collect(
                    _menuRoot,
                    _previewContext,
                    screen,
                    layout,
                    region,
                    panels);
                LoogaMenuNavigationRegionContent navigation =
                    LoogaMenuRegionPanelResolver.CreateNavigationPreview(
                        _menuRoot,
                        _previewContext,
                        screen,
                        layout,
                        region);
                bool hasContent = panels.Count > 0 || navigation != null;

                foreach (LoogaMenuRegionObjectActivator activator in _regionActivators)
                {
                    if (activator != null
                        && activator.Region == region
                        && activator.Target != null
                        && activator.Target.activeSelf != hasContent)
                    {
                        activator.Target.SetActive(hasContent);
                    }
                }

                foreach (ILoogaMenuPreviewPresenter presenter in _previewPresenters)
                {
                    if (!IsDestroyedPresenter(presenter) && presenter.Region == region)
                        presenter.ApplyMenuPreview(navigation);
                }

                if (navigation != null)
                    DestroyImmediate(navigation);
            }
        }

        private bool RegionHasPreviewContent(
            LoogaMenuRoot root,
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuRegionDefinition region)
        {
            if (region == null)
                return false;

            List<LoogaMenuPanelDefinition> panels = new();
            LoogaMenuRegionPanelResolver.Collect(
                root,
                _previewContext,
                screen,
                layout,
                region,
                panels);
            LoogaMenuNavigationRegionContent navigation =
                LoogaMenuRegionPanelResolver.CreateNavigationPreview(
                    root,
                    _previewContext,
                    screen,
                    layout,
                    region);
            bool hasContent = panels.Count > 0 || navigation != null;
            if (navigation != null)
                DestroyImmediate(navigation);
            return hasContent;
        }

        private static LoogaMenuStructureProfile ResolveStructure(LoogaMenuRoot root)
        {
            return root != null ? root.Structure : LoogaMenuStructureEditorUtility.FindStructure();
        }

        private static bool IsDestroyedPresenter(ILoogaMenuPreviewPresenter presenter)
        {
            return presenter == null
                   || presenter is UnityEngine.Object unityObject && unityObject == null;
        }

        private static ILoogaMenuPreviewPresenter[] FindPreviewPresenters()
        {
            List<ILoogaMenuPreviewPresenter> presenters = new();
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                if (behaviour is ILoogaMenuPreviewPresenter presenter)
                    presenters.Add(presenter);
            }

            return presenters.ToArray();
        }

        private static LoogaMenuRegionObjectActivator[] FindRegionActivators()
        {
            return UnityEngine.Object.FindObjectsByType<LoogaMenuRegionObjectActivator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        private readonly struct PreviewKey : IEquatable<PreviewKey>
        {
            private readonly int _screenId;
            private readonly int _layoutId;

            public PreviewKey(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout)
            {
                _screenId = screen != null ? screen.GetInstanceID() : 0;
                _layoutId = layout != null ? layout.GetInstanceID() : 0;
            }

            public bool Equals(PreviewKey other)
            {
                return _screenId == other._screenId && _layoutId == other._layoutId;
            }

            public override bool Equals(object obj)
            {
                return obj is PreviewKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_screenId * 397) ^ _layoutId;
                }
            }
        }
    }
}
