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
        private static readonly string[] NoContextOption = { "No Context" };
        private static GUIStyle _configurationButtonStyle;
        private string[] _contextNames = NoContextOption;
        private Vector2 _scrollPosition;
        private LoogaMenuContextDefinition _previewContext;
        private LoogaMenuScreenDefinition _previewedScreen;
        private LoogaMenuScreenLayout _previewedLayout;
        private bool _includeSharedUi = true;

        [MenuItem("LoogaSoft/UI/Menus/Menu Preview", priority = 23)]
        public static void Open()
        {
            LoogaMenuPreviewWindow window = GetWindow<LoogaMenuPreviewWindow>("Menu Preview");
            window.minSize = new Vector2(360f, 260f);
            window.RefreshDefinitions();
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnFocus()
        {
            RefreshDefinitions();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove)
                Repaint();

            LoogaMenuPanel[] panels = LoogaMenuEditorUtility.FindScenePanels();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(panels.Length == 0))
                {
                    if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                        ClearPreview(panels);
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    RefreshDefinitions();
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
                ReapplyCurrentPreview();

            if (panels.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Open the UI scene that contains the LoogaMenuPanel objects to preview screens.",
                    MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (LoogaMenuScreenDefinition screen in _screens)
                DrawScreen(screen, panels);
            EditorGUILayout.EndScrollView();
        }

        private void DrawScreen(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] scenePanels)
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

                bool canPreview = scenePanels.Length > 0;
                if (DrawScreenHeader(
                        headerRect,
                        clickRect,
                        label,
                        screen,
                        hasLayoutFoldout,
                        expanded,
                        canPreview,
                        HasSceneObjects(screen, screen.DefaultLayout, scenePanels),
                        () => Preview(screen, screen.DefaultLayout),
                        () => SelectSceneObjects(screen, screen.DefaultLayout, scenePanels)))
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
                        HasSceneObjects(screen, layout, scenePanels),
                        () => Preview(screen, layout),
                        () => SelectSceneObjects(screen, layout, scenePanels));
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
                    LoogaMenuEditorUtility.FindMenuRoot()?.DefaultContext;
                _previewContext = defaultContext != null && _contexts.Contains(defaultContext)
                    ? defaultContext
                    : _contexts.Count > 0 ? _contexts[0] : null;
            }
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

        private void ClearPreview(LoogaMenuPanel[] panels)
        {
            _previewedScreen = null;
            _previewedLayout = null;
            ResetPreview(panels);
        }

        private bool HasSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuPanel[] scenePanels)
        {
            return FindSceneObjects(screen, layout, scenePanels).Count > 0;
        }

        private void SelectSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuPanel[] scenePanels)
        {
            List<GameObject> sceneObjects = FindSceneObjects(screen, layout, scenePanels);
            if (sceneObjects.Count == 0)
                return;

            UnityEngine.Object[] selection = new UnityEngine.Object[sceneObjects.Count];
            for (int i = 0; i < sceneObjects.Count; i++)
                selection[i] = sceneObjects[i];

            Selection.objects = selection;
            Selection.activeObject = sceneObjects[0];
            EditorGUIUtility.PingObject(sceneObjects[0]);
        }

        private List<GameObject> FindSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuPanel[] scenePanels)
        {
            HashSet<LoogaMenuPanelDefinition> definitions = CollectPanelDefinitions(screen, layout);
            List<GameObject> sceneObjects = new();
            foreach (LoogaMenuPanel panel in scenePanels)
            {
                if (panel != null && panel.Panel != null && definitions.Contains(panel.Panel))
                    sceneObjects.Add(panel.gameObject);
            }

            if (_includeSharedUi)
            {
                LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
                LoogaMenuStructureProfile structure = ResolveStructure(root);
                foreach (LoogaMenuRegionObjectActivator activator in FindRegionActivators())
                {
                    if (activator == null
                        || activator.Target == null
                        || structure == null
                        || !RegionHasPreviewContent(root, screen, layout, activator.Region))
                    {
                        continue;
                    }

                    if (!sceneObjects.Contains(activator.Target))
                        sceneObjects.Add(activator.Target);
                }
            }

            return sceneObjects;
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

            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            LoogaMenuStructureProfile structure = ResolveStructure(root);
            if (!_includeSharedUi || structure == null)
                return definitions;

            List<LoogaMenuPanelDefinition> regionPanels = new();
            foreach (LoogaMenuRegionDefinition region in structure.Regions)
            {
                LoogaMenuRegionPanelResolver.Collect(
                    root,
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
            _previewedScreen = screen;
            _previewedLayout = layout;
            LoogaMenuPanel[] panels = LoogaMenuEditorUtility.FindScenePanels();
            ResetPreview(panels);

            HashSet<LoogaMenuPanelDefinition> definitions = CollectPanelDefinitions(screen, layout);
            foreach (LoogaMenuPanelDefinition definition in definitions)
                ShowPanel(definition);

            if (_includeSharedUi)
                ApplySharedPreview(screen, layout);
        }

        private static void ResetPreview(LoogaMenuPanel[] panels)
        {
            foreach (ILoogaMenuPreviewPresenter presenter in FindPreviewPresenters())
                presenter.ClearMenuPreview();

            foreach (LoogaMenuRegionObjectActivator activator in FindRegionActivators())
            {
                if (activator != null && activator.Target != null && activator.Target.activeSelf)
                    activator.Target.SetActive(false);
            }

            foreach (LoogaMenuPanel panel in panels)
            {
                panel.Hide();
                EditorUtility.SetDirty(panel);
            }
        }

        private static void ShowPanel(LoogaMenuPanelDefinition definition)
        {
            if (definition == null
                || !LoogaMenuEditorUtility.TryFindPanel(definition, out LoogaMenuPanel panel))
            {
                return;
            }

            panel.Show();
            EditorUtility.SetDirty(panel);
        }

        private void ApplySharedPreview(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout)
        {
            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            LoogaMenuStructureProfile structure = ResolveStructure(root);
            if (structure == null)
                return;

            foreach (LoogaMenuRegionDefinition region in structure.Regions)
            {
                if (region == null)
                    continue;

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

                foreach (LoogaMenuRegionObjectActivator activator in FindRegionActivators())
                {
                    if (activator != null
                        && activator.Region == region
                        && activator.Target != null
                        && activator.Target.activeSelf != hasContent)
                    {
                        activator.Target.SetActive(hasContent);
                    }
                }

                foreach (ILoogaMenuPreviewPresenter presenter in FindPreviewPresenters())
                {
                    if (presenter.Region == region)
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
    }
}
