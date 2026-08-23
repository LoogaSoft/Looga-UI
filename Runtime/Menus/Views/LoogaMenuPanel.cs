using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("LoogaSoft/Menu/Menu Panel")]
    public sealed class LoogaMenuPanel : MonoBehaviour
    {
        [TooltipBox("Assign the panel definition this scene object represents. Screens enable this panel through that asset reference.")]
        [ExposeScriptable(showScriptField: false, createButtonLabel: "New")]
        [SerializeField] private LoogaMenuPanelDefinition _panel;

        private CanvasGroup _canvasGroup;
        private readonly List<ILoogaMenuActionProvider> _actionProviders = new();
        private bool _actionProvidersDirty = true;
        private bool _isCovered;
        private bool _isVisible;

        public LoogaMenuPanelDefinition Panel => _panel;
        public bool IsCovered => _isCovered;
        public bool IsVisible => _isVisible;

        public event Action<bool> CoveredChanged;
        public event Action<bool> VisibilityChanged;
        public event Action ActionsChanged;

        public CanvasGroup CanvasGroup
        {
            get
            {
                ResolveReferences();
                return _canvasGroup;
            }
        }

        public RectTransform RectTransform => transform as RectTransform;

        private void Awake()
        {
            ResolveReferences(true);
        }

        private void OnValidate()
        {
            _actionProvidersDirty = true;
            ResolveReferences(false);
        }

        private void OnTransformChildrenChanged()
        {
            _actionProvidersDirty = true;
            ActionsChanged?.Invoke();
        }

        public void Show()
        {
            ResolveReferences(true);
            CacheActionProviders();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Visibility is owned by the panel GameObject. Restore an authored Canvas that
            // may still be disabled by scenes saved with the former multi-state workflow.
            if (TryGetComponent(out Canvas canvas) && !canvas.enabled)
            {
                canvas.enabled = true;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            SetVisible(true);
            SetCoveredState(false);
        }

        public void Hide()
        {
            SetVisible(false);
            SetCoveredState(false);

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void SetCovered(bool covered)
        {
            ResolveReferences(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = !covered;
                _canvasGroup.blocksRaycasts = !covered;
            }

            SetCoveredState(covered);
        }

        /// <summary>
        /// Collects actions from providers contained by this panel without searching the scene.
        /// </summary>
        public void CollectMenuActions(List<LoogaMenuActionDescriptor> actions)
        {
            if (actions == null)
                return;

            CacheActionProviders();
            foreach (ILoogaMenuActionProvider provider in _actionProviders)
            {
                provider?.CollectMenuActions(actions);
            }
        }

        /// <summary>
        /// Requests that the active action bar recollect this panel's contextual actions.
        /// </summary>
        public void NotifyActionsChanged()
        {
            ActionsChanged?.Invoke();
        }

        private void SetCoveredState(bool covered)
        {
            if (_isCovered == covered)
                return;

            _isCovered = covered;
            CoveredChanged?.Invoke(covered);
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible)
                return;

            _isVisible = visible;
            VisibilityChanged?.Invoke(visible);
        }

        private void ResolveReferences()
        {
            ResolveReferences(true);
        }

        private void ResolveReferences(bool logMissingComponents)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (logMissingComponents && _canvasGroup == null)
            {
                Debug.LogWarning($"{name} is missing a {nameof(CanvasGroup)}. Add one to the menu panel object.", this);
            }

        }

        private void CacheActionProviders()
        {
            if (!_actionProvidersDirty)
                return;

            _actionProviders.Clear();
            foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ILoogaMenuActionProvider provider)
                {
                    _actionProviders.Add(provider);
                }
            }

            _actionProvidersDirty = false;
        }
    }
}
