using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Collects the default theme and semantic styles for one product.</summary>
    [CreateAssetMenu(fileName = "UI Design System", menuName = "LoogaSoft/UI/Design System/Design System")]
    public sealed class LoogaDesignSystem : LoogaDesignAsset
    {
        [SerializeField] private LoogaTheme _defaultTheme;
        [SerializeField] private List<LoogaElementStyle> _styles = new List<LoogaElementStyle>();

        /// <summary>Gets the default theme.</summary>
        public LoogaTheme DefaultTheme => _defaultTheme;

        /// <summary>Gets the semantic styles registered for this product.</summary>
        public IReadOnlyList<LoogaElementStyle> Styles => _styles;
    }
}
