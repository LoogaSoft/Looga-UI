using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.UniTask
{
    /// <summary>
    /// Supplies the core package with a UniTask-backed worker when UniTask is installed.
    /// </summary>
    public static class LoogaUIUniTaskScheduler
    {
        private static readonly LoogaUIAsyncScheduler.ScheduleHandler Handler = Schedule;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            LoogaUIAsyncScheduler.Register(Handler);
        }

        private static bool Schedule(
            Func<Color32[]> work,
            CancellationToken cancellationToken,
            Action<Color32[], Exception, bool> completion)
        {
            Run(work, cancellationToken, completion).Forget();
            return true;
        }

        private static async UniTaskVoid Run(
            Func<Color32[]> work,
            CancellationToken cancellationToken,
            Action<Color32[], Exception, bool> completion)
        {
            Color32[] pixels = null;
            Exception failure = null;
            bool cancelled = false;

            try
            {
                pixels = await Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(
                    work,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
            completion(pixels, failure, cancelled);
        }
    }
}
