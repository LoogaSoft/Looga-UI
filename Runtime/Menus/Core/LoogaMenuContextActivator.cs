using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Applies an authored menu context while this scene object is active.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Context Activator")]
    public sealed class LoogaMenuContextActivator : MonoBehaviour
    {
        [SerializeField] private LoogaMenuContextDefinition _context;
        [SerializeField] private LoogaMenuRoot _root;
        [SerializeField] private bool _clearOnDisable = true;

        private LoogaMenuContextDefinition _previousContext;

        private void OnEnable()
        {
            LoogaMenuRoot root = ResolveRoot();
            if (root == null)
                return;

            _previousContext = root.ActiveContext;
            root.SetContext(_context);
        }

        private void OnDisable()
        {
            LoogaMenuRoot root = ResolveRoot();
            if (_clearOnDisable && root != null && root.ActiveContext == _context)
                root.SetContext(_previousContext);

            _previousContext = null;
        }

        private LoogaMenuRoot ResolveRoot()
        {
            if (_root != null)
                return _root;

            _root = GetComponentInParent<LoogaMenuRoot>(true);
            return _root != null ? _root : LoogaMenuRoot.Active;
        }
    }
}
