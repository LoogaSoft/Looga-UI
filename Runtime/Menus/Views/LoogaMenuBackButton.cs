using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("LoogaSoft/Menu/Menu Back Button")]
    public sealed class LoogaMenuBackButton : MonoBehaviour
    {
        [SerializeField] private bool _useActiveMenuRoot = true;
        [HideIf(nameof(_useActiveMenuRoot))]
        [SerializeField] private LoogaMenuRoot _menuRoot;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Back);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(Back);
            }
        }

        private void Back()
        {
            LoogaMenuRoot root = ResolveMenuRoot();
            root?.Back();
        }

        private LoogaMenuRoot ResolveMenuRoot()
        {
            if (!_useActiveMenuRoot && _menuRoot != null)
                return _menuRoot;

            return LoogaMenuRoot.Active;
        }
    }
}
