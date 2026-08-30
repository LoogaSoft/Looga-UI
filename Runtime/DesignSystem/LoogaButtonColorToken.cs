using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Design
{
    /// <summary>Defines the color states for one Unity UI button role.</summary>
    [CreateAssetMenu(
        fileName = "New Button Color Token",
        menuName = "LoogaSoft/UI/Design System/Button Color Token")]
    public sealed class LoogaButtonColorToken : LoogaDesignAsset
    {
        [SerializeField] private Color _normal = Color.white;
        [SerializeField] private Color _highlighted = new(0.882f, 0.882f, 0.882f, 1f);
        [SerializeField] private Color _pressed = new(0.698f, 0.698f, 0.698f, 1f);
        [SerializeField] private Color _selected = new(0.882f, 0.882f, 0.882f, 1f);
        [SerializeField] private Color _disabled = new(0.521f, 0.521f, 0.521f, 0.502f);
        [SerializeField, Min(0f)] private float _colorMultiplier = 1f;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.1f;

        /// <summary>Gets the configured Unity UI color block.</summary>
        public ColorBlock Colors => new()
        {
            normalColor = _normal,
            highlightedColor = _highlighted,
            pressedColor = _pressed,
            selectedColor = _selected,
            disabledColor = _disabled,
            colorMultiplier = _colorMultiplier,
            fadeDuration = _fadeDuration
        };

        /// <summary>Applies these color states to a Unity UI button.</summary>
        public void Apply(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.transition = Selectable.Transition.ColorTint;
            button.colors = Colors;
        }
    }
}
