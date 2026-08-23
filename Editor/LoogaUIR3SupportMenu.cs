using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor
{
    internal static class LoogaUIR3SupportProvider
    {
        private const string MenuDefineSymbol = "LOOGA_MENU_R3_SUPPORT";
        private const string UiDefineSymbol = "LOOGA_UI_R3_SUPPORT";

        private static readonly string[] DefineSymbols =
        {
            MenuDefineSymbol,
            UiDefineSymbol
        };

        private static readonly string[] RequiredAssemblies =
        {
            "R3",
            "R3.Unity",
            "ObservableCollections",
            "ObservableCollections.R3"
        };

        public static string ProviderId => "looga-ui.r3";
        public static string PackageName => "Looga UI";
        public static string IntegrationName => "R3";
        public static string Description =>
            "Adds reactive menu, collection, theme, token, and style adapters through R3.";

        public static bool IsEnabled()
        {
            return LoogaUIOptionalSupportUtility.DefineIsEnabled(MenuDefineSymbol) &&
                   LoogaUIOptionalSupportUtility.DefineIsEnabled(UiDefineSymbol);
        }

        public static string GetUnavailableReason()
        {
            return LoogaUIOptionalSupportUtility.AllAssembliesAreAvailable(
                RequiredAssemblies,
                out string missingAssemblies)
                ? string.Empty
                : "Install R3 and ObservableCollections support. Missing assemblies: " + missingAssemblies;
        }

        public static void SetEnabled(bool enabled)
        {
            LoogaUIOptionalSupportUtility.SetDefineSymbols(DefineSymbols, enabled);

            Debug.Log($"Looga UI R3 integrations {(enabled ? "enabled" : "disabled")}.");
        }
    }
}
