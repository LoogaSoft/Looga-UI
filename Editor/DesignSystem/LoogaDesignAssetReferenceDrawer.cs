using System;
using System.Collections.Generic;
using System.IO;
using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Adds creation actions to references for Looga design assets.</summary>
    [CustomPropertyDrawer(typeof(LoogaDesignAsset), true)]
    internal sealed class LoogaDesignAssetReferenceDrawer : PropertyDrawer
    {
        private const float NewButtonWidth = 38f;
        private const float CloneButtonWidth = 45f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Type assetType = ResolveAssetType();
            if (assetType == null || !typeof(LoogaDesignAsset).IsAssignableFrom(assetType))
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            Rect cloneRect = TakeRight(ref contentRect, CloneButtonWidth);
            Rect newRect = TakeRight(ref contentRect, NewButtonWidth);

            EditorGUI.BeginChangeCheck();
            UnityEngine.Object value = EditorGUI.ObjectField(
                contentRect,
                GUIContent.none,
                property.objectReferenceValue,
                assetType,
                false);
            if (EditorGUI.EndChangeCheck())
            {
                property.objectReferenceValue = value;
            }

            bool multipleTargets = property.serializedObject.isEditingMultipleObjects;
            bool canCreate = !multipleTargets && !assetType.IsAbstract;
            using (new EditorGUI.DisabledScope(!canCreate))
            {
                GUIContent newContent = new GUIContent("New", $"Create and assign a new {GetAssetKind(assetType)}.");
                if (GUI.Button(newRect, newContent, EditorStyles.miniButton))
                {
                    CreateAndAssign(property, assetType, null);
                }
            }

            bool canClone = !multipleTargets && property.objectReferenceValue is LoogaDesignAsset;
            using (new EditorGUI.DisabledScope(!canClone))
            {
                GUIContent cloneContent = new GUIContent("Clone", "Duplicate and assign the current asset.");
                if (GUI.Button(cloneRect, cloneContent, EditorStyles.miniButton))
                {
                    CreateAndAssign(property, assetType, property.objectReferenceValue as LoogaDesignAsset);
                }
            }

            EditorGUI.EndProperty();
        }

        private static Rect TakeRight(ref Rect available, float width)
        {
            Rect result = new Rect(available.xMax - width, available.y, width, available.height);
            available.width -= width;
            return result;
        }

        private Type ResolveAssetType()
        {
            Type type = fieldInfo?.FieldType;
            if (type == null)
            {
                return null;
            }

            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        private static void CreateAndAssign(
            SerializedProperty property,
            Type assetType,
            LoogaDesignAsset source)
        {
            LoogaDesignAsset created = source != null
                ? UnityEngine.Object.Instantiate(source)
                : ScriptableObject.CreateInstance(assetType) as LoogaDesignAsset;
            if (created == null)
            {
                return;
            }

            created.name = BuildAssetName(property, assetType, source);
            string undoName = source != null ? "Clone Design Asset" : "Create Design Asset";

            if (!TryStoreAsset(property, created, source))
            {
                UnityEngine.Object.DestroyImmediate(created);
                return;
            }

            Undo.RegisterCreatedObjectUndo(created, undoName);
            property.objectReferenceValue = created;
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();
        }

        private static bool TryStoreAsset(
            SerializedProperty property,
            LoogaDesignAsset created,
            LoogaDesignAsset source)
        {
            UnityEngine.Object container = FindSubAssetContainer(property, source, created.GetType());
            if (container != null)
            {
                string path = AssetDatabase.GetAssetPath(container);
                created.name = MakeUniqueSubAssetName(path, created.name);
                AssetDatabase.AddObjectToAsset(created, container);
                return true;
            }

            string assetPath = ResolveStandalonePath(source, created.name);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            AssetDatabase.CreateAsset(created, assetPath);
            return true;
        }

        private static UnityEngine.Object FindSubAssetContainer(
            SerializedProperty property,
            LoogaDesignAsset source,
            Type assetType)
        {
            if (assetType == typeof(LoogaDesignSystem))
            {
                return null;
            }

            if (source != null && AssetDatabase.IsSubAsset(source))
            {
                return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(source));
            }

            if (source != null && AssetDatabase.IsMainAsset(source))
            {
                return null;
            }

            UnityEngine.Object owner = property.serializedObject.targetObject;
            if (owner is LoogaDesignAsset && AssetDatabase.Contains(owner))
            {
                return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(owner));
            }

            LoogaDesignSystem designSystem = FindDesignSystem(owner);
            return designSystem != null && AssetDatabase.Contains(designSystem)
                ? designSystem
                : null;
        }

        private static LoogaDesignSystem FindDesignSystem(UnityEngine.Object owner)
        {
            if (owner is Component component)
            {
                LoogaThemeScope scope = LoogaThemeScope.FindNearest(component.transform);
                if (scope?.DesignSystem != null)
                {
                    return scope.DesignSystem;
                }
            }

            LoogaDesignSystem loaded = LoogaDesignSystemConsolidator.FindLoadedDesignSystem();
            if (loaded != null)
            {
                return loaded;
            }

            string[] guids = AssetDatabase.FindAssets("t:LoogaDesignSystem");
            if (guids.Length != 1)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LoogaDesignSystem>(path);
        }

        private static string ResolveStandalonePath(LoogaDesignAsset source, string assetName)
        {
            if (source != null && AssetDatabase.IsMainAsset(source))
            {
                string sourcePath = AssetDatabase.GetAssetPath(source);
                string folder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(folder))
                {
                    return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{SanitizeFileName(assetName)}.asset");
                }
            }

            return EditorUtility.SaveFilePanelInProject(
                "Create Design Asset",
                SanitizeFileName(assetName),
                "asset",
                "Choose where to save the new design asset.");
        }

        private static string BuildAssetName(
            SerializedProperty property,
            Type assetType,
            LoogaDesignAsset source)
        {
            if (source != null)
            {
                return $"{source.name} Copy";
            }

            string context = property.serializedObject.targetObject switch
            {
                Component component => component.gameObject.name,
                LoogaDesignAsset designAsset => designAsset.name,
                _ => property.displayName
            };
            return $"{context} {GetAssetKind(assetType)}";
        }

        private static string GetAssetKind(Type assetType)
        {
            string name = assetType.Name;
            if (name.StartsWith("Looga", StringComparison.Ordinal))
            {
                name = name.Substring("Looga".Length);
            }

            return ObjectNames.NicifyVariableName(name);
        }

        private static string MakeUniqueSubAssetName(string path, string requestedName)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                names.Add(assets[i].name);
            }

            if (names.Add(requestedName))
            {
                return requestedName;
            }

            for (int suffix = 2; suffix < int.MaxValue; suffix++)
            {
                string candidate = $"{requestedName} {suffix}";
                if (names.Add(candidate))
                {
                    return candidate;
                }
            }

            return requestedName;
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                value = value.Replace(invalidCharacters[i], '_');
            }

            return value;
        }
    }
}
