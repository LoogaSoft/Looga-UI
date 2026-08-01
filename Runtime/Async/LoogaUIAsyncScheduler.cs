using System;
using System.Threading;
using UnityEngine;

namespace LoogaSoft.UI.Extensions
{
    /// <summary>
    /// Bridges optional async providers into the core package without giving the core assembly
    /// a hard dependency on UniTask or another scheduling library.
    /// </summary>
    public static class LoogaUIAsyncScheduler
    {
        public delegate bool ScheduleHandler(
            Func<Color32[]> work,
            CancellationToken cancellationToken,
            Action<Color32[], Exception, bool> completion);

        private static ScheduleHandler _handler;

        public static bool IsAvailable => _handler != null;

        public static void Register(ScheduleHandler handler)
        {
            _handler = handler;
        }

        public static void Unregister(ScheduleHandler handler)
        {
            if (_handler == handler)
            {
                _handler = null;
            }
        }

        internal static bool TrySchedule(
            Func<Color32[]> work,
            CancellationToken cancellationToken,
            Action<Color32[], Exception, bool> completion)
        {
            return _handler != null && _handler(work, cancellationToken, completion);
        }
    }
}
