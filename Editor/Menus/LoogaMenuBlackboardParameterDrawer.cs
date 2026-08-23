using LoogaSoft.Blackboard;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomPropertyDrawer(typeof(LoogaMenuBlackboardParameter))]
    public sealed class LoogaMenuBlackboardParameterDrawer : PropertyDrawer
    {
        private const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty key = property.FindPropertyRelative("_key");
            Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            float keyWidth = Mathf.Max(120f, row.width * 0.52f);
            Rect keyRect = new(row.x, row.y, keyWidth, row.height);
            Rect valueRect = new(keyRect.xMax + Gap, row.y, row.width - keyWidth - Gap, row.height);

            EditorGUI.PropertyField(keyRect, key, GUIContent.none);
            DrawValueField(valueRect, property, key.objectReferenceValue as LoogaBlackboardKey);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static void DrawValueField(Rect rect, SerializedProperty property, LoogaBlackboardKey key)
        {
            if (key == null)
            {
                EditorGUI.LabelField(rect, "No Key");
                return;
            }

            SerializedProperty value = key.ValueType switch
            {
                LoogaBlackboardValueType.Bool => property.FindPropertyRelative("_boolValue"),
                LoogaBlackboardValueType.Int => property.FindPropertyRelative("_intValue"),
                LoogaBlackboardValueType.Float => property.FindPropertyRelative("_floatValue"),
                LoogaBlackboardValueType.String => property.FindPropertyRelative("_stringValue"),
                _ => null
            };

            if (value != null)
            {
                EditorGUI.PropertyField(rect, value, GUIContent.none);
            }
        }
    }
}
