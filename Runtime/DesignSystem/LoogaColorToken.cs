using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines one reusable color.</summary>
    [CreateAssetMenu(fileName = "New Color Token", menuName = "LoogaSoft/UI/Design System/Color Token")]
    public sealed class LoogaColorToken : LoogaDesignAsset
    {
        [SerializeField] private Color _defaultColor = Color.white;

        /// <summary>Gets the configured color.</summary>
        public Color DefaultColor => _defaultColor;

        /// <summary>Sets the configured color.</summary>
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
