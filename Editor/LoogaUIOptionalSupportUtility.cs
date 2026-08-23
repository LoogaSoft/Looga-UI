using System;
using System.Collections.Generic;
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
            SetDefineSymbols(new[] { defineSymbol }, true);
        }

        public static void RemoveDefineSymbol(string defineSymbol)
        {
            SetDefineSymbols(new[] { defineSymbol }, false);
        }

        public static void SetDefineSymbols(IReadOnlyList<string> defineSymbols, bool enabled)
        {
            List<string> defines = GetDefines();
            bool changed = false;
            for (int i = 0; i < defineSymbols.Count; i++)
            {
                string defineSymbol = defineSymbols[i];
                if (enabled)
                {
                    if (!defines.Contains(defineSymbol))
                    {
                        defines.Add(defineSymbol);
                        changed = true;
                    }
                }
                else
                {
                    changed |= defines.Remove(defineSymbol);
                }
            }

            if (changed)
                ApplyDefineSymbols(defines);
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
    }
}
