using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Defines the visual roles for one reusable button style.</summary>
    public sealed class LoogaButtonStyle : LoogaDesignAsset
    {
        [SerializeField] private LoogaElementStyle _button;
        [SerializeField] private LoogaElementStyle _label;
        [SerializeField] private LoogaElementStyle _icon;

        /// <summary>Applies this style to one button and its visual targets.</summary>
        public void Apply(LoogaTheme theme, Button button, TMP_Text label, Graphic icon)
        {
            Graphic background = button != null ? button.targetGraphic : null;
            _button?.Apply(theme, background, null, button);
            _label?.Apply(theme, label, label, null);
            _icon?.Apply(theme, icon, null, null);
        }

        /// <summary>Returns true when this style uses the supplied design asset.</summary>
        public bool Uses(Object source)
        {
            return source == this ||
                   Uses(_button, source) ||
                   Uses(_label, source) ||
                   Uses(_icon, source);
        }

        private static bool Uses(LoogaElementStyle style, Object source)
        {
            return style != null && (source == style || style.Uses(source));
        }
    }
}
