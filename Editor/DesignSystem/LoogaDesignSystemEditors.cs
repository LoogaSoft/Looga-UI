using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
            EditorGUILayout.LabelField("Token Library", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Color"))
                {
                    CreateToken<LoogaColorToken>("Color");
                }

                if (GUILayout.Button("New Button Colors"))
                {
                    CreateToken<LoogaButtonColorToken>("Button Colors");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Image"))
                {
                    CreateToken<LoogaImageToken>("Image");
                }

                if (GUILayout.Button("New Text"))
                {
                    CreateToken<LoogaTextToken>("Text");
                }
            }

            if (GUILayout.Button("Synchronize Token Library"))
            {
                SynchronizeTokenLibrary();
            }

            if (GUILayout.Button("Manage Token Sub-assets..."))
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

        private void CreateToken<T>(string label) where T : LoogaDesignAsset
        {
            LoogaDesignSystem designSystem = (LoogaDesignSystem)target;
            string path = AssetDatabase.GetAssetPath(designSystem);
            T token = CreateInstance<T>();
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            string[] names = new string[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                names[i] = assets[i].name;
            }

            token.name = ObjectNames.GetUniqueName(
                names,
                $"New {label}");
            AssetDatabase.AddObjectToAsset(token, designSystem);
            Undo.RegisterCreatedObjectUndo(token, $"Create {label} Token");
            EditorUtility.SetDirty(token);
            AssetDatabase.SaveAssets();
            SynchronizeTokenLibrary();
            Selection.activeObject = token;
            EditorGUIUtility.PingObject(token);
        }

        private void SynchronizeTokenLibrary()
        {
            LoogaDesignSystemConsolidator.SynchronizationReport report =
                LoogaDesignSystemConsolidator.Synchronize((LoogaDesignSystem)target);
            _synchronizationSummary =
                $"Registered {report.RegisteredTokens} token(s). " +
                $"Added {report.AddedTokens}; removed {report.RemovedEntries} null, duplicate, or stale entries.";
            serializedObject.Update();
        }

        private static void ApplyOpenSceneBindings()
        {
            LoogaStyleBinding[] bindings = Object.FindObjectsByType<LoogaStyleBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i].ApplyStyle();
            }

            SceneView.RepaintAll();
            Debug.Log($"Looga UI applied {bindings.Length} token binding(s) in open scenes.");
        }
    }

    [CustomEditor(typeof(LoogaStyleBinding))]
    [CanEditMultipleObjects]
    public sealed class LoogaStyleBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            SerializedProperty color = serializedObject.FindProperty("_color");
            SerializedProperty image = serializedObject.FindProperty("_image");
            if (!color.hasMultipleDifferentValues &&
                !image.hasMultipleDifferentValues &&
                color.objectReferenceValue != null &&
                image.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    "The Image token owns Image color. Color Only is ignored until the Image token is cleared.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            if (!GUILayout.Button("Apply Tokens"))
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is LoogaStyleBinding binding)
                {
                    binding.ApplyStyle();
                }
            }
        }
    }
}
