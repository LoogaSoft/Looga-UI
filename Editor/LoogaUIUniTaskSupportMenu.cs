using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor
{
    internal static class LoogaUIUniTaskSupportMenu
    {
        const string MenuPath = "LoogaSoft/UI/Enable UniTask Support";
        const string DefineSymbol = "LOOGA_UI_UNITASK_SUPPORT";
        const string RuntimeAsmdefName = "LoogaSoft.UI.Extensions.Runtime";

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
            return LoogaUIOptionalSupportUtility.DefineIsEnabled(DefineSymbol);
        }

        static void Enable()
        {
            if (!LoogaUIOptionalSupportUtility.SetAsmdefReferences(RuntimeAsmdefName, RequiredAssemblies, include: true, out string error))
            {
                EditorUtility.DisplayDialog("Unable To Update UI FX", error, "OK");
                return;
            }

            LoogaUIOptionalSupportUtility.AddDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI UniTask support enabled.");
        }

        static void Disable()
        {
            if (!LoogaUIOptionalSupportUtility.SetAsmdefReferences(RuntimeAsmdefName, RequiredAssemblies, include: false, out string error))
            {
                EditorUtility.DisplayDialog("Unable To Update UI FX", error, "OK");
                return;
            }

            LoogaUIOptionalSupportUtility.RemoveDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga UI UniTask support disabled.");
        }
    }
}
