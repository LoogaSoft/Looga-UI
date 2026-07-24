using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UIFX.Editor
{
    internal static class LoogaUIFXUniTaskSupportMenu
    {
        const string MenuPath = "LoogaSoft/UI FX/Enable UniTask Support";
        const string DefineSymbol = "LOOGA_UIFX_UNITASK_SUPPORT";
        const string RuntimeAsmdefName = "LoogaSoft.UIFX.Runtime";

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

            if (!LoogaUIFXOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out string missingAssemblies))
            {
                EditorUtility.DisplayDialog("UniTask Not Found", "Install UniTask before enabling Looga UI FX UniTask support.\n\nMissing: " + missingAssemblies, "OK");
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
            return LoogaUIFXOptionalSupportUtility.DefineIsEnabled(DefineSymbol);
        }

        static void Enable()
        {
            if (!LoogaUIFXOptionalSupportUtility.SetAsmdefReferences(RuntimeAsmdefName, RequiredAssemblies, include: true, out string error))
            {
                EditorUtility.DisplayDialog("Unable To Update UI FX", error, "OK");
                return;
            }

            LoogaUIFXOptionalSupportUtility.AddDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI FX UniTask support enabled.");
        }

        static void Disable()
        {
            if (!LoogaUIFXOptionalSupportUtility.SetAsmdefReferences(RuntimeAsmdefName, RequiredAssemblies, include: false, out string error))
            {
                EditorUtility.DisplayDialog("Unable To Update UI FX", error, "OK");
                return;
            }

            LoogaUIFXOptionalSupportUtility.RemoveDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI FX UniTask support disabled.");
        }
    }
}
