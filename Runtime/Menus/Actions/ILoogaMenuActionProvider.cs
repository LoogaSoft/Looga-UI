using System.Collections.Generic;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Contributes actions while the provider's containing menu panel is visible.
    /// </summary>
    public interface ILoogaMenuActionProvider
    {
        void CollectMenuActions(List<LoogaMenuActionDescriptor> actions);
    }
}
