using System;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenLayout))]
    public sealed class LoogaMenuScreenLayoutEditor : LoogaEditor
    {
        private static readonly string[] InspectorTabs = { "Layout", "Shared UI" };

        private int _selectedTab;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            _selectedTab = DrawBody(
                serializedObject,
                _selectedTab,
                $"{nameof(LoogaMenuScreenLayoutEditor)}_{target.GetInstanceID()}",
                propertyName => DrawLoogaProperty(propertyName));

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the editable layout fields without creating a nested editor instance.
        /// Screen inspectors use this method for their selected-layout detail view.
        /// </summary>
        internal static int DrawBody(
            SerializedObject layoutObject,
            int selectedTab,
            string controlId,
            Action<string> drawProperty = null)
        {
            selectedTab = LoogaGUILayout.Tabs(selectedTab, InspectorTabs, controlId);
            if (selectedTab == 1)
            {
                LoogaMenuScreenAuthoringGUI.DrawRegions(layoutObject.FindProperty("_regionOverrides"));
                return selectedTab;
            }

            if (drawProperty != null)
            {
                drawProperty("_description");
                drawProperty("_panels");
                drawProperty("_includeInNavigation");
                drawProperty("_navigationRequirements");
            }
            else
            {
                LoogaGUILayout.PropertyField(layoutObject.FindProperty("_description"));
                LoogaGUILayout.PropertyField(layoutObject.FindProperty("_panels"), true);
                LoogaGUILayout.PropertyField(layoutObject.FindProperty("_includeInNavigation"));
                LoogaGUILayout.PropertyField(layoutObject.FindProperty("_navigationRequirements"));
            }

            return selectedTab;
        }
    }
}
