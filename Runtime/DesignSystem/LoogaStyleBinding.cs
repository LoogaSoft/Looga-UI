using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Applies direct design tokens to Unity UI components on one GameObject.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/UI/Design System/Style Binding")]
    public sealed class LoogaStyleBinding : MonoBehaviour
    {
        [Header("Tokens")]
        [SerializeField] private LoogaColorToken _color;
        [SerializeField] private LoogaButtonColorToken _buttonColors;
        [SerializeField] private LoogaImageToken _image;
        [SerializeField] private LoogaTextToken _text;

        [Header("Targets")]
        [SerializeField] private bool _resolveTargetsAutomatically = true;
        [SerializeField] private Graphic _graphicTarget;
        [SerializeField] private Button _buttonTarget;
        [SerializeField] private Image _imageTarget;
        [SerializeField] private TMP_Text _textTarget;

        /// <summary>Occurs after this binding applies its tokens.</summary>
        public event Action StyleApplied;

        /// <summary>Gets the assigned generic color token.</summary>
        public LoogaColorToken Color => _color;

        /// <summary>Gets the assigned button color token.</summary>
        public LoogaButtonColorToken ButtonColors => _buttonColors;

        /// <summary>Gets the assigned image token.</summary>
        public LoogaImageToken Image => _image;

        /// <summary>Gets the assigned TextMeshPro token.</summary>
        public LoogaTextToken Text => _text;

        /// <summary>Applies every assigned token to its compatible target.</summary>
        public void ApplyStyle()
        {
            if (_resolveTargetsAutomatically)
            {
                ResolveTargets();
            }

            if (_color != null && _graphicTarget != null)
            {
                _graphicTarget.color = _color.DefaultColor;
            }

            _buttonColors?.Apply(_buttonTarget);
            _image?.Apply(_imageTarget);
            _text?.Apply(_textTarget);
            StyleApplied?.Invoke();
        }

        /// <summary>Resolves compatible Unity UI targets on this GameObject.</summary>
        public void ResolveTargets()
        {
            TryGetComponent(out _buttonTarget);
            TryGetComponent(out _imageTarget);
            TryGetComponent(out _textTarget);

            _graphicTarget = _textTarget != null
                ? _textTarget
                : _imageTarget != null
                    ? _imageTarget
                    : GetComponent<Graphic>();
        }

        private void OnEnable()
        {
            LoogaDesignSystemEvents.Changed += HandleDesignChanged;
            ApplyStyle();
        }

        private void OnDisable()
        {
            LoogaDesignSystemEvents.Changed -= HandleDesignChanged;
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                ApplyStyle();
            }
        }

        private void HandleDesignChanged(LoogaDesignSystemChange change)
        {
            if (change.Source == _color ||
                change.Source == _buttonColors ||
                change.Source == _image ||
                change.Source == _text)
            {
                ApplyStyle();
            }
        }
    }
}
