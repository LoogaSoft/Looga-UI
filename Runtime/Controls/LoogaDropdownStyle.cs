using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Defines the visual roles for one reusable dropdown style.</summary>
    public sealed class LoogaDropdownStyle : LoogaDesignAsset
    {
        [SerializeField] private LoogaElementStyle _dropdown;
        [SerializeField] private LoogaElementStyle _caption;
        [SerializeField] private LoogaElementStyle _captionImage;
        [SerializeField] private LoogaElementStyle _arrow;
        [SerializeField] private LoogaElementStyle _item;
        [SerializeField] private LoogaElementStyle _itemImage;

        /// <summary>Applies this style to one dropdown and its visual targets.</summary>
        public void Apply(TMP_Dropdown dropdown, LoogaTheme theme, Graphic arrow)
        {
            _dropdown?.Apply(theme, dropdown != null ? dropdown.targetGraphic : null, null, dropdown);
            _caption?.Apply(theme, dropdown != null ? dropdown.captionText : null, dropdown != null ? dropdown.captionText : null, null);
            _captionImage?.Apply(theme, dropdown != null ? dropdown.captionImage : null, null, null);
            _arrow?.Apply(theme, arrow, null, null);
            _item?.Apply(theme, dropdown != null ? dropdown.itemText : null, dropdown != null ? dropdown.itemText : null, null);
            _itemImage?.Apply(theme, dropdown != null ? dropdown.itemImage : null, null, null);
        }

        /// <summary>Returns true when this style uses the supplied design asset.</summary>
        public bool Uses(Object source)
        {
            return source == this || Uses(_dropdown, source) || Uses(_caption, source) ||
                   Uses(_captionImage, source) || Uses(_arrow, source) || Uses(_item, source) ||
                   Uses(_itemImage, source);
        }

        private static bool Uses(LoogaElementStyle style, Object source)
        {
            return style != null && (source == style || style.Uses(source));
        }
    }
}
