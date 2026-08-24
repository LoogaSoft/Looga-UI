using LoogaSoft.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Controls
{
    /// <summary>Defines the visual roles for one reusable scrollbar style.</summary>
    [CreateAssetMenu(fileName = "New Scrollbar Style", menuName = "LoogaSoft/UI/Design System/Scrollbar Style")]
    public sealed class LoogaScrollbarStyle : LoogaDesignAsset
    {
        [SerializeField] private LoogaElementStyle _scrollbar;
        [SerializeField] private LoogaElementStyle _handle;

        /// <summary>Applies this style to one scrollbar and its visual targets.</summary>
        public void Apply(LoogaTheme theme, Scrollbar scrollbar, Graphic handle)
        {
            _scrollbar?.Apply(theme, scrollbar != null ? scrollbar.targetGraphic : null, null, scrollbar);
            _handle?.Apply(theme, handle, null, null);
        }

        /// <summary>Returns true when this style uses the supplied design asset.</summary>
        public bool Uses(Object source)
        {
            return source == this || Uses(_scrollbar, source) || Uses(_handle, source);
        }

        private static bool Uses(LoogaElementStyle style, Object source)
        {
            return style != null && (source == style || style.Uses(source));
        }
    }
}
