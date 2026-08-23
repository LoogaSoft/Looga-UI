using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Lets an authored region presenter render transient editor-preview content.</summary>
    public interface ILoogaMenuPreviewPresenter
    {
        /// <summary>Gets the shared slot presented by this object.</summary>
        LoogaMenuRegionDefinition Region { get; }

        /// <summary>Renders content without changing authored scene data.</summary>
        void ApplyMenuPreview(LoogaMenuRegionContent content);

        /// <summary>Removes transient preview content.</summary>
        void ClearMenuPreview();
    }

    /// <summary>
    /// Activates an authored region presenter when the menu resolves content for its shared slot.
    /// Keep this component on an active parent so the presenter itself can remain inactive at rest.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Region Object Activator")]
    public sealed class LoogaMenuRegionObjectActivator : MonoBehaviour
    {
        [InspectorName("Shared Slot")]
        [SerializeField] private LoogaMenuRegionDefinition _region;

        [Tooltip("The presenter root that this component activates when the shared slot has content.")]
        [SerializeField] private GameObject _target;

        private LoogaMenuManager _manager;

        /// <summary>Gets the shared slot controlled by this activator.</summary>
        public LoogaMenuRegionDefinition Region => _region;

        /// <summary>Gets the presenter root controlled by this activator.</summary>
        public GameObject Target => _target;

        private void OnEnable()
        {
            LoogaMenuRoot.ActiveChanged += OnActiveRootChanged;
            TryBindManager();
            Refresh();
        }

        private void OnDisable()
        {
            LoogaMenuRoot.ActiveChanged -= OnActiveRootChanged;
            UnbindManager();
        }

        private void OnValidate()
        {
            if (_target == gameObject)
            {
                Debug.LogWarning(
                    "A menu region object activator must be outside the target that it controls.",
                    this);
            }
        }

        private void TryBindManager()
        {
            LoogaMenuManager manager = LoogaMenuRoot.Active?.MenuManager;
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
        }

        private void OnMenuStateChanged(LoogaMenuState state)
        {
            _ = state;
            Refresh();
        }

        private void OnActiveRootChanged(LoogaMenuRoot root)
        {
            _ = root;
            TryBindManager();
        }

        private void Refresh()
        {
            if (_target == null || _target == gameObject)
                return;

            bool shouldBeActive = _manager?.ResolveRegionContent(_region) != null;
            if (_target.activeSelf != shouldBeActive)
                _target.SetActive(shouldBeActive);
        }
    }
}
