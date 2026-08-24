using System.Collections.Generic;
using LoogaSoft.UI.Controls;
using LoogaSoft.UI.Design;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    public sealed class LoogaDesignSystemWindow : EditorWindow
    {
        private readonly List<GameObject> _unboundObjects = new List<GameObject>();
        private LoogaDesignSystem _designSystem;
        private LoogaElementStyle _mergeTarget;
        private int _reuseThreshold = 2;
        private string _librarySummary;
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
        }

        private void OnGUI()
        {
            DrawAdoption();
            EditorGUILayout.Space(10f);
            DrawStyleLibrary();
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
                if (GUILayout.Button("Analyze Loaded UI"))
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
            Repaint();
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
