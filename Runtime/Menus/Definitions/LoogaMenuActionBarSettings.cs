using System;
using System.Collections.Generic;

namespace LoogaSoft.Menu
{
    /// <summary>Provides the actions shown by the shared menu action-bar view.</summary>
    public interface ILoogaMenuActionBar
    {
        IReadOnlyList<LoogaMenuActionDescriptor> Actions { get; }
        event Action ActionsChanged;
        void RefreshActions();
    }

}
