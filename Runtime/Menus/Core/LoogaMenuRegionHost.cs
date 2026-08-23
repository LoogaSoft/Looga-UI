using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Binds one scene presenter to a typed region definition.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Region Host")]
    public sealed class LoogaMenuRegionHost : MonoBehaviour
    {
        [InspectorName("Shared Slot")]
        [SerializeField] private LoogaMenuRegionDefinition _region;

        private LoogaMenuManager _manager;
        private LoogaMenuRegionContent _content;

        public event Action<LoogaMenuRegionContent> ContentChanged;

        public LoogaMenuRegionDefinition Region => _region;
        public LoogaMenuRegionContent Content => _content;

        private void OnEnable()
        {
            LoogaMenuRoot.ActiveChanged += OnActiveRootChanged;
            BindManager();
        }

        private void OnDisable()
        {
            LoogaMenuRoot.ActiveChanged -= OnActiveRootChanged;
            UnbindManager();
        }

        public T GetContent<T>() where T : LoogaMenuRegionContent
        {
            return _content as T;
        }

        private void BindManager()
        {
            LoogaMenuManager manager = LoogaMenuRoot.Active != null
                ? LoogaMenuRoot.Active.MenuManager
                : null;
            if (manager == _manager)
                return;

            UnbindManager();
            _manager = manager;
            if (_manager != null)
            {
                _manager.StateChanged += OnMenuStateChanged;
            }

            Refresh();
        }

        private void UnbindManager()
        {
            if (_manager != null)
                _manager.StateChanged -= OnMenuStateChanged;

            _manager = null;
            SetContent(null);
        }

        private void OnMenuStateChanged(LoogaMenuState state)
        {
            _ = state;
            Refresh();
        }

        private void OnActiveRootChanged(LoogaMenuRoot root)
        {
            _ = root;
            BindManager();
        }

        private void Refresh()
        {
            SetContent(_manager?.ResolveRegionContent(_region));
        }

        private void SetContent(LoogaMenuRegionContent content)
        {
            if (_content == content)
                return;

            _content = content;
            ContentChanged?.Invoke(_content);
        }
    }
}
