using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor
{
    [CustomEditor(typeof(LoogaLayoutElement))]
    [CanEditMultipleObjects]
    sealed class LoogaLayoutElementEditor : UnityEditor.Editor
    {
        SerializedProperty _ignoreLayout;
        SerializedProperty _overrideMinWidth;
        SerializedProperty _minWidth;
        SerializedProperty _overridePreferredWidth;
        SerializedProperty _preferredWidth;
        SerializedProperty _useMaxWidth;
        SerializedProperty _maxWidth;
        SerializedProperty _overrideFlexibleWidth;
        SerializedProperty _flexibleWidth;
        SerializedProperty _overrideMinHeight;
        SerializedProperty _minHeight;
        SerializedProperty _overridePreferredHeight;
        SerializedProperty _preferredHeight;
        SerializedProperty _useMaxHeight;
        SerializedProperty _maxHeight;
        SerializedProperty _overrideFlexibleHeight;
        SerializedProperty _flexibleHeight;
        SerializedProperty _layoutPriority;

        void OnEnable()
        {
            _ignoreLayout = serializedObject.FindProperty("_ignoreLayout");
            _overrideMinWidth = serializedObject.FindProperty("_overrideMinWidth");
            _minWidth = serializedObject.FindProperty("_minWidth");
            _overridePreferredWidth = serializedObject.FindProperty("_overridePreferredWidth");
            _preferredWidth = serializedObject.FindProperty("_preferredWidth");
            _useMaxWidth = serializedObject.FindProperty("_useMaxWidth");
            _maxWidth = serializedObject.FindProperty("_maxWidth");
            _overrideFlexibleWidth = serializedObject.FindProperty("_overrideFlexibleWidth");
            _flexibleWidth = serializedObject.FindProperty("_flexibleWidth");
            _overrideMinHeight = serializedObject.FindProperty("_overrideMinHeight");
            _minHeight = serializedObject.FindProperty("_minHeight");
            _overridePreferredHeight = serializedObject.FindProperty("_overridePreferredHeight");
            _preferredHeight = serializedObject.FindProperty("_preferredHeight");
            _useMaxHeight = serializedObject.FindProperty("_useMaxHeight");
            _maxHeight = serializedObject.FindProperty("_maxHeight");
            _overrideFlexibleHeight = serializedObject.FindProperty("_overrideFlexibleHeight");
            _flexibleHeight = serializedObject.FindProperty("_flexibleHeight");
            _layoutPriority = serializedObject.FindProperty("_layoutPriority");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScript();
            EditorGUILayout.PropertyField(_ignoreLayout);

            bool constraintsDisabled = !_ignoreLayout.hasMultipleDifferentValues && _ignoreLayout.boolValue;
            using (new EditorGUI.DisabledScope(constraintsDisabled))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Width", EditorStyles.boldLabel);
                DrawConstraint("Minimum", _overrideMinWidth, _minWidth);
                DrawConstraint("Preferred", _overridePreferredWidth, _preferredWidth);
                DrawConstraint("Maximum", _useMaxWidth, _maxWidth);
                DrawConstraint("Grow", _overrideFlexibleWidth, _flexibleWidth);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Height", EditorStyles.boldLabel);
                DrawConstraint("Minimum", _overrideMinHeight, _minHeight);
                DrawConstraint("Preferred", _overridePreferredHeight, _preferredHeight);
                DrawConstraint("Maximum", _useMaxHeight, _maxHeight);
                DrawConstraint("Grow", _overrideFlexibleHeight, _flexibleHeight);

                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(_layoutPriority);
            }

            serializedObject.ApplyModifiedProperties();
            DrawValidation();
        }

        static void DrawConstraint(
            string label,
            SerializedProperty enabled,
            SerializedProperty value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.showMixedValue = enabled.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayout.ToggleLeft(
                    label,
                    enabled.boolValue,
                    GUILayout.Width(EditorGUIUtility.labelWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    enabled.boolValue = newValue;
                }

                EditorGUI.showMixedValue = false;

                bool valueDisabled = !enabled.hasMultipleDifferentValues && !enabled.boolValue;
                using (new EditorGUI.DisabledScope(valueDisabled))
                {
                    EditorGUILayout.PropertyField(value, GUIContent.none);
                }
            }
        }

        void DrawValidation()
        {
            foreach (Object inspectedTarget in targets)
            {
                LoogaLayoutElement element = (LoogaLayoutElement)inspectedTarget;
                if (element.TryGetComponent(out LayoutElement _))
                {
                    EditorGUILayout.HelpBox(
                        "Unity Layout Element is also present on one or more selected objects. Remove one component so layout priorities and overrides remain unambiguous.",
                        MessageType.Warning);
                    return;
                }
            }
        }

        void DrawScript()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour((LoogaLayoutElement)target),
                    typeof(MonoScript),
                    false);
            }
        }
    }
}
