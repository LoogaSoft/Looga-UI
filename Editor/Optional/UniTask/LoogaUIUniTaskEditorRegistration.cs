using UnityEditor;

namespace LoogaSoft.UI.Extensions.UniTask.Editor
{
    internal static class LoogaUIUniTaskEditorRegistration
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            LoogaUIUniTaskScheduler.Register();
        }
    }
}
