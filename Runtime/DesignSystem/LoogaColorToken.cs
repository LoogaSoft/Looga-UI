using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines one semantic color role and its fallback value.</summary>
    [CreateAssetMenu(fileName = "New Color Token", menuName = "LoogaSoft/UI/Design System/Color Token")]
    public sealed class LoogaColorToken : LoogaDesignAsset
    {
        [SerializeField] private Color _defaultColor = Color.white;

        /// <summary>Gets the color used when the active theme has no override.</summary>
        public Color DefaultColor => _defaultColor;

        /// <summary>Sets the fallback color.</summary>
        public void SetDefaultColor(Color color)
        {
            if (_defaultColor == color)
            {
                return;
            }

            _defaultColor = color;
            NotifyChanged();
        }
    }
}
