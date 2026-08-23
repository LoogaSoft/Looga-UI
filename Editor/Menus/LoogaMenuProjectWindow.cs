using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    /// <summary>Provides one entry point for the framework's four authoring concepts.</summary>
    public sealed class LoogaMenuProjectWindow : EditorWindow
    {
        private static readonly string[] Tabs = { "Screens", "Panels", "Shared UI", "Advanced" };

        private Vector2 _scroll;
        private int _selectedTab;

        [MenuItem("LoogaSoft/UI/Menus/Menu Project", priority = 23)]
        private static void Open()
        {
            GetWindow<LoogaMenuProjectWindow>("Menu Project");
        }

        private void OnGUI()
        {
            _selectedTab = LoogaGUILayout.Tabs(
                _selectedTab,
                Tabs,
                nameof(LoogaMenuProjectWindow));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(6f);
            switch (_selectedTab)
            {
                case 1:
                    DrawAssetList<LoogaMenuPanelDefinition>("Panel", CreatePanel);
                    break;
                case 2:
                    DrawSharedUi();
                    break;
                case 3:
                    DrawAdvanced();
                    break;
                default:
                    DrawAssetList<LoogaMenuScreenDefinition>("Screen", CreateScreen);
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawAssetList<T>(string label, Action create) where T : UnityEngine.Object
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{label}s", EditorStyles.boldLabel);
                if (GUILayout.Button($"Create {label}", GUILayout.Width(104f)))
                    create();
            }

            List<T> assets = FindAssets<T>();
            if (assets.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No {label.ToLowerInvariant()} assets exist yet.",
                    MessageType.Info);
                return;
            }

            foreach (T asset in assets)
                DrawAssetRow(asset);
        }

        private static void DrawSharedUi()
        {
            LoogaMenuStructureProfile project = LoogaMenuStructureEditorUtility.FindStructure();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Shared UI Slots", EditorStyles.boldLabel);
                if (project == null)
                {
                    if (GUILayout.Button("Create Menu Project", GUILayout.Width(132f)))
                        CreateMenuProject();
                }
                else if (GUILayout.Button("Edit Slots", GUILayout.Width(80f)))
                {
                    Select(project);
                }
            }

            if (project == null)
            {
                EditorGUILayout.HelpBox(
                    "Create one Menu Project. It owns shared slots such as Header, Navigation, Actions, and Background.",
                    MessageType.Info);
                return;
            }

            DrawAssetRow(project);
            EditorGUILayout.Space(4f);
            foreach (LoogaMenuRegionDefinition slot in project.Regions)
            {
                if (slot == null)
                    continue;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(slot.DisplayName);
                    EditorGUILayout.LabelField(
                        GetSlotTypeName(slot),
                        EditorStyles.miniLabel,
                        GUILayout.Width(84f));
                }
            }
        }

        private static void DrawAdvanced()
        {
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Contexts and input policies are optional. Start with screens, layouts, panels, and shared UI slots.",
                MessageType.Info);
            DrawAssetGroup<LoogaMenuContextDefinition>("Contexts");
            DrawAssetGroup<LoogaMenuInputPolicy>("Input Policies");
            DrawAssetGroup<LoogaMenuRuleSet>("Rule Sets");
        }

        private static void DrawAssetGroup<T>(string label) where T : UnityEngine.Object
        {
            List<T> assets = FindAssets<T>();
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"{label} ({assets.Count})", EditorStyles.boldLabel);
            foreach (T asset in assets)
                DrawAssetRow(asset);
        }

        private static void DrawAssetRow(UnityEngine.Object asset)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(asset != null ? asset.name : "Missing Asset");
                using (new EditorGUI.DisabledScope(asset == null))
                {
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(52f)))
                        Select(asset);
                }
            }
        }

        private static List<T> FindAssets<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> assets = new(guids.Length);
            foreach (string guid in guids)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    assets.Add(asset);
            }

            assets.Sort((left, right) => string.Compare(
                left.name,
                right.name,
                StringComparison.OrdinalIgnoreCase));
            return assets;
        }

        private static void CreateScreen()
        {
            string path = PromptForAssetPath("New Menu Screen", "New Menu Screen");
            if (string.IsNullOrWhiteSpace(path))
                return;

            LoogaMenuScreenDefinition screen = CreateInstance<LoogaMenuScreenDefinition>();
            LoogaMenuScreenLayout layout = CreateInstance<LoogaMenuScreenLayout>();
            screen.name = System.IO.Path.GetFileNameWithoutExtension(path);
            layout.name = "Default";
            AssetDatabase.CreateAsset(screen, path);
            AssetDatabase.AddObjectToAsset(layout, screen);

            SerializedObject screenObject = new(screen);
            SerializedProperty layouts = screenObject.FindProperty("_layouts");
            layouts.arraySize = 1;
            layouts.GetArrayElementAtIndex(0).objectReferenceValue = layout;
            screenObject.FindProperty("_defaultLayout").objectReferenceValue = layout;
            screenObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            Select(screen);
        }

        private static void CreatePanel()
        {
            string path = PromptForAssetPath("New Menu Panel", "New Menu Panel");
            if (string.IsNullOrWhiteSpace(path))
                return;

            LoogaMenuPanelDefinition panel = CreateInstance<LoogaMenuPanelDefinition>();
            panel.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(panel, path);
            AssetDatabase.SaveAssets();
            Select(panel);
        }

        private static void CreateMenuProject()
        {
            string path = PromptForAssetPath("New Menu Project", "Menu Project");
            if (string.IsNullOrWhiteSpace(path))
                return;

            LoogaMenuStructureProfile project = CreateInstance<LoogaMenuStructureProfile>();
            project.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(project, path);
            LoogaMenuRegionDefinition[] slots =
            {
                CreateDefaultSlot<LoogaMenuPanelRegionContent>(project, "Background"),
                CreateDefaultSlot<LoogaMenuPanelRegionContent>(project, "Header"),
                CreateDefaultSlot<LoogaMenuNavigationRegionContent>(project, "Navigation"),
                CreateDefaultSlot<LoogaMenuActionRegionContent>(project, "Actions")
            };

            SerializedObject projectObject = new(project);
            SerializedProperty regions = projectObject.FindProperty("_regions");
            regions.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                regions.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            projectObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Select(project);
        }

        private static LoogaMenuRegionDefinition CreateDefaultSlot<T>(
            LoogaMenuStructureProfile project,
            string name)
            where T : LoogaMenuRegionContent
        {
            LoogaMenuRegionDefinition slot = CreateInstance<LoogaMenuRegionDefinition>();
            T content = CreateInstance<T>();
            slot.name = name;
            content.name = name;
            AssetDatabase.AddObjectToAsset(slot, project);
            AssetDatabase.AddObjectToAsset(content, project);

            SerializedObject slotObject = new(slot);
            slotObject.FindProperty("_defaultContent").objectReferenceValue = content;
            slotObject.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static string PromptForAssetPath(string title, string defaultName)
        {
            return EditorUtility.SaveFilePanelInProject(
                title,
                defaultName,
                "asset",
                $"Choose where to save {defaultName}.");
        }

        private static void Select(UnityEngine.Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static string GetSlotTypeName(LoogaMenuRegionDefinition slot)
        {
            Type type = slot.ContentType;
            if (typeof(LoogaMenuNavigationRegionContent).IsAssignableFrom(type))
                return "Navigation";
            if (typeof(LoogaMenuActionRegionContent).IsAssignableFrom(type))
                return "Actions";
            return "Panels";
        }
    }
}
