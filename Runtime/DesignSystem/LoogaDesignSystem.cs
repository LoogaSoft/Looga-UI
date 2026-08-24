using System.Collections.Generic;
using LoogaSoft.UI.Controls;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Collects the default theme and semantic styles for one product.</summary>
    [CreateAssetMenu(fileName = "UI Design System", menuName = "LoogaSoft/UI/Design System/Design System")]
    public sealed class LoogaDesignSystem : LoogaDesignAsset
    {
        [SerializeField] private LoogaTheme _defaultTheme;
        [SerializeField] private List<LoogaElementStyle> _styles = new List<LoogaElementStyle>();
        [SerializeField] private List<LoogaButtonStyle> _buttonStyles = new List<LoogaButtonStyle>();
        [SerializeField] private List<LoogaToggleStyle> _toggleStyles = new List<LoogaToggleStyle>();
        [SerializeField] private List<LoogaSliderStyle> _sliderStyles = new List<LoogaSliderStyle>();
        [SerializeField] private List<LoogaScrollbarStyle> _scrollbarStyles = new List<LoogaScrollbarStyle>();
        [SerializeField] private List<LoogaInputFieldStyle> _inputFieldStyles = new List<LoogaInputFieldStyle>();
        [SerializeField] private List<LoogaDropdownStyle> _dropdownStyles = new List<LoogaDropdownStyle>();

        /// <summary>Gets the default theme.</summary>
        public LoogaTheme DefaultTheme => _defaultTheme;

        /// <summary>Gets the semantic styles registered for this product.</summary>
        public IReadOnlyList<LoogaElementStyle> Styles => _styles;

        /// <summary>Gets the button styles registered for this product.</summary>
        public IReadOnlyList<LoogaButtonStyle> ButtonStyles => _buttonStyles;

        /// <summary>Gets the toggle styles registered for this product.</summary>
        public IReadOnlyList<LoogaToggleStyle> ToggleStyles => _toggleStyles;

        /// <summary>Gets the slider styles registered for this product.</summary>
        public IReadOnlyList<LoogaSliderStyle> SliderStyles => _sliderStyles;

        /// <summary>Gets the scrollbar styles registered for this product.</summary>
        public IReadOnlyList<LoogaScrollbarStyle> ScrollbarStyles => _scrollbarStyles;

        /// <summary>Gets the input-field styles registered for this product.</summary>
        public IReadOnlyList<LoogaInputFieldStyle> InputFieldStyles => _inputFieldStyles;

        /// <summary>Gets the dropdown styles registered for this product.</summary>
        public IReadOnlyList<LoogaDropdownStyle> DropdownStyles => _dropdownStyles;
    }
}
