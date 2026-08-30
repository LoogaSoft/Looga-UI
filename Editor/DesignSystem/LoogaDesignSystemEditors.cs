using System;
using System.Collections.Generic;
using System.IO;
using LoogaSoft.UI.Controls;
using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    [CustomEditor(typeof(LoogaDesignSystem))]
    public sealed class LoogaDesignSystemEditor : UnityEditor.Editor
    {
        private string _synchronizationSummary;
        private string _recoverySummary;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Design System Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Synchronize Style Library"))
            {
                SynchronizeStyleLibrary();
            }

            if (GUILayout.Button("Recover Styles to New Asset..."))
            {
                RecoverStyles();
            }

            if (GUILayout.Button("Manage Sub-assets..."))
            {
                LoogaDesignSubAssetManagerWindow.Open((LoogaDesignSystem)target);
            }

            if (!string.IsNullOrEmpty(_synchronizationSummary))
            {
                EditorGUILayout.HelpBox(_synchronizationSummary, MessageType.Info);
            }

            if (!string.IsNullOrEmpty(_recoverySummary))
            {
                EditorGUILayout.HelpBox(_recoverySummary, MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply To Open Scenes"))
            {
                ApplyOpenSceneBindings();
            }
        }

        private void SynchronizeStyleLibrary()
        {
            LoogaDesignSystemConsolidator.SynchronizationReport report =
                LoogaDesignSystemConsolidator.Synchronize((LoogaDesignSystem)target);
            _synchronizationSummary =
                $"Registered {report.RegisteredStyles} style(s). " +
                $"Added {report.AddedStyles} and removed {report.RemovedEntries} null or duplicate entries.";
            serializedObject.Update();
        }

        private void RecoverStyles()
        {
            LoogaDesignSystem designSystem = (LoogaDesignSystem)target;
            if (!TryGetCleanSceneSetup(out SceneSetup[] sceneSetup))
            {
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Recover Design-System Styles",
                "This reloads every open scene, captures each UI element's serialized appearance, " +
                "and creates a new design-system asset. The current asset remains unchanged.",
                "Recover Styles",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            List<ElementStyleState> states = CaptureElementStyleStates(designSystem);
            try
            {
                SetElementStylesEnabled(states, false);
                AssetDatabase.SaveAssets();
                ReloadScenes(sceneSetup);

                string assetPath = AssetDatabase.GetAssetPath(designSystem);
                string outputFolder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                LoogaDesignSystemAdopter.AdoptionResult result =
                    LoogaDesignSystemAdopter.AdoptLoadedScenes(outputFolder);
                _recoverySummary =
                    $"Created {result.AssetPath}. Captured {result.Controls} control(s), " +
                    $"{result.Elements} element(s), and {result.PublicStyles + result.CapturedStyles} element style(s).";
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _recoverySummary = $"Recovery failed: {exception.Message}";
            }
            finally
            {
                SetElementStylesEnabled(states, true);
                AssetDatabase.SaveAssets();
            }
        }

        private static bool TryGetCleanSceneSetup(out SceneSetup[] sceneSetup)
        {
            sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            if (sceneSetup.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Recover Design-System Styles",
                    "Open the UI scenes that the new design system must capture.",
                    "OK");
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (string.IsNullOrWhiteSpace(scene.path) || scene.isDirty)
                {
                    EditorUtility.DisplayDialog(
                        "Recover Design-System Styles",
                        "Save every open scene before recovery. Recovery reloads serialized scene data.",
                        "OK");
                    return false;
                }
            }

            return true;
        }

        private static List<ElementStyleState> CaptureElementStyleStates(LoogaDesignSystem designSystem)
        {
            string assetPath = AssetDatabase.GetAssetPath(designSystem);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<ElementStyleState> states = new List<ElementStyleState>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not LoogaElementStyle style)
                {
                    continue;
                }

                SerializedObject serializedStyle = new SerializedObject(style);
                states.Add(new ElementStyleState(
                    style,
                    serializedStyle.FindProperty("_graphic._enabled").boolValue,
                    serializedStyle.FindProperty("_text._enabled").boolValue,
                    serializedStyle.FindProperty("_selectable._enabled").boolValue));
            }

            return states;
        }

        private static void SetElementStylesEnabled(
            IReadOnlyList<ElementStyleState> states,
            bool useCapturedValues)
        {
            for (int i = 0; i < states.Count; i++)
            {
                ElementStyleState state = states[i];
                if (state.Style == null)
                {
                    continue;
                }

                SerializedObject serializedStyle = new SerializedObject(state.Style);
                serializedStyle.FindProperty("_graphic._enabled").boolValue =
                    useCapturedValues && state.GraphicEnabled;
                serializedStyle.FindProperty("_text._enabled").boolValue =
                    useCapturedValues && state.TextEnabled;
                serializedStyle.FindProperty("_selectable._enabled").boolValue =
                    useCapturedValues && state.SelectableEnabled;
                serializedStyle.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(state.Style);
            }
        }

        private static void ReloadScenes(SceneSetup[] sceneSetup)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }

        private static void ApplyOpenSceneBindings()
        {
            LoogaStyleBinding[] bindings = Object.FindObjectsByType<LoogaStyleBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (LoogaStyleBinding binding in bindings)
            {
                binding.ApplyStyle();
            }

            int controlCount = 0;
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is not ILoogaStyledControl control)
                {
                    continue;
                }

                control.ApplyStyle();
                controlCount++;
            }

            SceneView.RepaintAll();
            Debug.Log($"Looga UI applied {bindings.Length} binding(s) and {controlCount} control style(s) in open scenes.");
        }

        private readonly struct ElementStyleState
        {
            internal ElementStyleState(
                LoogaElementStyle style,
                bool graphicEnabled,
                bool textEnabled,
                bool selectableEnabled)
            {
                Style = style;
                GraphicEnabled = graphicEnabled;
                TextEnabled = textEnabled;
                SelectableEnabled = selectableEnabled;
            }

            internal LoogaElementStyle Style { get; }
            internal bool GraphicEnabled { get; }
            internal bool TextEnabled { get; }
            internal bool SelectableEnabled { get; }
        }
    }

    [CustomEditor(typeof(LoogaStyleBinding))]
    [CanEditMultipleObjects]
    public sealed class LoogaStyleBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (!GUILayout.Button("Apply Style"))
            {
                return;
            }

            foreach (Object currentTarget in targets)
            {
                if (currentTarget is LoogaStyleBinding binding)
                {
                    binding.ApplyStyle();
                }
            }
        }
    }
}
