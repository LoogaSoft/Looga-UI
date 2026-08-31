using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Identifies the Unity UI control that receives a control style.</summary>
    public enum LoogaControlType
    {
        Button,
        Dropdown,
        InputField,
        Scrollbar,
        Slider,
        Toggle
    }

    /// <summary>Defines reusable settings for one Unity UI control type.</summary>
    [MovedFrom(true, "LoogaSoft.UI.Design", null, "LoogaButtonColorToken")]
    [CreateAssetMenu(fileName = "New Control Style", menuName = "LoogaSoft/UI/Design System/Control Style")]
    public sealed class LoogaControlStyle : LoogaDesignAsset
    {
        [SerializeField] private LoogaControlType _controlType;

        [Header("Control")]
        [SerializeField] private LoogaImageStyle _background;
        [SerializeField] private Selectable.Transition _transition = Selectable.Transition.ColorTint;

        [Header("Color Tint")]
        [SerializeField] private Color _normal = Color.white;
        [SerializeField] private Color _highlighted = new(0.882f, 0.882f, 0.882f, 1f);
        [SerializeField] private Color _pressed = new(0.698f, 0.698f, 0.698f, 1f);
        [SerializeField] private Color _selected = new(0.882f, 0.882f, 0.882f, 1f);
        [SerializeField] private Color _disabled = new(0.521f, 0.521f, 0.521f, 0.502f);
        [SerializeField, Min(0f)] private float _colorMultiplier = 1f;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.1f;

        [Header("Sprite Swap")]
        [SerializeField] private Sprite _highlightedSprite;
        [SerializeField] private Sprite _pressedSprite;
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Sprite _disabledSprite;

        [Header("Animation")]
        [SerializeField] private AnimationTriggers _animationTriggers = new();

        [Header("Button")]
        [SerializeField] private LoogaTextStyle _buttonLabel;
        [SerializeField] private LoogaImageStyle _buttonIcon;

        [Header("Dropdown")]
        [SerializeField] private LoogaTextStyle _dropdownCaption;
        [SerializeField] private LoogaImageStyle _dropdownCaptionImage;
        [SerializeField] private LoogaImageStyle _dropdownArrow;
        [SerializeField] private LoogaTextStyle _dropdownItemText;
        [SerializeField] private LoogaImageStyle _dropdownItemImage;

        [Header("Input Field")]
        [SerializeField] private LoogaTextStyle _inputText;
        [SerializeField] private LoogaTextStyle _inputPlaceholder;

        [Header("Scrollbar")]
        [SerializeField] private LoogaImageStyle _scrollbarHandle;

        [Header("Slider")]
        [SerializeField] private LoogaImageStyle _sliderFill;
        [SerializeField] private LoogaImageStyle _sliderHandle;

        [Header("Toggle")]
        [SerializeField] private LoogaImageStyle _toggleCheckmark;
        [SerializeField] private LoogaTextStyle _toggleLabel;

        /// <summary>Gets the Unity UI control type for this style.</summary>
        public LoogaControlType ControlType => _controlType;

        /// <summary>Returns true when this control style uses the supplied design asset.</summary>
        public bool Uses(LoogaDesignAsset asset)
        {
            return asset == this ||
                   asset == _background ||
                   asset == _buttonLabel ||
                   asset == _buttonIcon ||
                   asset == _dropdownCaption ||
                   asset == _dropdownCaptionImage ||
                   asset == _dropdownArrow ||
                   asset == _dropdownItemText ||
                   asset == _dropdownItemImage ||
                   asset == _inputText ||
                   asset == _inputPlaceholder ||
                   asset == _scrollbarHandle ||
                   asset == _sliderFill ||
                   asset == _sliderHandle ||
                   asset == _toggleCheckmark ||
                   asset == _toggleLabel;
        }

        /// <summary>Applies this style to a button and its visual targets.</summary>
        public void Apply(Button button, TMP_Text label, Image icon)
        {
            if (_controlType != LoogaControlType.Button || button == null)
            {
                return;
            }

            ApplyControl(button);
            _buttonLabel?.Apply(label);
            _buttonIcon?.Apply(icon);
        }

        /// <summary>Applies this style to a TextMeshPro dropdown and its visual targets.</summary>
        public void Apply(TMP_Dropdown dropdown, Image arrow)
        {
            if (_controlType != LoogaControlType.Dropdown || dropdown == null)
            {
                return;
            }

            ApplyControl(dropdown);
            _dropdownCaption?.Apply(dropdown.captionText);
            _dropdownCaptionImage?.Apply(dropdown.captionImage);
            _dropdownArrow?.Apply(arrow);
            _dropdownItemText?.Apply(dropdown.itemText);
            _dropdownItemImage?.Apply(dropdown.itemImage);
        }

        /// <summary>Applies this style to a TextMeshPro input field.</summary>
        public void Apply(TMP_InputField inputField, TMP_Text text, TMP_Text placeholder)
        {
            if (_controlType != LoogaControlType.InputField || inputField == null)
            {
                return;
            }

            ApplyControl(inputField);
            _inputText?.Apply(text != null ? text : inputField.textComponent);
            _inputPlaceholder?.Apply(placeholder != null ? placeholder : inputField.placeholder as TMP_Text);
        }

        /// <summary>Applies this style to a scrollbar.</summary>
        public void Apply(Scrollbar scrollbar, Image handle)
        {
            if (_controlType != LoogaControlType.Scrollbar || scrollbar == null)
            {
                return;
            }

            ApplyControl(scrollbar);
            _scrollbarHandle?.Apply(handle != null ? handle : GetImage(scrollbar.handleRect));
        }

        /// <summary>Applies this style to a slider.</summary>
        public void Apply(Slider slider, Image fill, Image handle)
        {
            if (_controlType != LoogaControlType.Slider || slider == null)
            {
                return;
            }

            ApplyControl(slider);
            _sliderFill?.Apply(fill != null ? fill : GetImage(slider.fillRect));
            _sliderHandle?.Apply(handle != null ? handle : GetImage(slider.handleRect));
        }

        /// <summary>Applies this style to a toggle.</summary>
        public void Apply(Toggle toggle, Image checkmark, TMP_Text label)
        {
            if (_controlType != LoogaControlType.Toggle || toggle == null)
            {
                return;
            }

            ApplyControl(toggle);
            _toggleCheckmark?.Apply(checkmark != null ? checkmark : toggle.graphic as Image);
            _toggleLabel?.Apply(label);
        }

        private void ApplyControl(Selectable control)
        {
            _background?.Apply(control.targetGraphic as Image);
            control.transition = _transition;

            switch (_transition)
            {
                case Selectable.Transition.ColorTint:
                    control.colors = new ColorBlock
                    {
                        normalColor = _normal,
                        highlightedColor = _highlighted,
                        pressedColor = _pressed,
                        selectedColor = _selected,
                        disabledColor = _disabled,
                        colorMultiplier = Mathf.Max(0f, _colorMultiplier),
                        fadeDuration = Mathf.Max(0f, _fadeDuration)
                    };
                    break;
                case Selectable.Transition.SpriteSwap:
                    control.spriteState = new SpriteState
                    {
                        highlightedSprite = _highlightedSprite,
                        pressedSprite = _pressedSprite,
                        selectedSprite = _selectedSprite,
                        disabledSprite = _disabledSprite
                    };
                    break;
                case Selectable.Transition.Animation:
                    control.animationTriggers = _animationTriggers;
                    break;
            }
        }

        private static Image GetImage(RectTransform target)
        {
            return target != null ? target.GetComponent<Image>() : null;
        }
    }
}
