using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor
{
    [CustomEditor(typeof(LoogaContentFitter))]
    sealed class LoogaContentFitterEditor : UnityEditor.Editor
    {
        SerializedProperty _contentSource;
        SerializedProperty _assignedContent;
        SerializedProperty _width;
        SerializedProperty _height;
        SerializedProperty _minimumSize;
        SerializedProperty _maximumSize;
        SerializedProperty _layoutPriority;

        void OnEnable()
        {
            _contentSource = serializedObject.FindProperty("_contentSource");
            _assignedContent = serializedObject.FindProperty("_assignedContent");
            _width = serializedObject.FindProperty("_width");
            _height = serializedObject.FindProperty("_height");
            _minimumSize = serializedObject.FindProperty("_minimumSize");
            _maximumSize = serializedObject.FindProperty("_maximumSize");
            _layoutPriority = serializedObject.FindProperty("_layoutPriority");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScript();

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Content", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_contentSource);

            if ((LoogaContentSource)_contentSource.enumValueIndex == LoogaContentSource.Assigned)
            {
                EditorGUILayout.PropertyField(_assignedContent);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Sizing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_width);
            EditorGUILayout.PropertyField(_height);

            if (UsesClamping())
            {
                EditorGUILayout.PropertyField(_minimumSize);
                EditorGUILayout.PropertyField(_maximumSize);
            }

            EditorGUILayout.PropertyField(_layoutPriority);
            serializedObject.ApplyModifiedProperties();

            DrawValidation();
        }

        void DrawValidation()
        {
            LoogaContentFitter fitter = (LoogaContentFitter)target;

            if (fitter.TryGetComponent(out ContentSizeFitter _))
            {
                EditorGUILayout.HelpBox(
                    "Remove Unity Content Size Fitter. Both components would attempt to size the same RectTransform.",
                    MessageType.Error);
            }

            if (fitter.TryGetComponent(out LoogaLayout _))
            {
                EditorGUILayout.HelpBox(
                    "Looga Layout already supports content sizing. A separate Looga Content Fitter is unnecessary here.",
                    MessageType.Error);
            }

            if ((LoogaContentSource)_contentSource.enumValueIndex == LoogaContentSource.Assigned
                && _assignedContent.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign the RectTransform that provides this object's content size.", MessageType.Warning);
            }
        }

        void DrawScript()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour((LoogaContentFitter)target),
                    typeof(MonoScript),
                    false);
            }
        }

        bool UsesClamping()
        {
            return (LoogaContentFitMode)_width.enumValueIndex == LoogaContentFitMode.ClampedPreferred
                || (LoogaContentFitMode)_height.enumValueIndex == LoogaContentFitMode.ClampedPreferred;
        }
    }
}
