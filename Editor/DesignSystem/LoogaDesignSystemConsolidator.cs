using System.Collections.Generic;
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
    }
}
