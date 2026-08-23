using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("LoogaSoft/Menu/Menu Button")]
    public sealed class LoogaMenuButton : MonoBehaviour
    {
        [SerializeField] private LoogaMenuCommand _command = new();
        [SerializeField] private bool _useActiveMenuRoot = true;
        [HideIf(nameof(_useActiveMenuRoot))]
        [SerializeField] private LoogaMenuRoot _menuRoot;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Execute);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Execute);
        }

        public void Execute()
        {
            _command?.Execute(ResolveMenuRoot(), this);
        }

        private LoogaMenuRoot ResolveMenuRoot()
        {
            if (!_useActiveMenuRoot && _menuRoot != null)
                return _menuRoot;

            return LoogaMenuRoot.Active;
        }
    }
}
