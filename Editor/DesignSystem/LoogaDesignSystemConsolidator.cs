using System.Collections.Generic;
using LoogaSoft.UI.Controls;
using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Maintains a concise public style library without changing bound one-off visuals.</summary>
    internal static class LoogaDesignSystemConsolidator
    {
        internal static LoogaDesignSystem FindLoadedDesignSystem()
        {
            LoogaThemeScope[] scopes = Object.FindObjectsByType<LoogaThemeScope>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < scopes.Length; i++)
            {
                if (scopes[i].DesignSystem != null)
                {
                    return scopes[i].DesignSystem;
                }
            }

            return null;
        }

        internal static Report Analyze(LoogaDesignSystem designSystem, int reuseThreshold)
        {
            Dictionary<LoogaElementStyle, int> usage = CollectUsage(designSystem);
            HashSet<LoogaElementStyle> publicStyles = CollectPublicStyles(designSystem);
            int reusableCaptured = 0;

            foreach (KeyValuePair<LoogaElementStyle, int> pair in usage)
            {
                if (!publicStyles.Contains(pair.Key) && pair.Value >= reuseThreshold)
                {
                    reusableCaptured++;
                }
            }

            return new Report(
                usage.Count,
                publicStyles.Count,
                CountCaptured(usage, publicStyles),
                reusableCaptured);
        }

        internal static Report Consolidate(LoogaDesignSystem designSystem, int reuseThreshold)
        {
            Dictionary<LoogaElementStyle, int> usage = CollectUsage(designSystem);
            List<LoogaElementStyle> publicStyles = new List<LoogaElementStyle>();

            foreach (KeyValuePair<LoogaElementStyle, int> pair in usage)
            {
                if (pair.Key.KeepPublic || pair.Value >= reuseThreshold)
                {
                    publicStyles.Add(pair.Key);
                }
            }

            publicStyles.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            SetPublicStyles(designSystem, publicStyles);
            return Analyze(designSystem, reuseThreshold);
        }

        internal static void PromoteSelection(LoogaDesignSystem designSystem)
        {
            HashSet<LoogaElementStyle> publicStyles = CollectPublicStyles(designSystem);
            GameObject[] selectedObjects = Selection.gameObjects;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (!selectedObjects[i].TryGetComponent(out LoogaStyleBinding binding) ||
                    binding.Style == null)
                {
                    continue;
                }

                SetKeepPublic(binding.Style, true);
                publicStyles.Add(binding.Style);
            }

            List<LoogaElementStyle> ordered = new List<LoogaElementStyle>(publicStyles);
            ordered.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            SetPublicStyles(designSystem, ordered);
        }

        internal static SynchronizationReport Synchronize(LoogaDesignSystem designSystem)
        {
            HashSet<LoogaElementStyle> elementStyles = new HashSet<LoogaElementStyle>();
            HashSet<LoogaButtonStyle> buttonStyles = new HashSet<LoogaButtonStyle>();
            HashSet<LoogaToggleStyle> toggleStyles = new HashSet<LoogaToggleStyle>();
            HashSet<LoogaSliderStyle> sliderStyles = new HashSet<LoogaSliderStyle>();
            HashSet<LoogaScrollbarStyle> scrollbarStyles = new HashSet<LoogaScrollbarStyle>();
            HashSet<LoogaInputFieldStyle> inputFieldStyles = new HashSet<LoogaInputFieldStyle>();
            HashSet<LoogaDropdownStyle> dropdownStyles = new HashSet<LoogaDropdownStyle>();

            int removedEntries = 0;
            removedEntries += AddExisting(designSystem.Styles, elementStyles);
            removedEntries += AddExisting(designSystem.ButtonStyles, buttonStyles);
            removedEntries += AddExisting(designSystem.ToggleStyles, toggleStyles);
            removedEntries += AddExisting(designSystem.SliderStyles, sliderStyles);
            removedEntries += AddExisting(designSystem.ScrollbarStyles, scrollbarStyles);
            removedEntries += AddExisting(designSystem.InputFieldStyles, inputFieldStyles);
            removedEntries += AddExisting(designSystem.DropdownStyles, dropdownStyles);

            int originalCount = CountAllStyles(
                elementStyles,
                buttonStyles,
                toggleStyles,
                sliderStyles,
                scrollbarStyles,
                inputFieldStyles,
                dropdownStyles);

            AddContainedStyles(
                designSystem,
                elementStyles,
                buttonStyles,
                toggleStyles,
                sliderStyles,
                scrollbarStyles,
                inputFieldStyles,
                dropdownStyles);
            AddLoadedStyles(
                designSystem,
                elementStyles,
                buttonStyles,
                toggleStyles,
                sliderStyles,
                scrollbarStyles,
                inputFieldStyles,
                dropdownStyles);

            int synchronizedCount = CountAllStyles(
                elementStyles,
                buttonStyles,
                toggleStyles,
                sliderStyles,
                scrollbarStyles,
                inputFieldStyles,
                dropdownStyles);

            SerializedObject serializedSystem = new SerializedObject(designSystem);
            Undo.RecordObject(designSystem, "Synchronize UI Style Library");
            SetList(serializedSystem, "_styles", elementStyles);
            SetList(serializedSystem, "_buttonStyles", buttonStyles);
            SetList(serializedSystem, "_toggleStyles", toggleStyles);
            SetList(serializedSystem, "_sliderStyles", sliderStyles);
            SetList(serializedSystem, "_scrollbarStyles", scrollbarStyles);
            SetList(serializedSystem, "_inputFieldStyles", inputFieldStyles);
            SetList(serializedSystem, "_dropdownStyles", dropdownStyles);
            serializedSystem.ApplyModifiedProperties();
            EditorUtility.SetDirty(designSystem);
            AssetDatabase.SaveAssets();

            return new SynchronizationReport(
                synchronizedCount,
                synchronizedCount - originalCount,
                removedEntries);
        }

        internal static void MergeSelectionInto(LoogaElementStyle target)
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            int bindingCount = 0;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i].TryGetComponent(out LoogaStyleBinding _))
                {
                    bindingCount++;
                }
            }

            if (bindingCount == 0 || !EditorUtility.DisplayDialog(
                    "Merge Selected Styles",
                    $"Assign {target.name} to {bindingCount} selected style binding(s)?",
                    "Merge",
                    "Cancel"))
            {
                return;
            }

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (!selectedObjects[i].TryGetComponent(out LoogaStyleBinding binding))
                {
                    continue;
                }

                Undo.RecordObject(binding, "Merge UI Styles");
                binding.SetStyle(target);
                EditorUtility.SetDirty(binding);
            }
        }

        private static Dictionary<LoogaElementStyle, int> CollectUsage(LoogaDesignSystem designSystem)
        {
            Dictionary<LoogaElementStyle, int> usage = new Dictionary<LoogaElementStyle, int>();
            LoogaStyleBinding[] bindings = Object.FindObjectsByType<LoogaStyleBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < bindings.Length; i++)
            {
                LoogaStyleBinding binding = bindings[i];
                LoogaThemeScope scope = LoogaThemeScope.FindNearest(binding.transform);
                if (binding.Style == null || scope == null || scope.DesignSystem != designSystem)
                {
                    continue;
                }

                usage.TryGetValue(binding.Style, out int count);
                usage[binding.Style] = count + 1;
            }

            return usage;
        }

        private static HashSet<LoogaElementStyle> CollectPublicStyles(LoogaDesignSystem designSystem)
        {
            HashSet<LoogaElementStyle> styles = new HashSet<LoogaElementStyle>();
            IReadOnlyList<LoogaElementStyle> registered = designSystem.Styles;
            for (int i = 0; i < registered.Count; i++)
            {
                if (registered[i] != null)
                {
                    styles.Add(registered[i]);
                }
            }

            return styles;
        }

        private static int CountCaptured(
            Dictionary<LoogaElementStyle, int> usage,
            HashSet<LoogaElementStyle> publicStyles)
        {
            int count = 0;
            foreach (LoogaElementStyle style in usage.Keys)
            {
                if (!publicStyles.Contains(style))
                {
                    count++;
                }
            }

            return count;
        }

        private static int AddExisting<T>(IReadOnlyList<T> source, HashSet<T> destination)
            where T : UnityEngine.Object
        {
            int removedEntries = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == null || !destination.Add(source[i]))
                {
                    removedEntries++;
                }
            }

            return removedEntries;
        }

        private static int CountAllStyles(
            HashSet<LoogaElementStyle> elementStyles,
            HashSet<LoogaButtonStyle> buttonStyles,
            HashSet<LoogaToggleStyle> toggleStyles,
            HashSet<LoogaSliderStyle> sliderStyles,
            HashSet<LoogaScrollbarStyle> scrollbarStyles,
            HashSet<LoogaInputFieldStyle> inputFieldStyles,
            HashSet<LoogaDropdownStyle> dropdownStyles)
        {
            return elementStyles.Count +
                   buttonStyles.Count +
                   toggleStyles.Count +
                   sliderStyles.Count +
                   scrollbarStyles.Count +
                   inputFieldStyles.Count +
                   dropdownStyles.Count;
        }

        private static void AddContainedStyles(
            LoogaDesignSystem designSystem,
            HashSet<LoogaElementStyle> elementStyles,
            HashSet<LoogaButtonStyle> buttonStyles,
            HashSet<LoogaToggleStyle> toggleStyles,
            HashSet<LoogaSliderStyle> sliderStyles,
            HashSet<LoogaScrollbarStyle> scrollbarStyles,
            HashSet<LoogaInputFieldStyle> inputFieldStyles,
            HashSet<LoogaDropdownStyle> dropdownStyles)
        {
            string path = AssetDatabase.GetAssetPath(designSystem);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                switch (assets[i])
                {
                    case LoogaElementStyle elementStyle when elementStyle.KeepPublic:
                        elementStyles.Add(elementStyle);
                        break;
                    case LoogaButtonStyle buttonStyle:
                        buttonStyles.Add(buttonStyle);
                        break;
                    case LoogaToggleStyle toggleStyle:
                        toggleStyles.Add(toggleStyle);
                        break;
                    case LoogaSliderStyle sliderStyle:
                        sliderStyles.Add(sliderStyle);
                        break;
                    case LoogaScrollbarStyle scrollbarStyle:
                        scrollbarStyles.Add(scrollbarStyle);
                        break;
                    case LoogaInputFieldStyle inputFieldStyle:
                        inputFieldStyles.Add(inputFieldStyle);
                        break;
                    case LoogaDropdownStyle dropdownStyle:
                        dropdownStyles.Add(dropdownStyle);
                        break;
                }
            }
        }

        private static void AddLoadedStyles(
            LoogaDesignSystem designSystem,
            HashSet<LoogaElementStyle> elementStyles,
            HashSet<LoogaButtonStyle> buttonStyles,
            HashSet<LoogaToggleStyle> toggleStyles,
            HashSet<LoogaSliderStyle> sliderStyles,
            HashSet<LoogaScrollbarStyle> scrollbarStyles,
            HashSet<LoogaInputFieldStyle> inputFieldStyles,
            HashSet<LoogaDropdownStyle> dropdownStyles)
        {
            AddBindingStyles(designSystem, elementStyles);
            AddControlStyles<LoogaButton, LoogaButtonStyle>(
                designSystem,
                buttonStyles,
                control => control.StyleSource,
                control => control.Style);
            AddControlStyles<LoogaToggle, LoogaToggleStyle>(
                designSystem,
                toggleStyles,
                control => control.StyleSource,
                control => control.Style);
            AddControlStyles<LoogaSlider, LoogaSliderStyle>(
                designSystem,
                sliderStyles,
                control => control.StyleSource,
                control => control.Style);
            AddControlStyles<LoogaScrollbar, LoogaScrollbarStyle>(
                designSystem,
                scrollbarStyles,
                control => control.StyleSource,
                control => control.Style);
            AddControlStyles<LoogaInputField, LoogaInputFieldStyle>(
                designSystem,
                inputFieldStyles,
                control => control.StyleSource,
                control => control.Style);
            AddControlStyles<LoogaDropdown, LoogaDropdownStyle>(
                designSystem,
                dropdownStyles,
                control => control.StyleSource,
                control => control.Style);
        }

        private static void AddBindingStyles(
            LoogaDesignSystem designSystem,
            HashSet<LoogaElementStyle> styles)
        {
            LoogaStyleBinding[] bindings = UnityEngine.Object.FindObjectsByType<LoogaStyleBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < bindings.Length; i++)
            {
                LoogaThemeScope scope = LoogaThemeScope.FindNearest(bindings[i].transform);
                if (bindings[i].Style != null && scope?.DesignSystem == designSystem)
                {
                    styles.Add(bindings[i].Style);
                }
            }
        }

        private static void AddControlStyles<TControl, TStyle>(
            LoogaDesignSystem designSystem,
            HashSet<TStyle> styles,
            System.Func<TControl, LoogaControlStyleSource> getSource,
            System.Func<TControl, TStyle> getStyle)
            where TControl : Component
            where TStyle : UnityEngine.Object
        {
            TControl[] controls = UnityEngine.Object.FindObjectsByType<TControl>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < controls.Length; i++)
            {
                TStyle style = getStyle(controls[i]);
                if (style != null && UsesDesignSystem(controls[i].transform, getSource(controls[i]), designSystem))
                {
                    styles.Add(style);
                }
            }
        }

        private static bool UsesDesignSystem(
            Transform transform,
            LoogaControlStyleSource source,
            LoogaDesignSystem designSystem)
        {
            LoogaThemeScope scope = LoogaThemeScope.FindNearest(transform);
            return source == LoogaControlStyleSource.DesignSystem && scope?.DesignSystem == designSystem;
        }

        private static void SetList<T>(
            SerializedObject serializedSystem,
            string propertyName,
            HashSet<T> styles)
            where T : UnityEngine.Object
        {
            List<T> ordered = new List<T>(styles);
            ordered.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

            SerializedProperty property = serializedSystem.FindProperty(propertyName);
            property.arraySize = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            }
        }

        private static void SetKeepPublic(LoogaElementStyle style, bool value)
        {
            SerializedObject serializedStyle = new SerializedObject(style);
            serializedStyle.FindProperty("_keepPublic").boolValue = value;
            serializedStyle.ApplyModifiedProperties();
            EditorUtility.SetDirty(style);
        }

        private static void SetPublicStyles(
            LoogaDesignSystem designSystem,
            IReadOnlyList<LoogaElementStyle> styles)
        {
            Undo.RecordObject(designSystem, "Consolidate UI Styles");
            SerializedObject serializedSystem = new SerializedObject(designSystem);
            SerializedProperty property = serializedSystem.FindProperty("_styles");
            property.arraySize = styles.Count;
            for (int i = 0; i < styles.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = styles[i];
            }

            serializedSystem.ApplyModifiedProperties();
            EditorUtility.SetDirty(designSystem);
            AssetDatabase.SaveAssets();
        }

        internal readonly struct Report
        {
            internal Report(
                int boundStyles,
                int publicStyles,
                int capturedStyles,
                int reusableCapturedStyles)
            {
                BoundStyles = boundStyles;
                PublicStyles = publicStyles;
                CapturedStyles = capturedStyles;
                ReusableCapturedStyles = reusableCapturedStyles;
            }

            internal int BoundStyles { get; }
            internal int PublicStyles { get; }
            internal int CapturedStyles { get; }
            internal int ReusableCapturedStyles { get; }
        }

        internal readonly struct SynchronizationReport
        {
            internal SynchronizationReport(int registeredStyles, int addedStyles, int removedEntries)
            {
                RegisteredStyles = registeredStyles;
                AddedStyles = addedStyles;
                RemovedEntries = removedEntries;
            }

            internal int RegisteredStyles { get; }
            internal int AddedStyles { get; }
            internal int RemovedEntries { get; }
        }
    }
}
