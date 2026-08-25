using System.Collections.Generic;
using System.IO;
using LoogaSoft.UI.Controls;
using LoogaSoft.UI.Design;
using LoogaSoft.Inspector.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    public sealed class LoogaDesignSystemWindow : EditorWindow
    {
        private readonly List<GameObject> _unboundObjects = new List<GameObject>();
        private LoogaDesignSystem _designSystem;
        private LoogaElementStyle _mergeTarget;
        private int _reuseThreshold = 2;
        private string _librarySummary;
        private string _qualitySummary;
        private string _adoptionOutputFolder = LoogaDesignSystemAdopter.DefaultOutputFolder;
        private string _adoptionSummary;
        private Vector2 _scrollPosition;

        [MenuItem("LoogaSoft/UI/Design System")]
        private static void Open()
        {
            GetWindow<LoogaDesignSystemWindow>("UI Design System");
        }

        private void OnEnable()
        {
            _designSystem = LoogaDesignSystemConsolidator.FindLoadedDesignSystem();
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        public void CreateGUI()
        {
            RebuildUi();
        }

        private void RebuildUi()
        {
            using var _ = LoogaUiToolkitStyle.DesignSystemRefresh.Auto();
            VisualElement root = rootVisualElement;
            root.Clear();
            ScrollView scroll = new(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            root.Add(scroll);

            VisualElement content = LoogaUiToolkitStyle.CreateInspectorRoot();
            scroll.Add(content);
            content.Add(BuildAdoptionSection());
            content.Add(BuildStyleLibrarySection());
            content.Add(BuildDesignQualitySection());
            content.Add(BuildOpenSceneAuditSection());
        }

        private VisualElement BuildAdoptionSection()
        {
            VisualElement section = LoogaUiToolkitStyle.CreateSection(
                "Adopt Authored UI",
                "Create one reusable design system from the visuals in all loaded scenes. Matching controls share styles while current visuals stay unchanged.");
            TextField outputFolder = new("Output Folder") { value = _adoptionOutputFolder };
            outputFolder.RegisterValueChangedCallback(evt => _adoptionOutputFolder = evt.newValue);
            section.Add(outputFolder);
            Button adopt = new(() =>
            {
                AdoptLoadedUi();
                RebuildUi();
            }) { text = "Create Design System From Loaded UI" };
            adopt.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            section.Add(adopt);
            AddSummary(section, _adoptionSummary);
            return section;
        }

        private VisualElement BuildStyleLibrarySection()
        {
            VisualElement section = LoogaUiToolkitStyle.CreateSection(
                "Style Library",
                "Reusable styles belong in the public library. One-off captured styles remain internal and preserve their bound visuals.");
            ObjectField designSystem = new("Design System")
            {
                objectType = typeof(LoogaDesignSystem),
                allowSceneObjects = false,
                value = _designSystem
            };
            designSystem.RegisterValueChangedCallback(evt =>
            {
                _designSystem = evt.newValue as LoogaDesignSystem;
                RebuildUi();
            });
            section.Add(designSystem);
            IntegerField threshold = new("Reusable At") { value = _reuseThreshold };
            threshold.RegisterValueChangedCallback(evt => _reuseThreshold = Mathf.Max(2, evt.newValue));
            section.Add(threshold);

            Button analyze = new(() =>
            {
                AnalyzeStyleLibrary();
                RebuildUi();
            }) { text = "Analyze Style Usage" };
            Button consolidate = new(() =>
            {
                ConsolidateStyleLibrary();
                RebuildUi();
            }) { text = "Consolidate Public Styles" };
            consolidate.SetEnabled(_designSystem != null);
            section.Add(LoogaUiToolkitStyle.CreateButtonRow(analyze, consolidate));

            Button promote = new(() =>
            {
                LoogaDesignSystemConsolidator.PromoteSelection(_designSystem);
                AnalyzeStyleLibrary();
                RebuildUi();
            }) { text = "Promote Styles On Selected Objects" };
            promote.SetEnabled(_designSystem != null && Selection.gameObjects.Length > 0);
            section.Add(promote);

            ObjectField mergeTarget = new("Merge Into")
            {
                objectType = typeof(LoogaElementStyle),
                allowSceneObjects = false,
                value = _mergeTarget
            };
            mergeTarget.RegisterValueChangedCallback(evt =>
            {
                _mergeTarget = evt.newValue as LoogaElementStyle;
                RebuildUi();
            });
            section.Add(mergeTarget);
            Button merge = new(() =>
            {
                LoogaDesignSystemConsolidator.MergeSelectionInto(_mergeTarget);
                AnalyzeStyleLibrary();
                RebuildUi();
            }) { text = "Merge Selected Bindings" };
            merge.SetEnabled(_mergeTarget != null && Selection.gameObjects.Length > 0);
            section.Add(merge);
            AddSummary(section, _librarySummary);
            return section;
        }

        private VisualElement BuildDesignQualitySection()
        {
            VisualElement section = LoogaUiToolkitStyle.CreateSection(
                "Design Quality",
                "Measure typography drift, generated style names, repeated decoration, and serialized runtime clones without changing scene objects or assets.");
            Button analyze = new(() =>
            {
                _qualitySummary = LoogaDesignSystemAuditor.Analyze(_designSystem).ToSummary();
                RebuildUi();
            }) { text = "Analyze Design Quality" };
            Button save = new(() =>
            {
                SaveDesignQualityReport();
                RebuildUi();
            }) { text = "Save Markdown Report" };
            analyze.SetEnabled(_designSystem != null);
            save.SetEnabled(_designSystem != null);
            section.Add(LoogaUiToolkitStyle.CreateButtonRow(analyze, save));
            AddSummary(section, _qualitySummary);
            return section;
        }

        private VisualElement BuildOpenSceneAuditSection()
        {
            VisualElement section = LoogaUiToolkitStyle.CreateSection(
                "Open Scene Audit",
                "Find loaded UGUI and TextMeshPro objects without a style owner. Typed Looga controls cover their labels, icons, fills, and handles automatically.");
            Button scan = new(() =>
            {
                ScanOpenScenes();
                RebuildUi();
            }) { text = "Scan Open Scenes" };
            Button apply = new(() =>
            {
                ApplyAllBindings();
                RebuildUi();
            }) { text = "Apply All Bindings" };
            section.Add(LoogaUiToolkitStyle.CreateButtonRow(scan, apply));
            Button addSelection = new(() =>
            {
                AddBindingsToSelection();
                RebuildUi();
            }) { text = "Add Bindings To Selection" };
            addSelection.SetEnabled(Selection.gameObjects.Length > 0);
            section.Add(addSelection);
            section.Add(new Label($"Unbound styleable objects: {_unboundObjects.Count}"));

            foreach (GameObject current in _unboundObjects)
            {
                if (current == null)
                    continue;

                Button select = new(() =>
                {
                    Selection.activeGameObject = current;
                    EditorGUIUtility.PingObject(current);
                }) { text = GetPath(current) };
                select.style.unityTextAlign = TextAnchor.MiddleLeft;
                section.Add(select);
            }

            return section;
        }

        private static void AddSummary(VisualElement parent, string summary)
        {
            if (!string.IsNullOrWhiteSpace(summary))
                parent.Add(new HelpBox(summary, HelpBoxMessageType.None));
        }

        private void OnSelectionChanged()
        {
            if (rootVisualElement.panel != null)
                RebuildUi();
        }

        private void DrawLegacyGui()
        {
            DrawAdoption();
            EditorGUILayout.Space(10f);
            DrawStyleLibrary();
            EditorGUILayout.Space(10f);
            DrawDesignQuality();
            EditorGUILayout.Space(10f);

            EditorGUILayout.LabelField("Open Scene Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The audit finds loaded UGUI and TextMeshPro objects without a style owner. " +
                "Labels, icons, fills, and handles owned by typed Looga controls are covered automatically.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Open Scenes"))
                {
                    ScanOpenScenes();
                }

                if (GUILayout.Button("Apply All Bindings"))
                {
                    ApplyAllBindings();
                }
            }

            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("Add Bindings To Selection"))
                {
                    AddBindingsToSelection();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Unbound styleable objects: {_unboundObjects.Count}");

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _unboundObjects.Count; i++)
            {
                GameObject current = _unboundObjects[i];
                if (current == null)
                {
                    continue;
                }

                if (GUILayout.Button(current.name, EditorStyles.objectField))
                {
                    Selection.activeGameObject = current;
                    EditorGUIUtility.PingObject(current);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAdoption()
        {
            EditorGUILayout.LabelField("Adopt Authored UI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create one reusable design system from the visuals in all loaded scenes. " +
                "Matching controls share styles, current visuals stay unchanged, and root canvases receive theme scopes.",
                MessageType.Info);

            _adoptionOutputFolder = EditorGUILayout.TextField("Output Folder", _adoptionOutputFolder);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Create Design System From Loaded UI", GUILayout.Height(28f)))
                {
                    AdoptLoadedUi();
                }
            }

            if (!string.IsNullOrEmpty(_adoptionSummary))
            {
                EditorGUILayout.HelpBox(_adoptionSummary, MessageType.None);
            }
        }

        private void AdoptLoadedUi()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Adopt Authored UI",
                "This creates design-system assets and assigns them to UI in every loaded scene. " +
                "The current appearance is captured before assignments are changed.",
                "Adopt UI",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            try
            {
                LoogaDesignSystemAdopter.AdoptionResult result =
                    LoogaDesignSystemAdopter.AdoptLoadedScenes(_adoptionOutputFolder);
                _adoptionSummary =
                    $"Created {result.AssetPath}\n" +
                    $"Controls: {result.Controls}  Elements: {result.Elements}  " +
                    $"Public styles: {result.PublicStyles}  Captured one-offs: {result.CapturedStyles}  " +
                    $"Control styles: {result.ControlStyles}\n" +
                    $"Internal control parts: {result.InternalElementStyles}  Colors: {result.Colors}  " +
                    $"Typography: {result.Typography}";
                ScanOpenScenes();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                _adoptionSummary = $"Adoption failed: {exception.Message}";
            }
        }

        private void DrawStyleLibrary()
        {
            EditorGUILayout.LabelField("Style Library", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Reusable styles belong in the public library. One-off captured styles remain internal " +
                "and continue to preserve their bound visuals.",
                MessageType.Info);

            _designSystem = (LoogaDesignSystem)EditorGUILayout.ObjectField(
                "Design System",
                _designSystem,
                typeof(LoogaDesignSystem),
                false);
            _reuseThreshold = Mathf.Max(2, EditorGUILayout.IntField("Reusable At", _reuseThreshold));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Analyze Style Usage"))
                {
                    AnalyzeStyleLibrary();
                }

                using (new EditorGUI.DisabledScope(_designSystem == null))
                {
                    if (GUILayout.Button("Consolidate Public Styles"))
                    {
                        ConsolidateStyleLibrary();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(_designSystem == null || Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("Promote Styles On Selected Objects"))
                {
                    LoogaDesignSystemConsolidator.PromoteSelection(_designSystem);
                    AnalyzeStyleLibrary();
                }
            }

            _mergeTarget = (LoogaElementStyle)EditorGUILayout.ObjectField(
                "Merge Into",
                _mergeTarget,
                typeof(LoogaElementStyle),
                false);
            using (new EditorGUI.DisabledScope(_mergeTarget == null || Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("Merge Selected Bindings"))
                {
                    LoogaDesignSystemConsolidator.MergeSelectionInto(_mergeTarget);
                    AnalyzeStyleLibrary();
                }
            }

            if (!string.IsNullOrEmpty(_librarySummary))
            {
                EditorGUILayout.HelpBox(_librarySummary, MessageType.None);
            }
        }

        private void DrawDesignQuality()
        {
            EditorGUILayout.LabelField("Design Quality", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Measure typography drift, generated style names, repeated decoration, and serialized runtime clones. " +
                "The audit does not change scene objects or design assets.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(_designSystem == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Analyze Design Quality"))
                {
                    LoogaDesignSystemAuditor.Report report =
                        LoogaDesignSystemAuditor.Analyze(_designSystem);
                    _qualitySummary = report.ToSummary();
                }

                if (GUILayout.Button("Save Markdown Report"))
                {
                    SaveDesignQualityReport();
                }
            }

            if (!string.IsNullOrEmpty(_qualitySummary))
            {
                EditorGUILayout.HelpBox(_qualitySummary, MessageType.None);
            }
        }

        private void SaveDesignQualityReport()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save UI Design Audit",
                "UI Design Audit",
                "md",
                "Choose where to save the loaded-scene audit.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            LoogaDesignSystemAuditor.Report report =
                LoogaDesignSystemAuditor.Analyze(_designSystem);
            File.WriteAllText(path, report.ToMarkdown("UI Design Audit"));
            AssetDatabase.ImportAsset(path);
            _qualitySummary = report.ToSummary();
        }

        private void AnalyzeStyleLibrary()
        {
            _designSystem ??= LoogaDesignSystemConsolidator.FindLoadedDesignSystem();
            if (_designSystem == null)
            {
                _librarySummary = "No design system is assigned to a loaded theme scope.";
                return;
            }

            LoogaDesignSystemConsolidator.Report report =
                LoogaDesignSystemConsolidator.Analyze(_designSystem, _reuseThreshold);
            _librarySummary =
                $"Bound styles: {report.BoundStyles}  Public: {report.PublicStyles}  " +
                $"Captured: {report.CapturedStyles}  Reusable captured: {report.ReusableCapturedStyles}";
        }

        private void ConsolidateStyleLibrary()
        {
            LoogaDesignSystemConsolidator.Report report =
                LoogaDesignSystemConsolidator.Consolidate(_designSystem, _reuseThreshold);
            _librarySummary =
                $"Public library consolidated to {report.PublicStyles} style(s). " +
                $"{report.CapturedStyles} one-off style(s) remain internal and bound.";
        }

        private void ScanOpenScenes()
        {
            _unboundObjects.Clear();
            HashSet<GameObject> candidates = new HashSet<GameObject>();
            HashSet<GameObject> controlOwnedObjects = new HashSet<GameObject>();

            AddCandidates(Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None), candidates);
            AddCandidates(Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None), candidates);
            AddCandidates(Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None), candidates);
            LoogaStyledVisualCoverage.Collect(controlOwnedObjects);

            foreach (GameObject candidate in candidates)
            {
                if (!controlOwnedObjects.Contains(candidate) && !HasStyleOwner(candidate))
                {
                    _unboundObjects.Add(candidate);
                }
            }

            _unboundObjects.Sort((left, right) => string.CompareOrdinal(GetPath(left), GetPath(right)));
            RebuildUi();
        }

        private static void AddCandidates<T>(T[] components, HashSet<GameObject> candidates) where T : Component
        {
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && component.gameObject.scene.IsValid())
                {
                    candidates.Add(component.gameObject);
                }
            }
        }

        private static void ApplyAllBindings()
        {
            LoogaStyleBinding[] bindings = Object.FindObjectsByType<LoogaStyleBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i].ApplyStyle();
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ILoogaStyledControl control)
                {
                    control.ApplyStyle();
                }
            }

            SceneView.RepaintAll();
        }

        private void AddBindingsToSelection()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            HashSet<GameObject> controlOwnedObjects = new HashSet<GameObject>();
            LoogaStyledVisualCoverage.Collect(controlOwnedObjects);

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject selectedObject = selectedObjects[i];
                if (!IsStyleable(selectedObject) || controlOwnedObjects.Contains(selectedObject) ||
                    HasStyleOwner(selectedObject))
                {
                    continue;
                }

                Undo.AddComponent<LoogaStyleBinding>(selectedObject);
            }

            ScanOpenScenes();
        }

        private static bool IsStyleable(GameObject gameObject)
        {
            return gameObject.TryGetComponent(out Graphic _) ||
                   gameObject.TryGetComponent(out TMP_Text _) ||
                   gameObject.TryGetComponent(out Selectable _);
        }

        private static bool HasStyleOwner(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out LoogaStyleBinding _))
            {
                return true;
            }

            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ILoogaStyledControl control &&
                    control.StyleSource == LoogaControlStyleSource.DesignSystem)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetPath(GameObject gameObject)
        {
            string path = gameObject.name;
            Transform parent = gameObject.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
