using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuR3SupportProvider
    {
        private const string DefineSymbol = "LOOGA_MENU_R3_SUPPORT";

        private static readonly string[] RequiredAssemblies =
        {
            "R3",
            "R3.Unity",
            "ObservableCollections",
            "ObservableCollections.R3"
        };

        public static string ProviderId => "looga-menu-framework.r3";
        public static string PackageName => "Looga UI";
        public static string IntegrationName => "R3";
        public static string Description => "Adds reactive menu state and collection adapters through R3.";

        public static bool IsEnabled()
        {
            return LoogaMenuOptionalSupportUtility.DefineIsEnabled(DefineSymbol);
        }

        public static string GetUnavailableReason()
        {
            return LoogaMenuOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out string missingAssemblies)
                ? string.Empty
                : "Install R3 and ObservableCollections support. Missing assemblies: " + missingAssemblies;
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
                Enable();
            else
                Disable();
        }

        private static void Enable()
        {
            LoogaMenuOptionalSupportUtility.AddDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI menu R3 support enabled.");
        }

        private static void Disable()
        {
            LoogaMenuOptionalSupportUtility.RemoveDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI menu R3 support disabled.");
        }
    }
}
