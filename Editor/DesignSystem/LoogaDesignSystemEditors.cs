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
        private SerializedProperty _color;
        private SerializedProperty _buttonColors;
        private SerializedProperty _image;
        private SerializedProperty _text;
        private SerializedProperty _resolveTargetsAutomatically;
        private SerializedProperty _graphicTarget;
        private SerializedProperty _buttonTarget;
        private SerializedProperty _imageTarget;
        private SerializedProperty _textTarget;

        private void OnEnable()
        {
            _color = serializedObject.FindProperty("_color");
            _buttonColors = serializedObject.FindProperty("_buttonColors");
            _image = serializedObject.FindProperty("_image");
            _text = serializedObject.FindProperty("_text");
            _resolveTargetsAutomatically = serializedObject.FindProperty("_resolveTargetsAutomatically");
            _graphicTarget = serializedObject.FindProperty("_graphicTarget");
            _buttonTarget = serializedObject.FindProperty("_buttonTarget");
            _imageTarget = serializedObject.FindProperty("_imageTarget");
            _textTarget = serializedObject.FindProperty("_textTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Tokens", EditorStyles.boldLabel);
            DrawExposedToken(_color, "Color Only", typeof(LoogaColorToken));
            DrawExposedToken(_buttonColors, "Button Colors", typeof(LoogaButtonColorToken));
            DrawExposedToken(_image, "Image", typeof(LoogaImageToken));
            DrawExposedToken(_text, "Text", typeof(LoogaTextToken));

            if (!_color.hasMultipleDifferentValues &&
                !_image.hasMultipleDifferentValues &&
                _color.objectReferenceValue != null &&
                _image.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    "The Image token owns Image color. Color Only is ignored until the Image token is cleared.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_resolveTargetsAutomatically);
            EditorGUILayout.PropertyField(_graphicTarget);
            EditorGUILayout.PropertyField(_buttonTarget);
            EditorGUILayout.PropertyField(_imageTarget);
            EditorGUILayout.PropertyField(_textTarget);
            serializedObject.ApplyModifiedProperties();

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

        private void DrawExposedToken(SerializedProperty property, string label, System.Type assetType)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    property.isExpanded = GUILayout.Toggle(
                        property.isExpanded,
                        label,
                        EditorStyles.foldout,
                        GUILayout.Width(112f));

                    EditorGUI.BeginChangeCheck();
                    Object value = EditorGUILayout.ObjectField(
                        property.objectReferenceValue,
                        assetType,
                        false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.objectReferenceValue = value;
                    }

                    bool multipleTargets = serializedObject.isEditingMultipleObjects;
                    using (new EditorGUI.DisabledScope(multipleTargets))
                    {
                        if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(38f)))
                        {
                            LoogaDesignAssetReferenceDrawer.CreateAndAssign(property, assetType, null);
                            GUIUtility.ExitGUI();
                        }
                    }

                    bool canClone = !multipleTargets && property.objectReferenceValue is LoogaDesignAsset;
                    using (new EditorGUI.DisabledScope(!canClone))
                    {
                        if (GUILayout.Button("Clone", EditorStyles.miniButton, GUILayout.Width(45f)))
                        {
                            LoogaDesignAssetReferenceDrawer.CreateAndAssign(
                                property,
                                assetType,
                                property.objectReferenceValue as LoogaDesignAsset);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                if (property.isExpanded &&
                    !property.hasMultipleDifferentValues &&
                    property.objectReferenceValue != null)
                {
                    DrawInlineAsset(property.objectReferenceValue);
                }
            }
        }

        private static void DrawInlineAsset(Object asset)
        {
            SerializedObject nestedObject = new(asset);
            nestedObject.Update();
            SerializedProperty iterator = nestedObject.GetIterator();
            bool enterChildren = true;

            EditorGUI.indentLevel++;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }
            EditorGUI.indentLevel--;

            if (nestedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
