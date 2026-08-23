using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Identifies a screen destination and an optional layout within that screen.</summary>
    [Serializable]
    public sealed class LoogaMenuDestination
    {
        [SerializeField] private LoogaMenuScreenDefinition _screen;
        [SerializeField] private LoogaMenuScreenLayout _layout;
        [SerializeField] private LoogaMenuOpenMode _openMode = LoogaMenuOpenMode.Replace;

        public LoogaMenuScreenDefinition Screen => _screen;
        public LoogaMenuScreenLayout Layout => _layout;
        public LoogaMenuOpenMode OpenMode => _openMode;
        public bool IsAssigned => _screen != null;

        /// <summary>Creates a typed menu destination for generated presentation.</summary>
        public static LoogaMenuDestination Create(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuOpenMode openMode)
        {
            return new LoogaMenuDestination
            {
                _screen = screen,
                _layout = layout,
                _openMode = openMode
            };
        }

        public bool Open(LoogaMenuRoot root, UnityEngine.Object requester = null, object payload = null)
        {
            return root != null && root.Open(this, requester, payload);
        }

        internal bool Matches(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout)
        {
            if (_screen != screen)
                return false;

            LoogaMenuScreenLayout expected = _screen != null ? _screen.ResolveLayout(_layout) : null;
            return expected == layout;
        }
    }
}
