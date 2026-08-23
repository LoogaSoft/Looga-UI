using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenDefinition))]
    public sealed class LoogaMenuScreenDefinitionEditor : LoogaEditor
    {
        private static readonly string[] InspectorTabs = { "Screen", "Layouts", "Shared UI" };

        private LoogaMenuScreenLayout _selectedLayout;
        private SerializedObject _selectedLayoutObject;
        private bool _selectedLayoutExpanded = true;
        private int _selectedLayoutTab;
        private int _selectedTab;
        private bool _navigationExpanded;
        private bool _behaviorExpanded;

        private void OnEnable()
        {
            SelectLayout(((LoogaMenuScreenDefinition)target)?.ResolveLayout(null));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            _selectedTab = LoogaGUILayout.Tabs(
                _selectedTab,
                InspectorTabs,
                $"{nameof(LoogaMenuScreenDefinitionEditor)}_{target.GetInstanceID()}");

            switch (_selectedTab)
            {
                case 1:
                    DrawLayouts((LoogaMenuScreenDefinition)target);
                    break;
                case 2:
                    DrawRegions();
                    break;
                default:
                    DrawSettings();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (_selectedTab == 0)
                DrawValidation((LoogaMenuScreenDefinition)target);
        }

        private void DrawSettings()
        {
            LoogaGUILayout.PropertyField(serializedObject.FindProperty("_description"));

            EditorGUILayout.Space(6f);
            _navigationExpanded = LoogaGUILayout.FoldoutLarge(
                new GUIContent(
                    "Navigation",
                    "Generate navigation directly from this screen instead of creating shared-content assets."),
                _navigationExpanded,
                () =>
                {
                    LoogaGUILayout.PropertyField(serializedObject.FindProperty("_navigationSlot"));
                    LoogaGUILayout.PropertyField(
                        serializedObject.FindProperty("_includeLayoutsInNavigation"));
                    LoogaGUILayout.PropertyField(
                        serializedObject.FindProperty("_navigationLinks"),
                        true);
                });

            _behaviorExpanded = LoogaGUILayout.FoldoutSmall(
                "Advanced Behavior",
                _behaviorExpanded,
                () =>
                {
                    LoogaGUILayout.PropertyField(serializedObject.FindProperty("_rules"));
                    LoogaGUILayout.PropertyField(serializedObject.FindProperty("_inputPolicy"));
                    LoogaGUILayout.PropertyField(serializedObject.FindProperty("_defaultOpenMode"));
                    LoogaGUILayout.PropertyField(serializedObject.FindProperty("_missingPanelBehavior"));
                });
        }

        private void DrawRegions()
        {
            LoogaMenuScreenAuthoringGUI.DrawRegions(
                serializedObject.FindProperty("_regionOverrides"));
        }

        private void DrawLayouts(LoogaMenuScreenDefinition screen)
        {
            SerializedProperty layouts = serializedObject.FindProperty("_layouts");
            SerializedProperty defaultLayout = serializedObject.FindProperty("_defaultLayout");

            if (_selectedLayout == null || !screen.ContainsLayout(_selectedLayout))
                SelectLayout(screen.ResolveLayout(null));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Layouts", EditorStyles.boldLabel);
            if (layouts.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Add a layout before opening this screen. A layout owns the screen's panel composition.",
                    MessageType.Info);
            }

            for (int i = 0; i < layouts.arraySize; i++)
            {
                SerializedProperty element = layouts.GetArrayElementAtIndex(i);
                LoogaMenuScreenLayout layout = element.objectReferenceValue as LoogaMenuScreenLayout;
                Rect rowRect = EditorGUILayout.GetControlRect(
                    false,
                    LoogaEditorTabs.SegmentedButtonHeight);
                const float defaultButtonWidth = 86f;
                const float duplicateButtonWidth = 66f;
                const float removeButtonWidth = 22f;
                const float minimumLayoutButtonWidth = 80f;
                const float fixedButtonWidth = defaultButtonWidth
                    + duplicateButtonWidth
                    + removeButtonWidth;
                float fixedButtonScale = Mathf.Min(
                    1f,
                    Mathf.Max(0f, rowRect.width - minimumLayoutButtonWidth) / fixedButtonWidth);
                float scaledDefaultWidth = defaultButtonWidth * fixedButtonScale;
                float scaledDuplicateWidth = duplicateButtonWidth * fixedButtonScale;
                float scaledRemoveWidth = removeButtonWidth * fixedButtonScale;
                float layoutButtonWidth = Mathf.Max(
                    1f,
                    rowRect.width - scaledDefaultWidth - scaledDuplicateWidth - scaledRemoveWidth);
                Rect layoutRect = new(rowRect.x, rowRect.y, layoutButtonWidth, rowRect.height);
                Rect defaultRect = new(layoutRect.xMax, rowRect.y, scaledDefaultWidth, rowRect.height);
                Rect duplicateRect = new(defaultRect.xMax, rowRect.y, scaledDuplicateWidth, rowRect.height);
                Rect removeRect = new(duplicateRect.xMax, rowRect.y, scaledRemoveWidth, rowRect.height);

                bool selected = layout != null && layout == _selectedLayout;
                GUIContent rowContent = new(
                    layout != null ? layout.name : "Missing Layout",
                    BuildLayoutSummary(layout));
                bool nextSelected = LoogaEditorTabs.DrawSegmentedToggle(
                    layoutRect,
                    selected,
                    rowContent);
                if (nextSelected && !selected)
                    SelectLayout(layout);

                bool isDefault = defaultLayout.objectReferenceValue == layout;
                using (new EditorGUI.DisabledScope(isDefault || layout == null))
                {
                    if (LoogaEditorTabs.DrawSegmentedButton(
                            defaultRect,
                            new GUIContent(isDefault ? "Default" : "Make Default")))
                        defaultLayout.objectReferenceValue = layout;
                }

                using (new EditorGUI.DisabledScope(layout == null))
                {
                    if (LoogaEditorTabs.DrawSegmentedButton(
                            duplicateRect,
                            new GUIContent("Duplicate", "Create a copy of this layout.")))
                    {
                        DuplicateLayout(screen, layouts, layout);
                        return;
                    }
                }

                if (LoogaEditorTabs.DrawSegmentedButton(
                        removeRect,
                        new GUIContent("-", "Remove this layout.")))
                {
                    RemoveLayout(layouts, defaultLayout, i, layout);
                    return;
                }
            }

            if (GUILayout.Button("Add Layout"))
                CreateLayout(screen, layouts, defaultLayout);

            DrawSelectedLayout(screen);
        }

        private void CreateLayout(
            LoogaMenuScreenDefinition screen,
            SerializedProperty layouts,
            SerializedProperty defaultLayout)
        {
            string assetPath = AssetDatabase.GetAssetPath(screen);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                EditorUtility.DisplayDialog("Save Screen First",
                    "Save the screen asset before adding layouts.", "OK");
                return;
            }

            LoogaMenuScreenLayout layout = CreateInstance<LoogaMenuScreenLayout>();
            LoogaMenuScreenLayout[] existingLayouts = screen.Layouts ?? System.Array.Empty<LoogaMenuScreenLayout>();
            layout.name = ObjectNames.GetUniqueName(
                System.Array.ConvertAll(existingLayouts, value => value != null ? value.name : string.Empty),
                layouts.arraySize == 0 ? "Default" : "Layout");
            AssetDatabase.AddObjectToAsset(layout, screen);
            Undo.RegisterCreatedObjectUndo(layout, "Create Menu Screen Layout");

            int index = layouts.arraySize;
            layouts.InsertArrayElementAtIndex(index);
            layouts.GetArrayElementAtIndex(index).objectReferenceValue = layout;
            if (defaultLayout.objectReferenceValue == null)
                defaultLayout.objectReferenceValue = layout;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(screen);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            SelectLayout(layout);
            GUIUtility.ExitGUI();
        }

        private void DuplicateLayout(
            LoogaMenuScreenDefinition screen,
            SerializedProperty layouts,
            LoogaMenuScreenLayout source)
        {
            string assetPath = AssetDatabase.GetAssetPath(screen);
            if (source == null || string.IsNullOrWhiteSpace(assetPath))
                return;

            LoogaMenuScreenLayout duplicate = CreateInstance<LoogaMenuScreenLayout>();
            EditorUtility.CopySerialized(source, duplicate);
            duplicate.name = GetUniqueLayoutName(screen, $"{source.name} Copy");
            AssetDatabase.AddObjectToAsset(duplicate, screen);
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Menu Screen Layout");

            int index = layouts.arraySize;
            layouts.InsertArrayElementAtIndex(index);
            layouts.GetArrayElementAtIndex(index).objectReferenceValue = duplicate;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(screen);
            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();
            SelectLayout(duplicate);
            GUIUtility.ExitGUI();
        }

        private void RemoveLayout(
            SerializedProperty layouts,
            SerializedProperty defaultLayout,
            int index,
            LoogaMenuScreenLayout layout)
        {
            LoogaMenuScreenLayout nextSelection = null;
            if (_selectedLayout == layout && layouts.arraySize > 1)
            {
                int nextIndex = index < layouts.arraySize - 1 ? index + 1 : index - 1;
                nextSelection = layouts.GetArrayElementAtIndex(nextIndex).objectReferenceValue
                    as LoogaMenuScreenLayout;
            }

            SerializedProperty element = layouts.GetArrayElementAtIndex(index);
            element.objectReferenceValue = null;
            layouts.DeleteArrayElementAtIndex(index);
            if (defaultLayout.objectReferenceValue == layout)
            {
                defaultLayout.objectReferenceValue = layouts.arraySize > 0
                    ? layouts.GetArrayElementAtIndex(0).objectReferenceValue
                    : null;
            }

            serializedObject.ApplyModifiedProperties();
            if (layout != null && AssetDatabase.IsSubAsset(layout))
                Undo.DestroyObjectImmediate(layout);

            AssetDatabase.SaveAssets();
            SelectLayout(nextSelection);
            GUIUtility.ExitGUI();
        }

        private void DrawSelectedLayout(LoogaMenuScreenDefinition screen)
        {
            if (_selectedLayout == null || _selectedLayoutObject == null)
                return;

            EditorGUILayout.Space(4f);
            _selectedLayoutExpanded = LoogaGUILayout.FoldoutLarge(
                new GUIContent($"Layout Details: {_selectedLayout.name}"),
                _selectedLayoutExpanded,
                () =>
            {
                EditorGUI.BeginChangeCheck();
                string nextName = EditorGUILayout.DelayedTextField("Name", _selectedLayout.name);
                if (EditorGUI.EndChangeCheck())
                    RenameLayout(screen, _selectedLayout, nextName);

                _selectedLayoutObject.UpdateIfRequiredOrScript();
                _selectedLayoutTab = LoogaMenuScreenLayoutEditor.DrawBody(
                    _selectedLayoutObject,
                    _selectedLayoutTab,
                    $"{nameof(LoogaMenuScreenDefinitionEditor)}_Layout_{_selectedLayout.GetInstanceID()}",
                    propertyName => DrawLoogaProperty(_selectedLayoutObject, propertyName));
                _selectedLayoutObject.ApplyModifiedProperties();
            });
        }

        private void SelectLayout(LoogaMenuScreenLayout layout)
        {
            if (_selectedLayout == layout && (_selectedLayoutObject != null || layout == null))
                return;

            _selectedLayout = layout;
            _selectedLayoutObject = layout != null ? new SerializedObject(layout) : null;
            _selectedLayoutTab = 0;
        }

        private static void RenameLayout(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            string requestedName)
        {
            string fallbackName = string.IsNullOrWhiteSpace(layout.name) ? "Layout" : layout.name;
            string trimmedName = string.IsNullOrWhiteSpace(requestedName)
                ? fallbackName
                : requestedName.Trim();
            string uniqueName = GetUniqueLayoutName(screen, trimmedName, layout);
            if (layout.name == uniqueName)
                return;

            Undo.RecordObject(layout, "Rename Menu Screen Layout");
            layout.name = uniqueName;
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
        }

        private static string GetUniqueLayoutName(
            LoogaMenuScreenDefinition screen,
            string requestedName,
            LoogaMenuScreenLayout excludedLayout = null)
        {
            List<string> names = new();
            foreach (LoogaMenuScreenLayout layout in screen.Layouts ?? System.Array.Empty<LoogaMenuScreenLayout>())
            {
                if (layout != null && layout != excludedLayout)
                    names.Add(layout.name);
            }

            return ObjectNames.GetUniqueName(names.ToArray(), requestedName);
        }

        private static string BuildLayoutSummary(LoogaMenuScreenLayout layout)
        {
            if (layout == null)
                return "This layout reference is missing.";

            int panelCount = layout.Panels?.Length ?? 0;
            int regionCount = layout.RegionOverrides?.Length ?? 0;
            return $"{panelCount} panel(s), {regionCount} region override(s).";
        }

        private static void DrawValidation(LoogaMenuScreenDefinition screen)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            bool hasIssue = false;
            if ((screen.IncludeLayoutsInNavigation
                    || (screen.NavigationLinks?.Length ?? 0) > 0)
                && screen.NavigationSlot == null)
            {
                hasIssue = true;
                EditorGUILayout.HelpBox(
                    "Navigation is authored, but no shared navigation slot is assigned.",
                    MessageType.Warning);
            }
            else if (screen.NavigationSlot != null
                && !typeof(LoogaMenuNavigationRegionContent).IsAssignableFrom(
                    screen.NavigationSlot.ContentType))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox(
                    $"'{screen.NavigationSlot.name}' is not a navigation slot.",
                    MessageType.Error);
            }

            if (screen.Layouts == null || screen.Layouts.Length == 0)
            {
                hasIssue = true;
                EditorGUILayout.HelpBox("The screen has no layout.", MessageType.Error);
            }

            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            if (root == null)
            {
                EditorGUILayout.HelpBox(
                    "Open a scene with a LoogaMenuRoot to validate panel registrations.",
                    MessageType.Info);
                return;
            }

            foreach (LoogaMenuScreenLayout layout in screen.Layouts ?? System.Array.Empty<LoogaMenuScreenLayout>())
            {
                if (layout == null)
                    continue;

                HashSet<LoogaMenuPanelDefinition> panels = new();
                foreach (LoogaMenuScreenPanelEntry entry in layout.Panels)
                {
                    if (entry != null)
                        ValidatePanel(layout.DisplayName, entry.Panel, panels, ref hasIssue);
                }

                LoogaMenuStructureProfile structure = root.Structure;
                if (structure == null)
                    continue;

                List<LoogaMenuPanelDefinition> regionPanels = new();
                foreach (LoogaMenuRegionDefinition region in structure.Regions)
                {
                    LoogaMenuRegionPanelResolver.Collect(
                        root,
                        screen,
                        layout,
                        region,
                        regionPanels);
                    foreach (LoogaMenuPanelDefinition panel in regionPanels)
                    {
                        ValidatePanel(
                            $"{layout.DisplayName} / {region.DisplayName}",
                            panel,
                            panels,
                            ref hasIssue);
                    }
                }
            }

            if (!hasIssue)
                EditorGUILayout.HelpBox("No obvious screen setup issues found.", MessageType.None);
        }

        private static void ValidatePanel(
            string label,
            LoogaMenuPanelDefinition panel,
            HashSet<LoogaMenuPanelDefinition> panels,
            ref bool hasIssue)
        {
            if (panel == null)
                return;

            if (!panels.Add(panel))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox(
                    $"{label} references panel '{panel.name}' more than once.",
                    MessageType.Warning);
            }

            if (!LoogaMenuEditorUtility.TryFindPanel(panel, out _))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox(
                    $"{label} panel '{panel.name}' has no matching scene component.",
                    MessageType.Info);
            }
        }
    }
}
