using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines reusable Unity UI Image component settings.</summary>
    [CreateAssetMenu(fileName = "New Image Token", menuName = "LoogaSoft/UI/Design System/Image Token")]
    public sealed class LoogaImageToken : LoogaDesignAsset
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Material _material;
        [SerializeField] private Image.Type _type = Image.Type.Simple;
        [SerializeField] private bool _preserveAspect;
        [SerializeField] private bool _fillCenter = true;
        [SerializeField, Min(0.01f)] private float _pixelsPerUnitMultiplier = 1f;
        [SerializeField] private bool _useSpriteMesh;
        [SerializeField] private Image.FillMethod _fillMethod = Image.FillMethod.Radial360;
        [SerializeField, Range(0f, 1f)] private float _fillAmount = 1f;
        [SerializeField] private bool _fillClockwise = true;
        [SerializeField] private int _fillOrigin;
        [SerializeField, Range(0f, 1f)] private float _alphaHitTestMinimumThreshold;

        /// <summary>Gets the configured source sprite.</summary>
        public Sprite Sprite => _sprite;

        /// <summary>Applies these values without changing color, alignment, or raycast ownership.</summary>
        public void Apply(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = _sprite;
            image.material = _material;
            image.type = _type;
            image.preserveAspect = _preserveAspect;
            image.fillCenter = _fillCenter;
            image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, _pixelsPerUnitMultiplier);
            image.useSpriteMesh = _useSpriteMesh;
            image.fillMethod = _fillMethod;
            image.fillAmount = _fillAmount;
            image.fillClockwise = _fillClockwise;
            image.fillOrigin = _fillOrigin;
            image.alphaHitTestMinimumThreshold = _alphaHitTestMinimumThreshold;
        }
    }
}
