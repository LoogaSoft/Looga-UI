using System;
using UnityEngine;

namespace LoogaSoft.UI.Extensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class LoogaUIShadowRendererOwner : MonoBehaviour
    {
        [SerializeField, HideInInspector] LoogaUIShadow _owner;

        public LoogaUIShadow Owner => _owner;

        internal void Assign(LoogaUIShadow owner)
        {
            _owner = owner;
        }

        void LateUpdate()
        {
            if (_owner != null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }
    }

    // The editor assembly assigns this hook so the runtime assembly stays player-safe.
    public static class LoogaUIShadowEditorBridge
    {
        public static Action<GameObject> RegisterCreatedObjectUndo { get; set; }
    }
}
