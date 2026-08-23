namespace LoogaSoft.Menu
{
    public interface ILoogaMenuAudioHandler
    {
        void PlayOpen(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels);
        void PlayClose(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels);
    }
}
