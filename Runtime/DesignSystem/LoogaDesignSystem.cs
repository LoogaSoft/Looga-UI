using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LoogaSoft.UI.Design
{
    /// <summary>Collects the reusable visual styles for one product.</summary>
    [CreateAssetMenu(fileName = "UI Design System", menuName = "LoogaSoft/UI/Design System/Design System")]
    public sealed class LoogaDesignSystem : LoogaDesignAsset
    {
        [SerializeField] private List<LoogaColorToken> _colors = new();
        [FormerlySerializedAs("_buttonColors")]
        [SerializeField] private List<LoogaControlStyle> _controls = new();
        [SerializeField] private List<LoogaImageStyle> _images = new();
        [SerializeField] private List<LoogaTextStyle> _texts = new();

        /// <summary>Gets the registered graphic and text colors.</summary>
        public IReadOnlyList<LoogaColorToken> Colors => _colors;

        /// <summary>Gets the registered Unity UI control styles.</summary>
        public IReadOnlyList<LoogaControlStyle> Controls => _controls;

        /// <summary>Gets the registered Unity UI Image settings.</summary>
        public IReadOnlyList<LoogaImageStyle> Images => _images;

        /// <summary>Gets the registered TextMeshPro settings.</summary>
        public IReadOnlyList<LoogaTextStyle> Texts => _texts;
    }
}
