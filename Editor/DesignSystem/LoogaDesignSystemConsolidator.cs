using System;
using System.Collections.Generic;
using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Synchronizes the four supported style lists with stored sub-assets.</summary>
    internal static class LoogaDesignSystemConsolidator
    {
        internal static LoogaDesignSystem FindLoadedDesignSystem()
        {
            LoogaThemeScope[] scopes = UnityEngine.Object.FindObjectsByType<LoogaThemeScope>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < scopes.Length; i++)
            {
                if (scopes[i].DesignSystem != null)
                {
                    return scopes[i].DesignSystem;
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:LoogaDesignSystem");
            if (guids.Length != 1)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LoogaDesignSystem>(path);
        }

        internal static SynchronizationReport Synchronize(LoogaDesignSystem designSystem)
        {
            if (designSystem == null)
            {
                return default;
            }

            string path = AssetDatabase.GetAssetPath(designSystem);
            UnityEngine.Object[] storedAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<LoogaColorToken> colors = Collect<LoogaColorToken>(storedAssets);
            List<LoogaControlStyle> controls = Collect<LoogaControlStyle>(storedAssets);
            List<LoogaImageStyle> images = Collect<LoogaImageStyle>(storedAssets);
            List<LoogaTextStyle> texts = Collect<LoogaTextStyle>(storedAssets);

            SerializedObject serializedSystem = new(designSystem);
            int previousEntries = CountEntries(serializedSystem, "_colors") +
                CountEntries(serializedSystem, "_controls") +
                CountEntries(serializedSystem, "_images") +
                CountEntries(serializedSystem, "_texts");
            int retainedEntries = CountRetained(serializedSystem, "_colors", colors) +
                CountRetained(serializedSystem, "_controls", controls) +
                CountRetained(serializedSystem, "_images", images) +
                CountRetained(serializedSystem, "_texts", texts);

            Undo.RecordObject(designSystem, "Synchronize UI Style Library");
            SetArray(serializedSystem, "_colors", colors);
            SetArray(serializedSystem, "_controls", controls);
            SetArray(serializedSystem, "_images", images);
            SetArray(serializedSystem, "_texts", texts);
            serializedSystem.ApplyModifiedProperties();
            EditorUtility.SetDirty(designSystem);
            AssetDatabase.SaveAssets();

            int registered = colors.Count + controls.Count + images.Count + texts.Count;
            return new SynchronizationReport(
                registered,
                Mathf.Max(0, registered - retainedEntries),
                Mathf.Max(0, previousEntries - retainedEntries));
        }

        internal static bool IsSupportedStyle(UnityEngine.Object asset)
        {
            return asset is LoogaColorToken or
                LoogaControlStyle or
                LoogaImageStyle or
                LoogaTextStyle;
        }

        private static List<T> Collect<T>(IReadOnlyList<UnityEngine.Object> assets)
            where T : LoogaDesignAsset
        {
            List<T> result = new();
            HashSet<T> unique = new();
            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] is T token && unique.Add(token))
                {
                    result.Add(token);
                }
            }

            result.Sort((left, right) => string.Compare(
                left.name,
                right.name,
                StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static int CountEntries(SerializedObject serializedSystem, string propertyName)
        {
            SerializedProperty property = serializedSystem.FindProperty(propertyName);
            return property != null && property.isArray ? property.arraySize : 0;
        }

        private static int CountRetained<T>(
            SerializedObject serializedSystem,
            string propertyName,
            IReadOnlyCollection<T> expected)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serializedSystem.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return 0;
            }

            HashSet<T> expectedSet = new(expected);
            HashSet<T> retained = new();
            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is T token &&
                    expectedSet.Contains(token))
                {
                    retained.Add(token);
                }
            }

            return retained.Count;
        }

        private static void SetArray<T>(
            SerializedObject serializedSystem,
            string propertyName,
            IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serializedSystem.FindProperty(propertyName);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
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
