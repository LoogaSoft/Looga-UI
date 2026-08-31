using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Maintains compatibility with combined style bindings from earlier package versions.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class LoogaStyleBinding : MonoBehaviour
    {
        [Header("Styles")]
        [InspectorName("Color Only")]
        [Tooltip("Changes only the target Graphic color. An assigned Image style owns Image color instead.")]
        [SerializeField] private LoogaColorToken _color;
        [FormerlySerializedAs("_buttonColors")]
        [SerializeField] private LoogaControlStyle _control;
        [Tooltip("Changes the complete Image appearance, including color and alpha.")]
        [SerializeField] private LoogaImageStyle _image;
        [SerializeField] private LoogaTextStyle _text;

        [Header("Targets")]
        [SerializeField] private bool _resolveTargetsAutomatically = true;
        [SerializeField] private Graphic _graphicTarget;
        [SerializeField] private Button _buttonTarget;
        [SerializeField] private Image _imageTarget;
        [SerializeField] private TMP_Text _textTarget;

        /// <summary>Occurs after this binding applies its styles.</summary>
        public event Action StyleApplied;

        /// <summary>Gets the assigned color-only token.</summary>
        public LoogaColorToken Color => _color;

        /// <summary>Gets the assigned control style.</summary>
        public LoogaControlStyle Control => _control;

        /// <summary>Gets the assigned image style.</summary>
        public LoogaImageStyle Image => _image;

        /// <summary>Gets the assigned TextMeshPro style.</summary>
        public LoogaTextStyle Text => _text;

        /// <summary>Applies every assigned style to its compatible target.</summary>
        public void ApplyStyle()
        {
            if (_resolveTargetsAutomatically)
            {
                ResolveTargets();
            }

            _control?.Apply(
                _buttonTarget,
                _buttonTarget != null ? _buttonTarget.GetComponentInChildren<TMP_Text>(true) : null,
                FindButtonIcon(_buttonTarget));
            _image?.Apply(_imageTarget);

            if (_image == null && _color != null && _graphicTarget != null)
            {
                _graphicTarget.color = _color.DefaultColor;
            }

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
                (_control != null &&
                 change.Source is LoogaDesignAsset designAsset &&
                 _control.Uses(designAsset)) ||
                change.Source == _image ||
                change.Source == _text)
            {
                ApplyStyle();
            }
        }

        private static Image FindButtonIcon(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Image[] images = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != button.targetGraphic)
                {
                    return images[i];
                }
            }

            return null;
        }
    }
}
