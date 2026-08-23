using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Presents the actions collected for the active screen and layout.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(LoogaMenuRegionHost))]
    [AddComponentMenu("LoogaSoft/Menu/Action Bar View")]
    public sealed class LoogaMenuActionBarView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private bool _useActiveMenuRoot = true;
        [HideIf(nameof(_useActiveMenuRoot))]
        [SerializeField] private LoogaMenuRoot _menuRoot;

        [Header("Presentation")]
        [SerializeField] private RectTransform _actionParent;
        [SerializeField] private LoogaMenuActionBarItemView _itemTemplate;
        [SerializeField] private bool _hideWhenEmpty = true;

        private readonly List<LoogaMenuActionBarItemView> _items = new();
        private LoogaMenuManager _menuManager;
        private ILoogaMenuActionBar _actionBar;
        private CanvasGroup _canvasGroup;
        private LoogaMenuRegionHost _regionHost;

        private void Awake()
        {
            ResolveReferences();
            HideTemplate();
        }

        private void OnEnable()
        {
            ResolveReferences();
            HideTemplate();
            if (_useActiveMenuRoot)
            {
                LoogaMenuRoot.ActiveChanged += OnActiveRootChanged;
            }

            RebindManager();
            Refresh();
        }

        private void OnDisable()
        {
            LoogaMenuRoot.ActiveChanged -= OnActiveRootChanged;
            Unsubscribe();
        }

        public void Refresh()
        {
            BindActionBar();
            if (_actionBar != null)
            {
                _actionBar.RefreshActions();
                return;
            }

            Render();
        }

        private void RebindManager()
        {
            LoogaMenuRoot root = ResolveRoot();
            LoogaMenuManager manager = root != null ? root.MenuManager : null;
            if (manager == _menuManager)
                return;

            Unsubscribe();
            _menuManager = manager;
            if (_menuManager != null)
            {
                _menuManager.StateChanged += OnMenuStateChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_menuManager != null)
            {
                _menuManager.StateChanged -= OnMenuStateChanged;
            }

            if (_actionBar != null)
            {
                _actionBar.ActionsChanged -= Render;
            }

            _actionBar = null;
            _menuManager = null;
        }

        private void BindActionBar()
        {
            ILoogaMenuActionBar next = null;
            if (_regionHost != null)
                _menuManager?.TryGetActionBar(_regionHost.Region, out next);
            if (ReferenceEquals(_actionBar, next))
                return;

            if (_actionBar != null)
            {
                _actionBar.ActionsChanged -= Render;
            }

            _actionBar = next;
            if (_actionBar != null)
            {
                _actionBar.ActionsChanged += Render;
            }
        }

        private void OnMenuStateChanged(LoogaMenuState state)
        {
            _ = state;
            Refresh();
        }

        private void OnActiveRootChanged(LoogaMenuRoot root)
        {
            _ = root;
            RebindManager();
            Refresh();
        }

        private void Render()
        {
            IReadOnlyList<LoogaMenuActionDescriptor> actions = _actionBar?.Actions;
            int count = actions?.Count ?? 0;
            EnsureItemCapacity(count);

            for (int i = 0; i < _items.Count; i++)
            {
                bool active = i < count;
                _items[i].gameObject.SetActive(active);
                if (active)
                {
                    _items[i].Bind(actions[i]);
                }
            }

            if (_hideWhenEmpty && _canvasGroup != null)
            {
                bool visible = count > 0;
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }

            RebuildLayout();
        }

        private void EnsureItemCapacity(int count)
        {
            if (_itemTemplate == null || _actionParent == null)
                return;

            while (_items.Count < count)
            {
                LoogaMenuActionBarItemView item = Instantiate(_itemTemplate, _actionParent);
                item.name = _itemTemplate.name;
                item.gameObject.SetActive(false);
                _items.Add(item);
            }
        }

        private LoogaMenuRoot ResolveRoot()
        {
            if (!_useActiveMenuRoot && _menuRoot != null)
                return _menuRoot;

            return LoogaMenuRoot.Active;
        }

        private void ResolveReferences()
        {
            _actionParent ??= transform as RectTransform;
            _canvasGroup ??= GetComponent<CanvasGroup>();
            _regionHost ??= GetComponent<LoogaMenuRegionHost>();
            if (_itemTemplate == null && _actionParent != null)
            {
                _itemTemplate = _actionParent.GetComponentInChildren<LoogaMenuActionBarItemView>(true);
            }
        }

        private void HideTemplate()
        {
            if (_itemTemplate != null)
            {
                _itemTemplate.gameObject.SetActive(false);
            }
        }

        private void RebuildLayout()
        {
            if (_actionParent == null)
                return;

            LayoutRebuilder.MarkLayoutForRebuild(_actionParent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_actionParent);
        }
    }
}
