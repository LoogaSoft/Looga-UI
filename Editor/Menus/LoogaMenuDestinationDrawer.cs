using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomPropertyDrawer(typeof(LoogaMenuDestination))]
    public sealed class LoogaMenuDestinationDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3f + EditorGUIUtility.standardVerticalSpacing * 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty screen = property.FindPropertyRelative("_screen");
            SerializedProperty layout = property.FindPropertyRelative("_layout");
            SerializedProperty openMode = property.FindPropertyRelative("_openMode");

            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(line, screen);
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DrawLayout(line, screen.objectReferenceValue as LoogaMenuScreenDefinition, layout);
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, openMode);
            EditorGUI.EndProperty();
        }

        private static void DrawLayout(Rect rect, LoogaMenuScreenDefinition screen, SerializedProperty layout)
        {
            LoogaMenuScreenLayout[] layouts = screen != null ? screen.Layouts : null;
            if (layouts == null || layouts.Length == 0)
            {
                layout.objectReferenceValue = null;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.ObjectField(rect, "Layout", null, typeof(LoogaMenuScreenLayout), false);
                return;
            }

            string[] labels = new string[layouts.Length + 1];
            labels[0] = screen.DefaultLayout != null
                ? $"Default ({screen.DefaultLayout.DisplayName})"
                : "Default";
            int selectedIndex = 0;
            for (int i = 0; i < layouts.Length; i++)
            {
                LoogaMenuScreenLayout candidate = layouts[i];
                labels[i + 1] = candidate != null ? candidate.DisplayName : "Missing Layout";
                if (layout.objectReferenceValue == candidate)
                    selectedIndex = i + 1;
            }

            int nextIndex = EditorGUI.Popup(rect, "Layout", selectedIndex, labels);
            layout.objectReferenceValue = nextIndex == 0 ? null : layouts[nextIndex - 1];
        }
    }
}
