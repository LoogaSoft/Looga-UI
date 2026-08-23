#if LOOGA_MENU_R3_SUPPORT
using System;
using System.Collections.Generic;
using System.Threading;
using ObservableCollections;
using R3;

namespace LoogaSoft.Menu
{
    public static class LoogaMenuR3Extensions
    {
        public static Observable<LoogaMenuState> StateChangedAsObservable(
            this LoogaMenuRoot root,
            CancellationToken cancellationToken = default)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (root.MenuManager == null)
                throw new InvalidOperationException("The menu root has not initialized its menu manager yet.");

            return root.MenuManager.StateChangedAsObservable(cancellationToken);
        }

        public static Observable<LoogaMenuState> StateChangedAsObservable(
            this LoogaMenuManager manager,
            CancellationToken cancellationToken = default)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            return Observable.FromEvent<Action<LoogaMenuState>, LoogaMenuState>(
                handler => new Action<LoogaMenuState>(handler),
                handler => manager.StateChanged += handler,
                handler => manager.StateChanged -= handler,
                cancellationToken);
        }

        public static ObservableList<LoogaMenuScreenDefinition> CreateObservableOpenScreens(
            this LoogaMenuRoot root,
            CancellationToken cancellationToken = default)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (root.MenuManager == null)
                throw new InvalidOperationException("The menu root has not initialized its menu manager yet.");

            return root.MenuManager.CreateObservableOpenScreens(cancellationToken);
        }

        public static ObservableList<LoogaMenuScreenDefinition> CreateObservableOpenScreens(
            this LoogaMenuManager manager,
            CancellationToken cancellationToken = default)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            ObservableList<LoogaMenuScreenDefinition> openScreens = new();
            CopyOpenScreens(manager.OpenScreens, openScreens);

            void HandleStateChanged(LoogaMenuState state)
            {
                CopyOpenScreens(state.OpenScreens, openScreens);
            }

            manager.StateChanged += HandleStateChanged;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => manager.StateChanged -= HandleStateChanged);
            }

            return openScreens;
        }

        private static void CopyOpenScreens(
            IReadOnlyList<LoogaMenuScreenDefinition> source,
            ObservableList<LoogaMenuScreenDefinition> target)
        {
            if (OpenScreensMatch(source, target))
                return;

            target.Clear();

            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }
        }

        private static bool OpenScreensMatch(
            IReadOnlyList<LoogaMenuScreenDefinition> source,
            ObservableList<LoogaMenuScreenDefinition> target)
        {
            int sourceCount = source?.Count ?? 0;
            if (sourceCount != target.Count)
                return false;

            for (int i = 0; i < sourceCount; i++)
            {
                if (!ReferenceEquals(source[i], target[i]))
                    return false;
            }

            return true;
        }
    }
}
#endif