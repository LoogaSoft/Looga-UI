using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuScreenLayoutPopupUtility
    {
        public static void Draw(LoogaMenuScreenDefinition screen, SerializedProperty layoutProperty)
        {
            if (layoutProperty == null)
                return;

            LoogaMenuScreenLayout[] layouts = screen != null ? screen.Layouts : null;
            if (layouts == null || layouts.Length == 0)
            {
                layoutProperty.objectReferenceValue = null;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Layout", null, typeof(LoogaMenuScreenLayout), false);
                return;
            }

            string[] labels = new string[layouts.Length + 1];
            LoogaMenuScreenLayout defaultLayout = screen.DefaultLayout;
            labels[0] = defaultLayout != null ? $"Default ({defaultLayout.DisplayName})" : "Default";
            int selectedIndex = 0;

            for (int i = 0; i < layouts.Length; i++)
            {
                LoogaMenuScreenLayout layout = layouts[i];
                labels[i + 1] = layout != null ? layout.DisplayName : "Missing Layout";
                if (layoutProperty.objectReferenceValue == layout)
                    selectedIndex = i + 1;
            }

            int nextIndex = EditorGUILayout.Popup("Layout", selectedIndex, labels);
            layoutProperty.objectReferenceValue = nextIndex == 0 ? null : layouts[nextIndex - 1];
        }
    }
}
