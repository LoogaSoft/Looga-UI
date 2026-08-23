using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("LoogaSoft/Menu/Open Menu Button")]
    public sealed class LoogaMenuOpenButton : MonoBehaviour
    {
        [SerializeField] private LoogaMenuDestination _destination = new();

        [SerializeField] private bool _useActiveMenuRoot = true;
        [HideIf(nameof(_useActiveMenuRoot))]
        [SerializeField] private LoogaMenuRoot _menuRoot;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Open);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Open);
        }

        private void Open()
        {
            _destination?.Open(ResolveMenuRoot(), this);
        }

        private LoogaMenuRoot ResolveMenuRoot()
        {
            if (!_useActiveMenuRoot && _menuRoot != null)
                return _menuRoot;

            return LoogaMenuRoot.Active;
        }
    }
}
