using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Applies a shared color without changing other Graphic settings.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/UI/Design System/Color Binding")]
    public sealed class LoogaColorBinding : LoogaStyleBindingBase
    {
        [SerializeField] private LoogaColorToken _style;
        [SerializeField] private bool _resolveTargetAutomatically = true;
        [SerializeField] private Graphic _target;

        /// <summary>Gets the assigned color token.</summary>
        public LoogaColorToken Style => _style;

        /// <summary>Resolves the Graphic on this GameObject.</summary>
        public void ResolveTarget()
        {
            TryGetComponent(out _target);
        }

        protected override bool Uses(LoogaDesignAsset asset)
        {
            return asset == _style;
        }

        protected override void ApplyStyleInternal()
        {
            if (_resolveTargetAutomatically)
            {
                ResolveTarget();
            }

            if (_style != null && _target != null)
            {
                _target.color = _style.DefaultColor;
            }
        }
    }
}
