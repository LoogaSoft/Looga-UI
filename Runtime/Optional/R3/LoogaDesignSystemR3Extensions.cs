#if LOOGA_UI_R3_SUPPORT
using System;
using global::R3;
using LoogaSoft.UI.Design;

namespace LoogaSoft.UI.Extensions.R3
{
    /// <summary>Exposes design-system and scope changes as R3 observables.</summary>
    public static class LoogaDesignSystemR3Extensions
    {
        /// <summary>Returns the global design-system change stream.</summary>
        public static Observable<LoogaDesignSystemChange> DesignChangesAsObservable()
        {
            return Observable.FromEvent<Action<LoogaDesignSystemChange>, LoogaDesignSystemChange>(
                handler => new Action<LoogaDesignSystemChange>(handler),
                handler => LoogaDesignSystemEvents.Changed += handler,
                handler => LoogaDesignSystemEvents.Changed -= handler);
        }

        /// <summary>Returns the active theme stream for one scope.</summary>
        public static Observable<LoogaTheme> ThemeAsObservable(this LoogaThemeScope scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            return Observable.Create<LoogaTheme, LoogaThemeScope>(scope, static (observer, value) =>
            {
                observer.OnNext(value.ActiveTheme);
                return Observable.FromEvent<Action<LoogaTheme>, LoogaTheme>(
                        handler => new Action<LoogaTheme>(handler),
                        handler => value.ThemeChanged += handler,
                        handler => value.ThemeChanged -= handler)
                    .Subscribe(observer);
            }).DistinctUntilChanged();
        }

        /// <summary>Returns a signal after the binding applies its style.</summary>
        public static Observable<Unit> StyleAppliedAsObservable(this LoogaStyleBinding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            return Observable.FromEvent<Action, Unit>(
                handler => () => handler(Unit.Default),
                handler => binding.StyleApplied += handler,
                handler => binding.StyleApplied -= handler);
        }

        /// <summary>Returns a signal after a split binding applies its style.</summary>
        public static Observable<Unit> StyleAppliedAsObservable(this LoogaStyleBindingBase binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            return Observable.FromEvent<Action, Unit>(
                handler => () => handler(Unit.Default),
                handler => binding.StyleApplied += handler,
                handler => binding.StyleApplied -= handler);
        }
    }
}
#endif
