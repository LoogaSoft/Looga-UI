using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Owns menu state, panel registration, shared presentation, and cursor policy.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Root")]
    public sealed class LoogaMenuRoot : MonoBehaviour
    {
        [Header("Registration")]
        [SerializeField] private bool _registerChildrenOnAwake = true;
        [SerializeField] private LoogaMenuPanel[] _scenePanels = System.Array.Empty<LoogaMenuPanel>();

        [Header("Structure")]
        [InspectorName("Menu Project")]
        [SerializeField] private LoogaMenuStructureProfile _structure;
        [SerializeField] private LoogaMenuContextDefinition _defaultContext;

        [Header("Cursor")]
        [SerializeField] private bool _controlCursor = true;
        [SerializeField] private CursorLockMode _closedLockMode = CursorLockMode.Locked;
        [SerializeField] private bool _closedCursorVisible;

        private LoogaBlackboard _ownedBlackboard;
        private ILoogaBlackboardReader _blackboardReader;
        private ILoogaBlackboardWriter _blackboardWriter;
        private LoogaMenuManager _menuManager;
        private bool _hadOpenScreens;

        /// <summary>Gets the active menu root.</summary>
        public static LoogaMenuRoot Active { get; private set; }

        /// <summary>Occurs when the active menu root changes.</summary>
        public static event System.Action<LoogaMenuRoot> ActiveChanged;

        /// <summary>Gets the menu manager owned by this root.</summary>
        public LoogaMenuManager MenuManager => _menuManager;

        /// <summary>Gets read access to the active menu blackboard.</summary>
        public ILoogaBlackboardReader BlackboardReader => _blackboardReader;

        /// <summary>Gets write access to the active menu blackboard.</summary>
        public ILoogaBlackboardWriter BlackboardWriter => _blackboardWriter;

        /// <summary>Gets the project-authored menu region structure.</summary>
        public LoogaMenuStructureProfile Structure => _structure;

        /// <summary>Gets the context applied when this root initializes.</summary>
        public LoogaMenuContextDefinition DefaultContext => _defaultContext;

        /// <summary>Gets the persistent context currently applied to shared regions.</summary>
        public LoogaMenuContextDefinition ActiveContext => _menuManager?.ActiveContext;

        /// <summary>Applies project-level menu behavior at runtime.</summary>
        public void ApplyRuntimeDefaults(
            bool registerChildrenOnAwake,
            LoogaMenuStructureProfile structure,
            bool controlCursor,
            CursorLockMode closedLockMode,
            bool closedCursorVisible,
            LoogaMenuContextDefinition defaultContext = null)
        {
            _registerChildrenOnAwake = registerChildrenOnAwake;
            _structure = structure;
            _controlCursor = controlCursor;
            _closedLockMode = closedLockMode;
            _closedCursorVisible = closedCursorVisible;
            _defaultContext = defaultContext;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            ActiveChanged = null;
        }

        private void Awake()
        {
            ResolveBlackboard();
            _menuManager = new LoogaMenuManager(
                _blackboardReader,
                _blackboardWriter,
                _structure);
            _menuManager.StateChanged += OnMenuStateChanged;
            SetActive(this);

            RegisterStateProviders();
            ResolveHandlers();
            RegisterPanels();
            if (_defaultContext != null)
            {
                _menuManager.SetContext(_defaultContext);
            }
            else
            {
                _menuManager.RefreshPresentation();
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                SetActive(null);
            }

            if (_menuManager != null)
            {
                _menuManager.StateChanged -= OnMenuStateChanged;
                _menuManager.Dispose();
            }

            UnregisterStateProviders();
            ReleaseOwnedBlackboard();
        }

        /// <summary>Opens a typed menu destination.</summary>
        public bool Open(LoogaMenuDestination destination, Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.Open(destination, requester, payload);
        }

        /// <summary>Opens the default layout of a screen.</summary>
        public bool Open(LoogaMenuScreenDefinition screen, Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.Open(screen, requester, payload);
        }

        /// <summary>Opens a specific layout of a screen.</summary>
        public bool Open(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            Object requester = null,
            object payload = null)
        {
            return _menuManager != null && _menuManager.Open(screen, layout, requester, payload);
        }

        /// <summary>Changes an open screen layout without adding a history entry.</summary>
        public bool SetLayout(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            Object requester = null)
        {
            return _menuManager != null && _menuManager.SetLayout(screen, layout, requester);
        }

        /// <summary>Returns to the previous menu state.</summary>
        public bool Back()
        {
            return _menuManager != null && _menuManager.Back();
        }

        /// <summary>Closes all open screens.</summary>
        public void CloseAll()
        {
            _menuManager?.CloseAll();
        }

        /// <summary>Applies persistent shared presentation for the active gameplay context.</summary>
        public void SetContext(LoogaMenuContextDefinition context)
        {
            _menuManager?.SetContext(context);
        }

        /// <summary>Registers a scene panel with this root.</summary>
        public void RegisterPanel(LoogaMenuPanel panel)
        {
            _menuManager?.RegisterPanel(panel);
        }

        private void RegisterPanels()
        {
            foreach (LoogaMenuPanel panel in _scenePanels)
            {
                RegisterPanel(panel);
            }

            if (!_registerChildrenOnAwake)
                return;

            foreach (LoogaMenuPanel panel in GetComponentsInChildren<LoogaMenuPanel>(true))
            {
                RegisterPanel(panel);
            }
        }

        private void RegisterStateProviders()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaStateProvider provider)
                {
                    provider.RegisterStates(_blackboardWriter);
                }
            }
        }

        private void UnregisterStateProviders()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaStateProvider provider)
                {
                    provider.UnregisterStates(_blackboardWriter);
                }
            }
        }

        private void ResolveBlackboard()
        {
            LoogaBlackboard blackboard = LoogaBlackboardRegistry.Active;
            if (blackboard == null)
            {
                _ownedBlackboard = new LoogaBlackboard();
                LoogaBlackboardRegistry.SetActive(_ownedBlackboard);
                blackboard = _ownedBlackboard;
            }

            _blackboardReader = blackboard;
            _blackboardWriter = blackboard;
        }

        private void ReleaseOwnedBlackboard()
        {
            if (_ownedBlackboard == null)
                return;

            LoogaBlackboardRegistry.ClearActive(_ownedBlackboard);
            _ownedBlackboard = null;
        }

        private void ResolveHandlers()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component is ILoogaMenuTransitionHandler transitionHandler)
                {
                    _menuManager.SetTransitionHandler(transitionHandler);
                }

                if (component is ILoogaMenuAudioHandler audioHandler)
                {
                    _menuManager.SetAudioHandler(audioHandler);
                }
            }
        }

        private static void SetActive(LoogaMenuRoot root)
        {
            if (Active == root)
                return;

            Active = root;
            ActiveChanged?.Invoke(root);
        }

        private void OnMenuStateChanged(LoogaMenuState state)
        {
            bool hadOpenScreens = _hadOpenScreens;
            _hadOpenScreens = state.HasOpenScreens;

            if (!_controlCursor)
                return;

            if (state.HasOpenScreens && state.ShowsCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            if (hadOpenScreens && !state.HasOpenScreens)
                ApplyClosedCursorState();
        }

        private void ApplyClosedCursorState()
        {
            Cursor.visible = _closedCursorVisible;
            Cursor.lockState = _closedLockMode;
        }
    }
}
