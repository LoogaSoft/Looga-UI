using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomPropertyDrawer(typeof(LoogaMenuCommand))]
    public sealed class LoogaMenuCommandDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty type = property.FindPropertyRelative("_type");
            LoogaMenuCommandType commandType = (LoogaMenuCommandType)type.enumValueIndex;
            int lineCount = commandType switch
            {
                LoogaMenuCommandType.OpenScreen => 4,
                LoogaMenuCommandType.SwitchLayout => 3,
                _ => 1
            };
            return EditorGUIUtility.singleLineHeight * lineCount
                + EditorGUIUtility.standardVerticalSpacing * (lineCount - 1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty type = property.FindPropertyRelative("_type");
            SerializedProperty target = property.FindPropertyRelative("_target");
            Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(row, type, new GUIContent("Action"));

            LoogaMenuCommandType commandType = (LoogaMenuCommandType)type.enumValueIndex;
            if (commandType is LoogaMenuCommandType.OpenScreen)
            {
                row.y += EditorGUIUtility.singleLineHeight
                    + EditorGUIUtility.standardVerticalSpacing;
                row.height = EditorGUI.GetPropertyHeight(target, true);
                EditorGUI.PropertyField(row, target, GUIContent.none, true);
            }
            else if (commandType is LoogaMenuCommandType.SwitchLayout)
            {
                DrawSwitchLayoutTarget(ref row, target);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawSwitchLayoutTarget(
            ref Rect row,
            SerializedProperty target)
        {
            SerializedProperty screen = target.FindPropertyRelative("_screen");
            SerializedProperty layout = target.FindPropertyRelative("_layout");
            MoveToNextRow(ref row);
            EditorGUI.PropertyField(row, screen, new GUIContent("Screen"));
            MoveToNextRow(ref row);

            LoogaMenuScreenDefinition selectedScreen =
                screen.objectReferenceValue as LoogaMenuScreenDefinition;
            using (new EditorGUI.DisabledScope(selectedScreen == null))
            {
                if (selectedScreen == null)
                {
                    EditorGUI.PropertyField(row, layout, new GUIContent("Layout"));
                    return;
                }

                int selectedIndex = 0;
                string[] names = new string[selectedScreen.Layouts.Length + 1];
                names[0] = "Default";
                for (int i = 0; i < selectedScreen.Layouts.Length; i++)
                {
                    LoogaMenuScreenLayout candidate = selectedScreen.Layouts[i];
                    names[i + 1] = candidate != null ? candidate.name : "Missing";
                    if (candidate == layout.objectReferenceValue)
                        selectedIndex = i + 1;
                }

                int nextIndex = EditorGUI.Popup(row, "Layout", selectedIndex, names);
                layout.objectReferenceValue = nextIndex > 0
                    ? selectedScreen.Layouts[nextIndex - 1]
                    : null;
            }
        }

        private static void MoveToNextRow(ref Rect row)
        {
            row.y += EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
