using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor
{
    internal static class LoogaUIR3SupportProvider
    {
        private const string DefineSymbol = "LOOGA_UI_R3_SUPPORT";
        private static readonly string[] RequiredAssemblies = { "R3" };

        public static string ProviderId => "looga-ui.r3";
        public static string PackageName => "Looga UI";
        public static string IntegrationName => "R3";
        public static string Description => "Adds observable theme, token, and style application adapters.";

        public static bool IsEnabled()
        {
            return LoogaUIOptionalSupportUtility.DefineIsEnabled(DefineSymbol);
        }

        public static string GetUnavailableReason()
        {
            return LoogaUIOptionalSupportUtility.AllAssembliesAreAvailable(
                RequiredAssemblies,
                out string missingAssemblies)
                ? string.Empty
                : "Install R3. Missing assemblies: " + missingAssemblies;
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                LoogaUIOptionalSupportUtility.AddDefineSymbol(DefineSymbol);
            }
            else
            {
                LoogaUIOptionalSupportUtility.RemoveDefineSymbol(DefineSymbol);
            }

            Debug.Log($"Looga UI R3 support {(enabled ? "enabled" : "disabled")}.");
        }
    }
}
