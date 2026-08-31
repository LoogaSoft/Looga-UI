using TMPro;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Applies a shared text style to one TextMeshPro component.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/UI/Design System/Text Style Binding")]
    public sealed class LoogaTextStyleBinding : LoogaStyleBindingBase
    {
        [SerializeField] private LoogaTextStyle _style;
        [SerializeField] private bool _resolveTargetAutomatically = true;
        [SerializeField] private TMP_Text _target;

        /// <summary>Gets the assigned text style.</summary>
        public LoogaTextStyle Style => _style;

        /// <summary>Resolves the TextMeshPro component on this GameObject.</summary>
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
