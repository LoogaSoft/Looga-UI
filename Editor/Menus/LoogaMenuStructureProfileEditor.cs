using System;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuStructureProfile))]
    public sealed class LoogaMenuStructureProfileEditor : LoogaEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = LoogaUiToolkitStyle.CreateInspectorRoot();
            PropertyField regions = LoogaUiToolkitStyle.CreatePropertyField(
                serializedObject,
                "_regions",
                "Regions");
            if (regions != null)
                root.Add(regions);

            Button addPanel = new(() => CreateRegionRetained<LoogaMenuPanelRegionContent>(
                root,
                "Shared Panel"))
            {
                text = "Add Panel Slot"
            };
            Button addNavigation = new(() =>
                CreateRegionRetained<LoogaMenuNavigationRegionContent>(root, "Navigation"))
            {
                text = "Add Navigation Slot"
            };
            Button addActions = new(() => CreateRegionRetained<LoogaMenuActionRegionContent>(
                root,
                "Actions"))
            {
                text = "Add Action Slot"
            };
            root.Add(LoogaUiToolkitStyle.CreateButtonRow(addPanel, addNavigation, addActions));
            root.Bind(serializedObject);
            return root;
        }

        private void CreateRegionRetained<T>(VisualElement root, string baseName)
            where T : LoogaMenuRegionContent
        {
            serializedObject.UpdateIfRequiredOrScript();
            CreateRegion<T>(serializedObject.FindProperty("_regions"), baseName);
            root.Clear();
            root.Add(CreateInspectorGUI());
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            SerializedProperty regions = serializedObject.FindProperty("_regions");
            for (int i = 0; i < regions.arraySize; i++)
            {
                LoogaMenuRegionDefinition region = regions.GetArrayElementAtIndex(i).objectReferenceValue
                    as LoogaMenuRegionDefinition;
                DrawRegion(regions, i, region);
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Panel Slot"))
                    CreateRegion<LoogaMenuPanelRegionContent>(regions, "Shared Panel");
                if (GUILayout.Button("Add Navigation Slot"))
                    CreateRegion<LoogaMenuNavigationRegionContent>(regions, "Navigation");
                if (GUILayout.Button("Add Action Slot"))
                    CreateRegion<LoogaMenuActionRegionContent>(regions, "Actions");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRegion(SerializedProperty regions, int index, LoogaMenuRegionDefinition region)
        {
            if (region == null)
            {
                LoogaGUILayout.PropertyField(regions.GetArrayElementAtIndex(index));
                return;
            }

            SerializedObject regionObject = new(region);
            SerializedProperty content = regionObject.FindProperty("_defaultContent");
            regionObject.UpdateIfRequiredOrScript();

            content.isExpanded = LoogaGUILayout.Foldout(
                new GUIContent(region.DisplayName),
                content.isExpanded,
                () =>
                {
                    EditorGUI.BeginChangeCheck();
                    string nextName = EditorGUILayout.DelayedTextField("Name", region.name);
                    if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(nextName))
                    {
                        Undo.RecordObject(region, "Rename Menu Region");
                        region.name = nextName.Trim();
                        if (region.DefaultContent != null)
                            region.DefaultContent.name = region.name;
                        EditorUtility.SetDirty(region);
                    }

                    LoogaMenuScreenAuthoringGUI.DrawContent(
                        content.objectReferenceValue as LoogaMenuRegionContent);

                    if (GUILayout.Button("Remove Region"))
                    {
                        RemoveRegion(regions, index, region);
                        GUIUtility.ExitGUI();
                    }
                });

            regionObject.ApplyModifiedProperties();
        }

        private void CreateRegion<T>(SerializedProperty regions, string baseName)
            where T : LoogaMenuRegionContent
        {
            LoogaMenuStructureProfile profile = (LoogaMenuStructureProfile)target;
            string path = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrWhiteSpace(path))
                return;

            LoogaMenuRegionDefinition region = CreateInstance<LoogaMenuRegionDefinition>();
            region.name = ObjectNames.GetUniqueName(
                Array.ConvertAll(
                    AssetDatabase.LoadAllAssetsAtPath(path),
                    asset => asset != null ? asset.name : string.Empty),
                baseName);
            T content = CreateInstance<T>();
            content.name = region.name;

            AssetDatabase.AddObjectToAsset(region, profile);
            AssetDatabase.AddObjectToAsset(content, profile);
            Undo.RegisterCreatedObjectUndo(region, "Create Menu Region");
            Undo.RegisterCreatedObjectUndo(content, "Create Menu Region Content");

            SerializedObject regionObject = new(region);
            regionObject.FindProperty("_defaultContent").objectReferenceValue = content;
            regionObject.ApplyModifiedPropertiesWithoutUndo();

            int index = regions.arraySize;
            regions.InsertArrayElementAtIndex(index);
            regions.GetArrayElementAtIndex(index).objectReferenceValue = region;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        private static void RemoveRegion(
            SerializedProperty regions,
            int index,
            LoogaMenuRegionDefinition region)
        {
            LoogaMenuRegionContent content = region.DefaultContent;
            regions.GetArrayElementAtIndex(index).objectReferenceValue = null;
            regions.DeleteArrayElementAtIndex(index);
            regions.serializedObject.ApplyModifiedProperties();

            if (content != null && AssetDatabase.IsSubAsset(content))
                Undo.DestroyObjectImmediate(content);
            if (AssetDatabase.IsSubAsset(region))
                Undo.DestroyObjectImmediate(region);

            AssetDatabase.SaveAssets();
        }
    }
}
