using System;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenLayout))]
    public sealed class LoogaMenuScreenLayoutEditor : LoogaEditor
    {
        private static readonly string[] InspectorTabs = { "Layout", "Shared UI" };

        private int _selectedTab;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = LoogaUiToolkitStyle.CreateInspectorRoot();
            VisualElement content = new();
            root.Add(LoogaUiToolkitStyle.CreateTabBar(
                InspectorTabs,
                _selectedTab,
                index =>
                {
                    _selectedTab = index;
                    BuildRetainedTab(content);
                }));
            root.Add(content);
            BuildRetainedTab(content);
            root.Bind(serializedObject);
            return root;
        }

        private void BuildRetainedTab(VisualElement content)
        {
            content.Clear();
            content.style.marginTop = 6f;
            if (_selectedTab == 1)
            {
                AddProperty(content, "_regionOverrides", "Region Overrides");
                return;
            }

            AddProperty(content, "_description");
            AddProperty(content, "_panels");
            AddProperty(content, "_includeInNavigation");
            AddProperty(content, "_navigationRequirements");
        }

        private void AddProperty(VisualElement parent, string propertyName, string label = null)
        {
            PropertyField field = LoogaUiToolkitStyle.CreatePropertyField(
                serializedObject,
                propertyName,
                label);
            if (field != null)
                parent.Add(field);
        }

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
