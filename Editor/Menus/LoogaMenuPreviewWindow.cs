using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LoogaSoft.Menu.Editor
{
    public sealed class LoogaMenuPreviewWindow : EditorWindow
    {
        private readonly List<LoogaMenuScreenDefinition> _screens = new();
        private readonly List<LoogaMenuContextDefinition> _contexts = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, bool> _screenFoldouts = new();
        private readonly Dictionary<LoogaMenuPanelDefinition, List<LoogaMenuPanel>> _panelsByDefinition = new();
        private readonly Dictionary<PreviewKey, GameObject[]> _sceneObjectsByPreview = new();
        private readonly HashSet<GameObject> _previewActivatedAncestors = new();
        private static readonly string[] NoContextOption = { "No Context" };
        private string[] _contextNames = NoContextOption;
        private LoogaMenuPanel[] _scenePanels = Array.Empty<LoogaMenuPanel>();
        private LoogaMenuRegionObjectActivator[] _regionActivators = Array.Empty<LoogaMenuRegionObjectActivator>();
        private ILoogaMenuPreviewPresenter[] _previewPresenters = Array.Empty<ILoogaMenuPreviewPresenter>();
        private LoogaMenuRoot _menuRoot;
        private LoogaMenuStructureProfile _structure;
        private LoogaMenuContextDefinition _previewContext;
        private LoogaMenuScreenDefinition _previewedScreen;
        private LoogaMenuScreenLayout _previewedLayout;
        private bool _includeSharedUi = true;
        private bool _definitionsDirty = true;
        private bool _sceneCacheDirty = true;
        private Button _resetButton;
        private DropdownField _contextField;
        private HelpBox _sceneWarning;
        private ScrollView _screenList;
        private ToolbarToggle _sharedUiToggle;

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
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            EditorApplication.projectChanged += HandleProjectChanged;
            RefreshCaches();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            EditorApplication.projectChanged -= HandleProjectChanged;
        }

        public void CreateGUI()
        {
            EnsureCaches();

            VisualElement root = rootVisualElement;
            root.Clear();
            LoogaUiToolkitStyle.AddSharedStyleSheet(root);
            root.style.flexDirection = FlexDirection.Column;

            Toolbar commandBar = new();
            VisualElement commandSpacer = new();
            commandSpacer.style.flexGrow = 1f;
            commandBar.Add(commandSpacer);

            _resetButton = new ToolbarButton(ClearPreview) { text = "Reset" };
            _resetButton.style.width = 64f;
            commandBar.Add(_resetButton);

            ToolbarButton refreshButton = new(RefreshCaches) { text = "Refresh" };
            refreshButton.style.width = 72f;
            commandBar.Add(refreshButton);
            root.Add(commandBar);

            Toolbar previewBar = new();
            _sharedUiToggle = new ToolbarToggle
            {
                text = "Shared UI",
                tooltip = "Include context, navigation, header, background, and action regions."
            };
            _sharedUiToggle.SetValueWithoutNotify(_includeSharedUi);
            _sharedUiToggle.RegisterValueChangedCallback(evt =>
            {
                _includeSharedUi = evt.newValue;
                _contextField?.SetEnabled(_includeSharedUi && _contexts.Count > 0);
                _sceneObjectsByPreview.Clear();
                ReapplyCurrentPreview();
            });
            previewBar.Add(_sharedUiToggle);

            _contextField = new DropdownField();
            _contextField.style.flexGrow = 1f;
            _contextField.style.minWidth = 100f;
            _contextField.RegisterValueChangedCallback(HandleContextChanged);
            previewBar.Add(_contextField);
            root.Add(previewBar);

            _sceneWarning = new HelpBox(
                "Open the UI scene that contains the LoogaMenuPanel objects to preview screens.",
                HelpBoxMessageType.Warning);
            _sceneWarning.style.marginLeft = 4f;
            _sceneWarning.style.marginRight = 4f;
            _sceneWarning.style.marginTop = 4f;
            _sceneWarning.style.marginBottom = 4f;
            root.Add(_sceneWarning);

            _screenList = new ScrollView(ScrollViewMode.Vertical);
            _screenList.style.flexGrow = 1f;
            _screenList.style.paddingLeft = 4f;
            _screenList.style.paddingRight = 4f;
            _screenList.style.paddingTop = 0f;
            _screenList.style.paddingBottom = 4f;
            root.Add(_screenList);

            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            using var _ = LoogaUiToolkitStyle.MenuPreviewRefresh.Auto();
            if (_screenList == null)
                return;

            bool canPreview = _scenePanels.Length > 0;
            _resetButton?.SetEnabled(canPreview);
            _sceneWarning.style.display = canPreview ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshContextField();

            _screenList.Clear();
            foreach (LoogaMenuScreenDefinition screen in _screens)
            {
                if (screen != null)
                    _screenList.Add(CreateScreenElement(screen, canPreview));
            }
        }

        private VisualElement CreateScreenElement(LoogaMenuScreenDefinition screen, bool canPreview)
        {
            int layoutCount = CountLayouts(screen.Layouts);
            if (layoutCount <= 1)
            {
                Button row = CreatePreviewButton(
                    screen.DisplayName,
                    BuildRowTooltip("Preview this screen.", "screen definition"),
                    screen,
                    canPreview,
                    () => Preview(screen, screen.DefaultLayout),
                    () => SelectSceneObjects(screen, screen.DefaultLayout));
                StyleScreenRow(row);
                return row;
            }

            Foldout foldout = new()
            {
                text = $"{screen.DisplayName} ({layoutCount})",
                tooltip = BuildRowTooltip(
                    "Expand or collapse this screen's layouts.",
                    "screen definition")
            };
            foldout.AddToClassList("looga-menu-preview-screen");
            foldout.style.marginLeft = 0f;
            foldout.style.marginRight = 0f;
            foldout.style.marginTop = 0f;
            foldout.style.marginBottom = LoogaUiToolkitStyle.InteractiveRowSpacing;
            foldout.style.borderBottomWidth = 1f;
            foldout.style.borderLeftWidth = 1f;
            foldout.style.borderRightWidth = 1f;
            foldout.style.borderTopWidth = 1f;
            Color border = EditorGUIUtility.isProSkin
                ? new Color(0.14f, 0.14f, 0.14f)
                : new Color(0.58f, 0.58f, 0.58f);
            foldout.style.borderBottomColor = border;
            foldout.style.borderLeftColor = border;
            foldout.style.borderRightColor = border;
            foldout.style.borderTopColor = border;

            bool expanded = _screenFoldouts.TryGetValue(screen, out bool current) && current;
            foldout.SetValueWithoutNotify(expanded);
            foldout.RegisterValueChangedCallback(evt => _screenFoldouts[screen] = evt.newValue);

            Toggle header = foldout.Q<Toggle>();
            if (header != null)
            {
                LoogaUiToolkitStyle.StyleFoldout(foldout);
                RegisterRowShortcuts(
                    header,
                    screen,
                    canPreview,
                    () => SelectSceneObjects(screen, screen.DefaultLayout));
            }

            VisualElement content = foldout.Q<VisualElement>(className: "unity-foldout__content");
            if (content != null)
            {
                content.style.marginLeft = 0f;
                content.style.paddingLeft = 0f;
                content.style.paddingRight = 0f;
            }

            foreach (LoogaMenuScreenLayout layout in screen.Layouts)
            {
                if (layout == null)
                    continue;

                string suffix = layout == screen.DefaultLayout ? " (Default)" : string.Empty;
                Button layoutRow = CreatePreviewButton(
                    layout.DisplayName + suffix,
                    BuildRowTooltip("Preview this layout.", "layout definition"),
                    layout,
                    canPreview,
                    () => Preview(screen, layout),
                    () => SelectSceneObjects(screen, layout));
                layoutRow.style.height = 34f;
                layoutRow.style.marginLeft = 8f;
                layoutRow.style.marginRight = 8f;
                layoutRow.style.marginTop = 2f;
                layoutRow.style.marginBottom = 0f;
                layoutRow.style.paddingLeft = 6f;
                layoutRow.style.unityTextAlign = TextAnchor.MiddleLeft;
                foldout.Add(layoutRow);
            }

            VisualElement bottomPadding = new();
            bottomPadding.style.height = 7f;
            foldout.Add(bottomPadding);
            return foldout;
        }

        private static Button CreatePreviewButton(
            string text,
            string tooltip,
            UnityEngine.Object definition,
            bool canPreview,
            Action preview,
            Action selectSceneObjects)
        {
            Button button = new(() =>
            {
                if (canPreview)
                    preview?.Invoke();
            })
            {
                text = text,
                tooltip = tooltip
            };
            RegisterRowShortcuts(button, definition, canPreview, selectSceneObjects);
            return button;
        }

        private static void StyleScreenRow(Button row)
        {
            LoogaUiToolkitStyle.StyleInteractiveRow(row);
        }

        private static void RegisterRowShortcuts(
            VisualElement row,
            UnityEngine.Object definition,
            bool canSelectSceneObjects,
            Action selectSceneObjects)
        {
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1 && definition != null)
                {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                    AssetDatabase.OpenAsset(definition);
                    evt.StopImmediatePropagation();
                }
                else if (evt.button == 2 && canSelectSceneObjects)
                {
                    selectSceneObjects?.Invoke();
                    evt.StopImmediatePropagation();
                }
            });
        }

        private static string BuildRowTooltip(string leftClickAction, string definitionName)
        {
            return $"Left-click: {leftClickAction}\n"
                   + $"Right-click: Open and ping the {definitionName}.\n"
                   + "Middle-click: Select and ping the corresponding hierarchy object(s).";
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
            RefreshVisualState();
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
            EnsureCaches();
            RefreshVisualState();
        }

        private void HandleProjectChanged()
        {
            _definitionsDirty = true;
            _sceneObjectsByPreview.Clear();
            EnsureCaches();
            RefreshVisualState();
        }

        private void RefreshContextField()
        {
            if (_contextField == null)
                return;

            _contextField.choices = new List<string>(_contextNames);
            int currentIndex = _contexts.Count > 0
                ? Mathf.Max(0, _contexts.IndexOf(_previewContext))
                : 0;
            _contextField.SetValueWithoutNotify(_contextNames[currentIndex]);
            _contextField.SetEnabled(_includeSharedUi && _contexts.Count > 0);
        }

        private void HandleContextChanged(ChangeEvent<string> evt)
        {
            int selectedIndex = _contextField.index;
            if (selectedIndex < 0 || selectedIndex >= _contexts.Count)
                return;

            _previewContext = _contexts[selectedIndex];
            _sceneObjectsByPreview.Clear();
            ReapplyCurrentPreview();
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

            foreach (GameObject ancestor in _previewActivatedAncestors)
            {
                if (ancestor != null && ancestor.activeSelf)
                    ancestor.SetActive(false);
            }

            _previewActivatedAncestors.Clear();
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

                ActivatePanelAncestors(panel);
                panel.Show();
                EditorUtility.SetDirty(panel);
            }
        }

        private void ActivatePanelAncestors(LoogaMenuPanel panel)
        {
            List<GameObject> inactiveAncestors = new();
            Transform ancestor = panel.transform.parent;
            while (ancestor != null)
            {
                if (!ancestor.gameObject.activeSelf)
                    inactiveAncestors.Add(ancestor.gameObject);

                ancestor = ancestor.parent;
            }

            for (int i = inactiveAncestors.Count - 1; i >= 0; i--)
            {
                GameObject inactiveAncestor = inactiveAncestors[i];
                if (_previewActivatedAncestors.Add(inactiveAncestor))
                    inactiveAncestor.SetActive(true);
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
