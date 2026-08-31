using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Applies a shared control style and its child image and text styles.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/UI/Design System/Control Style Binding")]
    public sealed class LoogaControlStyleBinding : LoogaStyleBindingBase
    {
        [SerializeField] private LoogaControlStyle _style;
        [SerializeField] private bool _resolveTargetsAutomatically = true;
        [SerializeField] private Selectable _controlTarget;

        [SerializeField] private TMP_Text _buttonLabel;
        [SerializeField] private Image _buttonIcon;
        [SerializeField] private Image _dropdownArrow;
        [SerializeField] private TMP_Text _inputText;
        [SerializeField] private TMP_Text _inputPlaceholder;
        [SerializeField] private Image _scrollbarHandle;
        [SerializeField] private Image _sliderFill;
        [SerializeField] private Image _sliderHandle;
        [SerializeField] private Image _toggleCheckmark;
        [SerializeField] private TMP_Text _toggleLabel;

        /// <summary>Gets the assigned control style.</summary>
        public LoogaControlStyle Style => _style;

        /// <summary>Gets whether the target matches the selected control type.</summary>
        public bool HasCompatibleTarget => _style == null || IsCompatible(_style.ControlType, _controlTarget);

        /// <summary>Resolves the control and its standard visual targets.</summary>
        public void ResolveTargets()
        {
            TryGetComponent(out _controlTarget);

            switch (_controlTarget)
            {
                case Button button:
                    _buttonLabel = button.GetComponentInChildren<TMP_Text>(true);
                    _buttonIcon = FindButtonIcon(button);
                    break;
                case TMP_Dropdown dropdown:
                    _dropdownArrow = FindImage(dropdown.transform, "Arrow");
                    break;
                case TMP_InputField inputField:
                    _inputText = inputField.textComponent;
                    _inputPlaceholder = inputField.placeholder as TMP_Text;
                    break;
                case Scrollbar scrollbar:
                    _scrollbarHandle = GetImage(scrollbar.handleRect);
                    break;
                case Slider slider:
                    _sliderFill = GetImage(slider.fillRect);
                    _sliderHandle = GetImage(slider.handleRect);
                    break;
                case Toggle toggle:
                    _toggleCheckmark = toggle.graphic as Image;
                    _toggleLabel = toggle.GetComponentInChildren<TMP_Text>(true);
                    break;
            }
        }

        protected override bool Uses(LoogaDesignAsset asset)
        {
            return _style != null && _style.Uses(asset);
        }

        protected override void ApplyStyleInternal()
        {
            if (_resolveTargetsAutomatically)
            {
                ResolveTargets();
            }

            if (_style == null)
            {
                return;
            }

            switch (_style.ControlType)
            {
                case LoogaControlType.Button:
                    _style.Apply(_controlTarget as Button, _buttonLabel, _buttonIcon);
                    break;
                case LoogaControlType.Dropdown:
                    _style.Apply(_controlTarget as TMP_Dropdown, _dropdownArrow);
                    break;
                case LoogaControlType.InputField:
                    _style.Apply(_controlTarget as TMP_InputField, _inputText, _inputPlaceholder);
                    break;
                case LoogaControlType.Scrollbar:
                    _style.Apply(_controlTarget as Scrollbar, _scrollbarHandle);
                    break;
                case LoogaControlType.Slider:
                    _style.Apply(_controlTarget as Slider, _sliderFill, _sliderHandle);
                    break;
                case LoogaControlType.Toggle:
                    _style.Apply(_controlTarget as Toggle, _toggleCheckmark, _toggleLabel);
                    break;
            }
        }

        private static bool IsCompatible(LoogaControlType type, Selectable target)
        {
            return type switch
            {
                LoogaControlType.Button => target is Button,
                LoogaControlType.Dropdown => target is TMP_Dropdown,
                LoogaControlType.InputField => target is TMP_InputField,
                LoogaControlType.Scrollbar => target is Scrollbar,
                LoogaControlType.Slider => target is Slider,
                LoogaControlType.Toggle => target is Toggle,
                _ => false
            };
        }

        private static Image FindButtonIcon(Button button)
        {
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

        private static Image FindImage(Transform root, string objectName)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (string.Equals(images[i].name, objectName, StringComparison.OrdinalIgnoreCase))
                {
                    return images[i];
                }
            }

            return null;
        }

        private static Image GetImage(RectTransform target)
        {
            return target != null ? target.GetComponent<Image>() : null;
        }
    }
}
