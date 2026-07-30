using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;

namespace LoogaSoft.UI.Extensions.Editor
{
    internal static class LoogaUIOptionalSupportUtility
    {
        public static bool AllAssembliesAreAvailable(IReadOnlyList<string> assemblyNames, out string missingAssemblies)
        {
            string[] missing = assemblyNames
                .Where(assemblyName => !AssemblyIsAvailable(assemblyName))
                .ToArray();

            missingAssemblies = string.Join(", ", missing);
            return missing.Length == 0;
        }

        public static bool DefineIsEnabled(string defineSymbol)
        {
            return GetDefines().Contains(defineSymbol);
        }

        public static void AddDefineSymbol(string defineSymbol)
        {
            List<string> defines = GetDefines();
            if (defines.Contains(defineSymbol))
            {
                return;
            }

            defines.Add(defineSymbol);
            ApplyDefineSymbols(defines);
        }

        public static void RemoveDefineSymbol(string defineSymbol)
        {
            List<string> defines = GetDefines();
            if (!defines.Remove(defineSymbol))
            {
                return;
            }

            ApplyDefineSymbols(defines);
        }

        public static bool SetAsmdefReferences(string asmdefName, IReadOnlyList<string> assemblyNames, bool include, out string error)
        {
            error = string.Empty;

            if (!TryGetAsmdefPath(asmdefName, out string path))
            {
                error = $"Could not find {asmdefName}.asmdef.";
                return false;
            }

            if (!PackageIsEditable(path, out error))
            {
                return false;
            }

            try
            {
                string filePath = ResolveFileSystemPath(path);
                string json = File.ReadAllText(filePath);
                AsmdefData asmdef = UnityEngine.JsonUtility.FromJson<AsmdefData>(json);
                List<string> references = asmdef.references != null ? asmdef.references.ToList() : new List<string>();
                bool changed = false;

                foreach (string assemblyName in assemblyNames)
                {
                    if (include)
                    {
                        if (references.Contains(assemblyName))
                        {
                            continue;
                        }

                        references.Add(assemblyName);
                        changed = true;
                    }
                    else
                    {
                        changed |= references.RemoveAll(reference => reference == assemblyName) > 0;
                    }
                }

                if (changed)
                {
                    asmdef.references = references.ToArray();
                    File.WriteAllText(filePath, UnityEngine.JsonUtility.ToJson(asmdef, prettyPrint: true));
                }

                return true;
            }
            catch (Exception exception)
            {
                error = $"Could not update {asmdefName}.asmdef. If this package is installed from an immutable PackageCache location, embed the package or edit the source package before enabling optional support.\n\n{exception.Message}";
                return false;
            }
        }

        static bool AssemblyIsAvailable(string assemblyName)
        {
            if (CompilationPipeline.GetAssemblies().Any(assembly => assembly.name == assemblyName))
            {
                return true;
            }

            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == assemblyName))
            {
                return true;
            }

            return AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset").Length > 0;
        }

        static bool PackageIsEditable(string assetPath, out string error)
        {
            error = string.Empty;
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            if (packageInfo == null)
            {
                return true;
            }

            if (packageInfo.source == UnityEditor.PackageManager.PackageSource.Embedded || packageInfo.source == UnityEditor.PackageManager.PackageSource.Local)
            {
                return true;
            }

            error = $"{packageInfo.displayName} is installed as {packageInfo.source}. Optional support can only modify asmdefs for embedded or local packages. Embed the package, or edit the source package and update the package version, before enabling this support toggle.";
            return false;
        }

        static bool TryGetAsmdefPath(string asmdefName, out string path)
        {
            string[] guids = AssetDatabase.FindAssets($"{asmdefName} t:AssemblyDefinitionAsset");
            foreach (string guid in guids)
            {
                string candidate = AssetDatabase.GUIDToAssetPath(guid);
                if (candidate.EndsWith($"{asmdefName}.asmdef", StringComparison.Ordinal))
                {
                    path = candidate;
                    return true;
                }
            }

            path = string.Empty;
            return false;
        }

        static string ResolveFileSystemPath(string assetPath)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                string prefix = "Packages/" + packageInfo.name + "/";
                if (assetPath.StartsWith(prefix, StringComparison.Ordinal))
                {
                    string relativePath = assetPath.Substring(prefix.Length).Replace('/', Path.DirectorySeparatorChar);
                    return Path.Combine(packageInfo.resolvedPath, relativePath);
                }
            }

            return assetPath;
        }

        static List<string> GetDefines()
        {
            return PlayerSettings.GetScriptingDefineSymbols(GetNamedBuildTarget())
                .Split(';')
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Distinct()
                .ToList();
        }

        static void ApplyDefineSymbols(List<string> defineSymbols)
        {
            string newDefines = string.Join(";", defineSymbols.Distinct().ToArray());
            PlayerSettings.SetScriptingDefineSymbols(GetNamedBuildTarget(), newDefines);
        }

        static NamedBuildTarget GetNamedBuildTarget()
        {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            return NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(activeBuildTarget));
        }

        [Serializable]
        sealed class AsmdefData
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public VersionDefine[] versionDefines;
            public bool noEngineReferences;
        }

        [Serializable]
        sealed class VersionDefine
        {
            public string name;
            public string expression;
            public string define;
        }
    }
}
