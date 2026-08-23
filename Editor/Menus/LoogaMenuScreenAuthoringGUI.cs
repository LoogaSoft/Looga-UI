using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    /// <summary>Draws configured shared-slot overrides for screens and layouts.</summary>
    internal static class LoogaMenuScreenAuthoringGUI
    {
        private const float HeaderControlGap = 6f;
        private const float ModeFieldWidth = 92f;
        private const float SummaryMaximumWidth = 150f;
        private const float ElementPadding = 2f;
        private const float ReorderHandleClearance = 14f;

        private static readonly GUIContent[] StandardRegionModes =
        {
            new("Inherit"),
            new("Override"),
            new("Hide")
        };
        private static readonly int[] StandardRegionModeValues =
        {
            (int)LoogaMenuRegionMode.Inherit,
            (int)LoogaMenuRegionMode.Override,
            (int)LoogaMenuRegionMode.Hide
        };
        private static readonly GUIContent[] AdditiveRegionModes =
        {
            new("Inherit"),
            new("Add"),
            new("Override"),
            new("Hide")
        };
        private static readonly int[] AdditiveRegionModeValues =
        {
            (int)LoogaMenuRegionMode.Inherit,
            (int)LoogaMenuRegionMode.Add,
            (int)LoogaMenuRegionMode.Override,
            (int)LoogaMenuRegionMode.Hide
        };
        private static readonly Dictionary<int, ContentEditorState> ContentStates = new();
        private static readonly Dictionary<long, bool> RegionFoldouts = new();

        public static void DrawRegions(SerializedProperty overrides)
        {
            LoogaMenuStructureProfile structure = LoogaMenuStructureEditorUtility.FindStructure();
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Shared Slots", EditorStyles.boldLabel);

            if (structure == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign or create a Menu Project before authoring shared UI.",
                    MessageType.Info);
                return;
            }

            DrawStructureField(structure);
            foreach (LoogaMenuRegionDefinition region in structure.Regions)
            {
                if (region != null)
                    DrawRegion(overrides, region);
            }
        }

        private static void DrawStructureField(LoogaMenuStructureProfile structure)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(
                    new GUIContent("Menu Project", "Defines the shared UI slots available to this menu."),
                    structure,
                    typeof(LoogaMenuStructureProfile),
                    false);
                if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(46f)))
                    AssetDatabase.OpenAsset(structure);
            }

            EditorGUILayout.Space(3f);
        }

        private static void DrawRegion(SerializedProperty overrides, LoogaMenuRegionDefinition region)
        {
            int index = FindOverride(overrides, region);
            SerializedProperty regionOverride = index >= 0
                ? overrides.GetArrayElementAtIndex(index)
                : null;
            int mode = regionOverride != null
                ? regionOverride.FindPropertyRelative("_mode").intValue
                : (int)LoogaMenuRegionMode.Inherit;

            LoogaMenuRegionContent displayedContent = ResolveDisplayedContent(regionOverride, region, mode);
            long foldoutKey = GetFoldoutKey(overrides, region);
            bool expanded = GetRegionExpanded(foldoutKey);
            bool isAuthored = IsAuthoredMode((LoogaMenuRegionMode)mode);
            if (!isAuthored)
                expanded = false;

            int nextMode = mode;
            using (LoogaEditorFoldouts.BeginLargeFoldoutLayout(
                isAuthored && expanded,
                out Rect headerRect,
                out _))
            {
                nextMode = DrawRegionHeader(
                    headerRect,
                    region.DisplayName,
                    BuildContentSummary(displayedContent, (LoogaMenuRegionMode)mode),
                    mode,
                    isAuthored,
                    region.DefaultContent != null && region.DefaultContent.SupportsAdd,
                    ref expanded);

                if (nextMode != mode)
                {
                    regionOverride ??= AddOverride(overrides, region);
                    regionOverride.FindPropertyRelative("_mode").intValue = nextMode;
                    isAuthored = IsAuthoredMode((LoogaMenuRegionMode)nextMode);
                    expanded = isAuthored;
                }

                SetRegionExpanded(foldoutKey, expanded);
                if (!isAuthored || !expanded)
                    return;

                regionOverride ??= AddOverride(overrides, region);
                SerializedProperty contentProperty = regionOverride.FindPropertyRelative("_content");
                LoogaMenuRegionContent content = EnsureContent(
                    overrides.serializedObject.targetObject,
                    region,
                    contentProperty);

                using (LoogaEditorFoldouts.ContainedFoldoutScope())
                {
                    EditorGUILayout.Space(3f);
                    DrawContent(content);
                    EditorGUILayout.Space(3f);
                }
            }
        }

        private static int DrawRegionHeader(
            Rect headerRect,
            string title,
            string summary,
            int mode,
            bool canExpand,
            bool supportsAdd,
            ref bool expanded)
        {
            Event current = Event.current;
            Rect contentRect = LoogaEditorFoldouts.GetLargeFoldoutHeaderContentRect(
                headerRect,
                false);
            Rect modeRect = new(
                contentRect.xMax - ModeFieldWidth,
                contentRect.y,
                ModeFieldWidth,
                contentRect.height);

            float arrowWidth = canExpand
                ? LoogaEditorStyle.FoldoutTriangleSize + HeaderControlGap
                : 0f;
            Rect arrowRect = new(
                modeRect.x - arrowWidth,
                headerRect.center.y - LoogaEditorStyle.FoldoutTriangleSize * 0.5f,
                LoogaEditorStyle.FoldoutTriangleSize,
                LoogaEditorStyle.FoldoutTriangleSize);
            float summaryRight = canExpand
                ? arrowRect.x - HeaderControlGap
                : modeRect.x - HeaderControlGap;
            float availableSummaryWidth = Mathf.Max(0f, summaryRight - contentRect.x - 90f);
            float summaryWidth = Mathf.Min(
                SummaryMaximumWidth,
                EditorStyles.miniLabel.CalcSize(new GUIContent(summary)).x);
            summaryWidth = Mathf.Min(summaryWidth, availableSummaryWidth);
            Rect summaryRect = new(
                summaryRight - summaryWidth,
                contentRect.y,
                summaryWidth,
                contentRect.height);
            Rect titleRect = new(
                contentRect.x,
                contentRect.y,
                Mathf.Max(0f, summaryRect.x - HeaderControlGap - contentRect.x),
                contentRect.height);

            if (current.type == EventType.Repaint && headerRect.Contains(current.mousePosition))
                EditorGUI.DrawRect(headerRect, LoogaEditorStyle.HoverColor);

            GUI.Label(titleRect, title, EditorStyles.boldLabel);
            if (summaryWidth > 0f)
            {
                GUIStyle summaryStyle = new(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight
                };
                GUI.Label(summaryRect, summary, summaryStyle);
            }

            if (canExpand)
                LoogaEditorStyle.DrawFoldoutTriangle(arrowRect, expanded);

            int nextMode = EditorGUI.IntPopup(
                modeRect,
                mode,
                supportsAdd ? AdditiveRegionModes : StandardRegionModes,
                supportsAdd ? AdditiveRegionModeValues : StandardRegionModeValues);
            Rect toggleRect = new(
                headerRect.x,
                headerRect.y,
                Mathf.Max(0f, modeRect.x - HeaderControlGap - headerRect.x),
                headerRect.height);
            if (canExpand
                && current.type == EventType.MouseDown
                && current.button == 0
                && toggleRect.Contains(current.mousePosition))
            {
                expanded = !expanded;
                current.Use();
            }

            return nextMode;
        }

        internal static void DrawContent(LoogaMenuRegionContent content)
        {
            if (content == null)
                return;

            ContentEditorState state = GetContentState(content);
            SerializedObject contentObject = state.SerializedObject;
            contentObject.UpdateIfRequiredOrScript();

            if (content is LoogaMenuNavigationRegionContent)
                DrawNavigationContent(state);
            else if (content is LoogaMenuActionRegionContent)
                DrawActionContent(contentObject);
            else
                DrawPanelContent(state);

            contentObject.ApplyModifiedProperties();
        }

        private static void DrawPanelContent(ContentEditorState state)
        {
            state.PanelList ??= CreatePanelList(
                state.SerializedObject,
                state.SerializedObject.FindProperty("_panels"));
            state.PanelList.DoLayoutList();
        }

        private static void DrawNavigationContent(ContentEditorState state)
        {
            SerializedProperty entries = state.SerializedObject.FindProperty("_entries");
            state.NavigationList ??= CreateNavigationList(state.SerializedObject, entries);
            state.NavigationList.DoLayoutList();

            SerializedProperty defaultIndex = state.SerializedObject.FindProperty("_defaultEntryIndex");
            if (entries.arraySize == 0)
                return;

            string[] entryNames = BuildEntryNames(entries);
            defaultIndex.intValue = EditorGUILayout.Popup(
                new GUIContent("Default Entry", "The entry selected when this navigation region opens."),
                Mathf.Clamp(defaultIndex.intValue, 0, entries.arraySize - 1),
                entryNames);
        }

        private static void DrawActionContent(SerializedObject contentObject)
        {
            LoogaGUILayout.PropertyField(contentObject.FindProperty("_panel"));
            SerializedProperty showBack = contentObject.FindProperty("_showBackAction");
            LoogaGUILayout.PropertyField(showBack);
            if (showBack.boolValue)
            {
                LoogaGUILayout.PropertyField(contentObject.FindProperty("_backLabel"));
                LoogaGUILayout.PropertyField(contentObject.FindProperty("_backInputAction"));
            }

            LoogaGUILayout.PropertyField(contentObject.FindProperty("_includeCoveredPanels"));
            SerializedProperty advanced = contentObject.FindProperty("_backBindingFallback");
            advanced.isExpanded = LoogaGUILayout.FoldoutSmall(
                "Advanced",
                advanced.isExpanded,
                () =>
                {
                    LoogaGUILayout.PropertyField(advanced);
                    LoogaGUILayout.PropertyField(contentObject.FindProperty("_backSortOrder"));
                },
                advanced);
        }

        private static ReorderableList CreatePanelList(
            SerializedObject serializedObject,
            SerializedProperty panels)
        {
            ReorderableList list = new(serializedObject, panels, true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + ElementPadding * 2f
            };
            list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, $"Panels ({panels.arraySize})", EditorStyles.boldLabel);
            list.drawElementCallback = (rect, index, _, _) =>
            {
                Rect fieldRect = new(
                    rect.x,
                    rect.y + ElementPadding,
                    rect.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(
                    fieldRect,
                    panels.GetArrayElementAtIndex(index),
                    GUIContent.none);
            };
            list.onAddCallback = _ =>
            {
                int index = panels.arraySize;
                panels.InsertArrayElementAtIndex(index);
                panels.GetArrayElementAtIndex(index).objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
            };
            list.onRemoveCallback = currentList =>
            {
                if (currentList.index < 0 || currentList.index >= panels.arraySize)
                    return;

                SerializedProperty element = panels.GetArrayElementAtIndex(currentList.index);
                element.objectReferenceValue = null;
                panels.DeleteArrayElementAtIndex(currentList.index);
                serializedObject.ApplyModifiedProperties();
            };
            return list;
        }

        private static ReorderableList CreateNavigationList(
            SerializedObject serializedObject,
            SerializedProperty entries)
        {
            ReorderableList list = new(serializedObject, entries, true, true, true, true);
            list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, $"Navigation Entries ({entries.arraySize})", EditorStyles.boldLabel);
            list.elementHeightCallback = index => GetNavigationElementHeight(
                entries.GetArrayElementAtIndex(index));
            list.drawElementCallback = (rect, index, _, _) =>
                DrawNavigationElement(rect, entries.GetArrayElementAtIndex(index), index);
            list.onAddCallback = _ =>
            {
                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_displayName").stringValue = string.Empty;
                SerializedProperty destination = entry.FindPropertyRelative("_destination");
                destination.FindPropertyRelative("_screen").objectReferenceValue = null;
                destination.FindPropertyRelative("_layout").objectReferenceValue = null;
                destination.FindPropertyRelative("_openMode").enumValueIndex =
                    (int)LoogaMenuOpenMode.Replace;
                entry.FindPropertyRelative("_requirements").objectReferenceValue = null;
                entry.isExpanded = true;
                serializedObject.ApplyModifiedProperties();
                list.index = index;
            };
            list.onRemoveCallback = currentList =>
            {
                if (currentList.index < 0 || currentList.index >= entries.arraySize)
                    return;

                entries.DeleteArrayElementAtIndex(currentList.index);
                serializedObject.ApplyModifiedProperties();
            };
            return list;
        }

        private static float GetNavigationElementHeight(SerializedProperty entry)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = line + ElementPadding * 2f;
            if (!entry.isExpanded)
                return height;

            height += (line + spacing) * 4f;
            SerializedProperty advancedState = entry.FindPropertyRelative("_requirements");
            if (advancedState.isExpanded)
                height += (line + spacing) * 2f;

            return height;
        }

        private static void DrawNavigationElement(
            Rect rect,
            SerializedProperty entry,
            int index)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect row = new(rect.x, rect.y + ElementPadding, rect.width, line);
            row.xMin += ReorderHandleClearance;
            string title = GetEntryName(entry, index);
            entry.isExpanded = EditorGUI.Foldout(
                row,
                entry.isExpanded,
                new GUIContent(title, BuildEntryTooltip(entry)),
                true);
            if (!entry.isExpanded)
                return;

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = previousIndent + 1;
            try
            {
                SerializedProperty destination = entry.FindPropertyRelative("_destination");
                DrawNextProperty(ref row, spacing, entry.FindPropertyRelative("_displayName"));
                DrawNextProperty(ref row, spacing, destination.FindPropertyRelative("_screen"));
                DrawNextProperty(ref row, spacing, destination.FindPropertyRelative("_layout"));

                SerializedProperty requirements = entry.FindPropertyRelative("_requirements");
                row.y += line + spacing;
                requirements.isExpanded = EditorGUI.Foldout(
                    row,
                    requirements.isExpanded,
                    "Advanced",
                    true);
                if (!requirements.isExpanded)
                    return;

                DrawNextProperty(ref row, spacing, destination.FindPropertyRelative("_openMode"));
                DrawNextProperty(ref row, spacing, requirements);
            }
            finally
            {
                EditorGUI.indentLevel = previousIndent;
            }
        }

        private static void DrawNextProperty(
            ref Rect currentRow,
            float spacing,
            SerializedProperty property)
        {
            currentRow.y += EditorGUIUtility.singleLineHeight + spacing;
            EditorGUI.PropertyField(currentRow, property);
        }

        private static string[] BuildEntryNames(SerializedProperty entries)
        {
            string[] names = new string[entries.arraySize];
            for (int i = 0; i < entries.arraySize; i++)
                names[i] = GetEntryName(entries.GetArrayElementAtIndex(i), i);

            return names;
        }

        private static string GetEntryName(SerializedProperty entry, int index)
        {
            string displayName = entry.FindPropertyRelative("_displayName").stringValue?.Trim();
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            SerializedProperty destination = entry.FindPropertyRelative("_destination");
            UnityEngine.Object screen = destination.FindPropertyRelative("_screen").objectReferenceValue;
            return screen != null ? screen.name : $"Entry {index + 1}";
        }

        private static string BuildEntryTooltip(SerializedProperty entry)
        {
            SerializedProperty destination = entry.FindPropertyRelative("_destination");
            UnityEngine.Object screen = destination.FindPropertyRelative("_screen").objectReferenceValue;
            UnityEngine.Object layout = destination.FindPropertyRelative("_layout").objectReferenceValue;
            if (screen == null)
                return "No destination screen is assigned.";

            return layout != null
                ? $"Opens {screen.name} using {layout.name}."
                : $"Opens {screen.name} using its default layout.";
        }

        private static ContentEditorState GetContentState(LoogaMenuRegionContent content)
        {
            int key = content.GetInstanceID();
            if (!ContentStates.TryGetValue(key, out ContentEditorState state)
                || state.SerializedObject.targetObject != content)
            {
                state = new ContentEditorState(content);
                ContentStates[key] = state;
            }

            return state;
        }

        private static LoogaMenuRegionContent ResolveDisplayedContent(
            SerializedProperty regionOverride,
            LoogaMenuRegionDefinition region,
            int mode)
        {
            if ((LoogaMenuRegionMode)mode == LoogaMenuRegionMode.Inherit)
                return region.DefaultContent;
            if ((LoogaMenuRegionMode)mode == LoogaMenuRegionMode.Hide || regionOverride == null)
                return null;

            return regionOverride.FindPropertyRelative("_content").objectReferenceValue
                as LoogaMenuRegionContent;
        }

        private static bool IsAuthoredMode(LoogaMenuRegionMode mode)
        {
            return mode == LoogaMenuRegionMode.Override || mode == LoogaMenuRegionMode.Add;
        }

        private static string BuildContentSummary(
            LoogaMenuRegionContent content,
            LoogaMenuRegionMode mode)
        {
            if (mode == LoogaMenuRegionMode.Hide)
                return "Not shown";
            string prefix = mode == LoogaMenuRegionMode.Add ? "+" : string.Empty;
            if (content is LoogaMenuPanelRegionContent panelContent)
                return prefix + FormatCount(panelContent.Panels?.Count ?? 0, "panel");
            if (content is LoogaMenuNavigationRegionContent navigationContent)
                return prefix + FormatCount(navigationContent.Entries?.Count ?? 0, "entry");
            if (content is LoogaMenuActionRegionContent actionContent)
            {
                if (actionContent.Panel != null && actionContent.ShowBackAction)
                    return "Panel + Back";
                if (actionContent.Panel != null)
                    return "Panel";
                return actionContent.ShowBackAction ? "Back action" : "No actions";
            }

            return content != null ? "Configured" : "Not configured";
        }

        private static string FormatCount(int count, string singular)
        {
            return $"{count} {singular}{(count == 1 ? string.Empty : "s")}";
        }

        private static LoogaMenuRegionContent EnsureContent(
            UnityEngine.Object owner,
            LoogaMenuRegionDefinition region,
            SerializedProperty contentProperty)
        {
            LoogaMenuRegionContent content = contentProperty.objectReferenceValue
                as LoogaMenuRegionContent;
            if (content != null && content.GetType() == region.ContentType)
                return content;

            content = CreateContent(owner, region);
            contentProperty.objectReferenceValue = content;
            contentProperty.serializedObject.ApplyModifiedProperties();
            return content;
        }

        private static long GetFoldoutKey(
            SerializedProperty overrides,
            LoogaMenuRegionDefinition region)
        {
            return ((long)overrides.serializedObject.targetObject.GetInstanceID() << 32)
                ^ (uint)region.GetInstanceID();
        }

        private static bool GetRegionExpanded(long key)
        {
            if (RegionFoldouts.TryGetValue(key, out bool expanded))
                return expanded;

            RegionFoldouts[key] = true;
            return true;
        }

        private static void SetRegionExpanded(long key, bool expanded)
        {
            RegionFoldouts[key] = expanded;
        }

        private static int FindOverride(SerializedProperty overrides, LoogaMenuRegionDefinition region)
        {
            for (int i = 0; i < overrides.arraySize; i++)
            {
                SerializedProperty candidate = overrides.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("_region").objectReferenceValue == region)
                    return i;
            }

            return -1;
        }

        private static SerializedProperty AddOverride(
            SerializedProperty overrides,
            LoogaMenuRegionDefinition region)
        {
            int index = overrides.arraySize;
            overrides.InsertArrayElementAtIndex(index);
            SerializedProperty regionOverride = overrides.GetArrayElementAtIndex(index);
            regionOverride.FindPropertyRelative("_region").objectReferenceValue = region;
            regionOverride.FindPropertyRelative("_mode").intValue =
                (int)LoogaMenuRegionMode.Inherit;
            regionOverride.FindPropertyRelative("_content").objectReferenceValue = null;
            return regionOverride;
        }

        private static LoogaMenuRegionContent CreateContent(
            UnityEngine.Object owner,
            LoogaMenuRegionDefinition region)
        {
            LoogaMenuRegionContent content =
                ScriptableObject.CreateInstance(region.ContentType) as LoogaMenuRegionContent;
            content.name = region.DisplayName;
            string ownerPath = AssetDatabase.GetAssetPath(owner);
            if (!string.IsNullOrWhiteSpace(ownerPath))
                AssetDatabase.AddObjectToAsset(content, owner);

            Undo.RegisterCreatedObjectUndo(content, "Create Menu Region Content");
            EditorUtility.SetDirty(owner);
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            return content;
        }

        private sealed class ContentEditorState
        {
            public ContentEditorState(LoogaMenuRegionContent content)
            {
                SerializedObject = new SerializedObject(content);
            }

            public SerializedObject SerializedObject { get; }
            public ReorderableList NavigationList { get; set; }
            public ReorderableList PanelList { get; set; }
        }
    }

    internal static class LoogaMenuStructureEditorUtility
    {
        public static LoogaMenuStructureProfile FindStructure()
        {
            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            if (root != null && root.Structure != null)
                return root.Structure;

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LoogaMenuStructureProfile)}");
            if (guids.Length != 1)
                return null;

            return AssetDatabase.LoadAssetAtPath<LoogaMenuStructureProfile>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
