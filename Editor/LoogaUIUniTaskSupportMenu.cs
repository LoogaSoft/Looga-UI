using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor
{
    internal static class LoogaUIUniTaskSupportProvider
    {
        const string DisableDefineSymbol = "LOOGA_UI_DISABLE_UNITASK_SUPPORT";

        static readonly string[] RequiredAssemblies =
        {
            "UniTask"
        };

        public static string ProviderId => "looga-ui.unitask";
        public static string PackageName => "Looga UI";
        public static string IntegrationName => "UniTask";
        public static string Description => "Uses UniTask for asynchronous UI effects and scheduling.";

        public static bool IsEnabled()
        {
            return LoogaUIOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out _) &&
                   !LoogaUIOptionalSupportUtility.DefineIsEnabled(DisableDefineSymbol);
        }

        public static string GetUnavailableReason()
        {
            return LoogaUIOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out string missingAssemblies)
                ? string.Empty
                : "Install UniTask. Missing assemblies: " + missingAssemblies;
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
                Enable();
            else
                Disable();
        }

        static void Enable()
        {
            LoogaUIOptionalSupportUtility.RemoveDefineSymbol(DisableDefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI UniTask support enabled.");
        }

        static void Disable()
        {
            LoogaUIOptionalSupportUtility.AddDefineSymbol(DisableDefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI UniTask support disabled.");
        }
    }
}
