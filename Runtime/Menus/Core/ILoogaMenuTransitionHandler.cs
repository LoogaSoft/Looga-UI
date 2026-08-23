using System;

namespace LoogaSoft.Menu
{
    public interface ILoogaMenuTransitionHandler
    {
        void PlayOpen(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels);
        void PlayClose(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels, Action onComplete);
    }
}
