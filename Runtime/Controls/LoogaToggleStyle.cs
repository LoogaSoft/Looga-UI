using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Defines the visual roles for one reusable toggle style.</summary>
    public sealed class LoogaToggleStyle : LoogaDesignAsset
    {
        [SerializeField] private LoogaElementStyle _toggle;
        [SerializeField] private LoogaElementStyle _checkmark;
        [SerializeField] private LoogaElementStyle _label;

        /// <summary>Applies this style to one toggle and its visual targets.</summary>
        public void Apply(LoogaTheme theme, Toggle toggle, Graphic checkmark, TMP_Text label)
        {
            _toggle?.Apply(theme, toggle != null ? toggle.targetGraphic : null, null, toggle);
            _checkmark?.Apply(theme, checkmark, null, null);
            _label?.Apply(theme, label, label, null);
        }

        /// <summary>Returns true when this style uses the supplied design asset.</summary>
        public bool Uses(Object source)
        {
            return source == this || Uses(_toggle, source) || Uses(_checkmark, source) || Uses(_label, source);
        }

        private static bool Uses(LoogaElementStyle style, Object source)
        {
            return style != null && (source == style || style.Uses(source));
        }
    }
}
