using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuCommandType
    {
        OpenScreen = 0,
        SwitchLayout = 1,
        Back = 2,
        CloseAll = 3
    }

    /// <summary>Defines one common menu operation for buttons and project integrations.</summary>
    [Serializable]
    public sealed class LoogaMenuCommand
    {
        [SerializeField] private LoogaMenuCommandType _type;
        [SerializeField] private LoogaMenuDestination _target = new();

        public LoogaMenuCommandType Type => _type;
        public LoogaMenuDestination Target => _target;

        public bool Execute(
            LoogaMenuRoot root,
            UnityEngine.Object requester = null,
            object payload = null)
        {
            if (root == null)
                return false;

            switch (_type)
            {
                case LoogaMenuCommandType.SwitchLayout:
                    return _target != null
                        && _target.Screen != null
                        && _target.Layout != null
                        && root.SetLayout(_target.Screen, _target.Layout, requester);
                case LoogaMenuCommandType.Back:
                    return root.Back();
                case LoogaMenuCommandType.CloseAll:
                    root.CloseAll();
                    return true;
                default:
                    return _target != null && _target.Open(root, requester, payload);
            }
        }
    }
}
