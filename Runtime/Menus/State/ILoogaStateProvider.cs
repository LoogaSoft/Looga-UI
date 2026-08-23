using LoogaSoft.Blackboard;

namespace LoogaSoft.Menu
{
    public interface ILoogaStateProvider
    {
        void RegisterStates(ILoogaBlackboardWriter blackboard);
        void UnregisterStates(ILoogaBlackboardWriter blackboard);
    }
}
