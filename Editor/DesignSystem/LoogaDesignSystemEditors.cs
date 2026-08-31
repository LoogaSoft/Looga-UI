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
            EditorGUILayout.LabelField("Style Library", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Color"))
                {
                    CreateStyle<LoogaColorToken>("Color");
                }

                if (GUILayout.Button("New Control Style"))
                {
                    CreateStyle<LoogaControlStyle>("Control Style");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Image"))
                {
                    CreateStyle<LoogaImageStyle>("Image Style");
                }

                if (GUILayout.Button("New Text"))
                {
                    CreateStyle<LoogaTextStyle>("Text Style");
                }
            }

            if (GUILayout.Button("Synchronize Style Library"))
            {
                SynchronizeStyleLibrary();
            }

            if (GUILayout.Button("Manage Design Sub-assets..."))
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

        private void CreateStyle<T>(string label) where T : LoogaDesignAsset
        {
            LoogaDesignSystem designSystem = (LoogaDesignSystem)target;
            string path = AssetDatabase.GetAssetPath(designSystem);
            T style = CreateInstance<T>();
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            string[] names = new string[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                names[i] = assets[i].name;
            }

            style.name = ObjectNames.GetUniqueName(
                names,
                $"New {label}");
            AssetDatabase.AddObjectToAsset(style, designSystem);
            Undo.RegisterCreatedObjectUndo(style, $"Create {label}");
            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            SynchronizeStyleLibrary();
            Selection.activeObject = style;
            EditorGUIUtility.PingObject(style);
        }

        private void SynchronizeStyleLibrary()
        {
            LoogaDesignSystemConsolidator.SynchronizationReport report =
                LoogaDesignSystemConsolidator.Synchronize((LoogaDesignSystem)target);
            _synchronizationSummary =
                $"Registered {report.RegisteredStyles} style asset(s). " +
                $"Added {report.AddedStyles}; removed {report.RemovedEntries} null, duplicate, or stale entries.";
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

            LoogaStyleBindingBase[] splitBindings = Object.FindObjectsByType<LoogaStyleBindingBase>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < splitBindings.Length; i++)
            {
                splitBindings[i].ApplyStyle();
            }

            SceneView.RepaintAll();
            Debug.Log($"Looga UI applied {bindings.Length + splitBindings.Length} style binding(s) in open scenes.");
        }
    }

    [CustomEditor(typeof(LoogaStyleBinding))]
    [CanEditMultipleObjects]
    public sealed class LoogaStyleBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _color;
        private SerializedProperty _control;
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
            _control = serializedObject.FindProperty("_control");
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

            EditorGUILayout.LabelField("Styles", EditorStyles.boldLabel);
            LoogaDesignAssetEditorGUI.DrawExposedReference(_color, "Color Only", typeof(LoogaColorToken));
            LoogaDesignAssetEditorGUI.DrawExposedReference(_control, "Control", typeof(LoogaControlStyle));
            LoogaDesignAssetEditorGUI.DrawExposedReference(_image, "Image", typeof(LoogaImageStyle));
            LoogaDesignAssetEditorGUI.DrawExposedReference(_text, "Text", typeof(LoogaTextStyle));

            if (!_color.hasMultipleDifferentValues &&
                !_image.hasMultipleDifferentValues &&
                _color.objectReferenceValue != null &&
                _image.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    "The Image style owns Image color. Color Only is ignored until the Image style is cleared.",
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
            if (!GUILayout.Button("Apply Styles"))
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

    [CustomEditor(typeof(LoogaStyleBindingBase), true)]
    [CanEditMultipleObjects]
    internal sealed class LoogaSplitStyleBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _style;

        private void OnEnable()
        {
            _style = serializedObject.FindProperty("_style");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LoogaDesignAssetEditorGUI.DrawExposedReference(
                _style,
                GetStyleLabel(),
                GetStyleType());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
            DrawTargets();
            serializedObject.ApplyModifiedProperties();

            if (target is LoogaControlStyleBinding controlBinding &&
                !controlBinding.HasCompatibleTarget)
            {
                EditorGUILayout.HelpBox(
                    "The target component does not match the control type selected by the style.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            if (!GUILayout.Button("Apply Style"))
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is LoogaStyleBindingBase binding)
                {
                    binding.ApplyStyle();
                }
            }
        }

        private string GetStyleLabel()
        {
            return target switch
            {
                LoogaColorBinding => "Color Only",
                LoogaImageStyleBinding => "Image Style",
                LoogaTextStyleBinding => "Text Style",
                LoogaControlStyleBinding => "Control Style",
                _ => "Style"
            };
        }

        private System.Type GetStyleType()
        {
            return target switch
            {
                LoogaColorBinding => typeof(LoogaColorToken),
                LoogaImageStyleBinding => typeof(LoogaImageStyle),
                LoogaTextStyleBinding => typeof(LoogaTextStyle),
                LoogaControlStyleBinding => typeof(LoogaControlStyle),
                _ => typeof(LoogaDesignAsset)
            };
        }

        private void DrawTargets()
        {
            if (target is LoogaControlStyleBinding)
            {
                DrawControlTargets();
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_resolveTargetAutomatically"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_target"));
        }

        private void DrawControlTargets()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_resolveTargetsAutomatically"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_controlTarget"));

            if (_style.hasMultipleDifferentValues ||
                _style.objectReferenceValue is not LoogaControlStyle controlStyle)
            {
                return;
            }

            switch (controlStyle.ControlType)
            {
                case LoogaControlType.Button:
                    DrawTarget("_buttonLabel");
                    DrawTarget("_buttonIcon");
                    break;
                case LoogaControlType.Dropdown:
                    DrawTarget("_dropdownArrow");
                    break;
                case LoogaControlType.InputField:
                    DrawTarget("_inputText");
                    DrawTarget("_inputPlaceholder");
                    break;
                case LoogaControlType.Scrollbar:
                    DrawTarget("_scrollbarHandle");
                    break;
                case LoogaControlType.Slider:
                    DrawTarget("_sliderFill");
                    DrawTarget("_sliderHandle");
                    break;
                case LoogaControlType.Toggle:
                    DrawTarget("_toggleCheckmark");
                    DrawTarget("_toggleLabel");
                    break;
            }
        }

        private void DrawTarget(string propertyName)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName));
        }
    }
}
