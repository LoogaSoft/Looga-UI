using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor
{
    internal static class LoogaUIUniTaskSupportMenu
    {
        const string MenuPath = "LoogaSoft/UI/Enable UniTask Support";
        const string DisableDefineSymbol = "LOOGA_UI_DISABLE_UNITASK_SUPPORT";

        static readonly string[] RequiredAssemblies =
        {
            "UniTask"
        };

        [MenuItem(MenuPath, priority = 240)]
        static void ToggleUniTaskSupport()
        {
            if (IsEnabled())
            {
                Disable();
                return;
            }

            if (!LoogaUIOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out string missingAssemblies))
            {
                EditorUtility.DisplayDialog("UniTask Not Found", "Install UniTask before enabling Looga UI UniTask support.\n\nMissing: " + missingAssemblies, "OK");
                return;
            }

            Enable();
        }

        [MenuItem(MenuPath, true)]
        static bool ValidateToggle()
        {
            UnityEditor.Menu.SetChecked(MenuPath, IsEnabled());
            return true;
        }

        static bool IsEnabled()
        {
            return LoogaUIOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out _) &&
                   !LoogaUIOptionalSupportUtility.DefineIsEnabled(DisableDefineSymbol);
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
