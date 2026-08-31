using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Applies a shared image style to one Unity UI Image.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/UI/Design System/Image Style Binding")]
    public sealed class LoogaImageStyleBinding : LoogaStyleBindingBase
    {
        [SerializeField] private LoogaImageStyle _style;
        [SerializeField] private bool _resolveTargetAutomatically = true;
        [SerializeField] private Image _target;

        /// <summary>Gets the assigned image style.</summary>
        public LoogaImageStyle Style => _style;

        /// <summary>Resolves the Image on this GameObject.</summary>
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

            _style?.Apply(_target);
        }
    }
}
