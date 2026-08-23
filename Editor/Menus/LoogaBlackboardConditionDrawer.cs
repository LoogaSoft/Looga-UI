using LoogaSoft.Blackboard;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomPropertyDrawer(typeof(LoogaBlackboardCondition))]
    public sealed class LoogaBlackboardConditionDrawer : PropertyDrawer
    {
        private const float Gap = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect firstLine = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect secondLine = new(position.x, firstLine.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUIUtility.singleLineHeight);

            SerializedProperty key = property.FindPropertyRelative("_key");
            SerializedProperty comparison = property.FindPropertyRelative("_comparison");
            SerializedProperty failureReason = property.FindPropertyRelative("_failureReason");

            float keyWidth = firstLine.width * 0.45f;
            float comparisonWidth = firstLine.width * 0.25f;
            float valueWidth = firstLine.width - keyWidth - comparisonWidth - Gap * 2f;

            Rect keyRect = new(firstLine.x, firstLine.y, keyWidth, firstLine.height);
            Rect comparisonRect = new(keyRect.xMax + Gap, firstLine.y, comparisonWidth, firstLine.height);
            Rect valueRect = new(comparisonRect.xMax + Gap, firstLine.y, valueWidth, firstLine.height);

            EditorGUI.PropertyField(keyRect, key, GUIContent.none);
            EditorGUI.PropertyField(comparisonRect, comparison, GUIContent.none);
            DrawExpectedValue(valueRect, property, key.objectReferenceValue as LoogaBlackboardKey);
            EditorGUI.PropertyField(secondLine, failureReason);

            EditorGUI.EndProperty();
        }

        private static void DrawExpectedValue(Rect rect, SerializedProperty property, LoogaBlackboardKey key)
        {
            if (key == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.TextField(rect, "Select Key");
                }

                return;
            }

            SerializedProperty value = key.ValueType switch
            {
                LoogaBlackboardValueType.Bool => property.FindPropertyRelative("_expectedBool"),
                LoogaBlackboardValueType.Int => property.FindPropertyRelative("_expectedInt"),
                LoogaBlackboardValueType.Float => property.FindPropertyRelative("_expectedFloat"),
                LoogaBlackboardValueType.String => property.FindPropertyRelative("_expectedString"),
                _ => null
            };

            if (value != null)
            {
                EditorGUI.PropertyField(rect, value, GUIContent.none);
            }
        }
    }
}
