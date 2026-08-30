using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.UI.Design
{
    /// <summary>Collects the reusable visual tokens for one product.</summary>
    [CreateAssetMenu(fileName = "UI Design System", menuName = "LoogaSoft/UI/Design System/Design System")]
    public sealed class LoogaDesignSystem : LoogaDesignAsset
    {
        [SerializeField] private List<LoogaColorToken> _colors = new();
        [SerializeField] private List<LoogaButtonColorToken> _buttonColors = new();
        [SerializeField] private List<LoogaImageToken> _images = new();
        [SerializeField] private List<LoogaTextToken> _texts = new();

        /// <summary>Gets the registered graphic and text colors.</summary>
        public IReadOnlyList<LoogaColorToken> Colors => _colors;

        /// <summary>Gets the registered Unity UI button color states.</summary>
        public IReadOnlyList<LoogaButtonColorToken> ButtonColors => _buttonColors;

        /// <summary>Gets the registered Unity UI Image settings.</summary>
        public IReadOnlyList<LoogaImageToken> Images => _images;

        /// <summary>Gets the registered TextMeshPro settings.</summary>
        public IReadOnlyList<LoogaTextToken> Texts => _texts;
    }
}
