using LoogaSoft.UI.Controls;
using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    [CustomEditor(typeof(LoogaDesignSystem))]
    public sealed class LoogaDesignSystemEditor : UnityEditor.Editor
    {
        private string _synchronizationSummary;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Design System Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Synchronize Style Library"))
            {
                SynchronizeStyleLibrary();
            }

            if (GUILayout.Button("Manage Sub-assets..."))
            {
                LoogaDesignSubAssetManagerWindow.Open((LoogaDesignSystem)target);
            }

            if (!string.IsNullOrEmpty(_synchronizationSummary))
            {
                EditorGUILayout.HelpBox(_synchronizationSummary, MessageType.Info);
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
