using LoogaSoft.Inspector.Runtime;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Panel", menuName = "LoogaSoft/UI/Menus/Panel Definition")]
    public sealed class LoogaMenuPanelDefinition : ScriptableObject
    {
        [LoogaBoxGroup("Identity")]
        [TooltipBox("A panel is one reusable UI piece, such as Stockpile, Loadout, Action Bar, or a shared background.")]
        [LoogaBoxGroupEnd]
        [SerializeField, TextArea] private string _description;

        [LoogaBoxGroup("Feedback")]
        [SerializeField] private bool _skipTransitions;
        [Tooltip("Auto selects an edge from the panel anchors. Choose an edge to override it.")]
        [SerializeField] private LoogaMenuTransitionMotion _transitionMotion = LoogaMenuTransitionMotion.Auto;
        [Tooltip("Waits before this panel begins its transition, in seconds.")]
        [SerializeField, Min(0f)] private float _transitionDelay;
        [Tooltip("Scales the global slide distance for this panel.")]
        [SerializeField, Min(0.1f)] private float _transitionDistanceMultiplier = 1f;
        [SerializeField] private bool _skipOpenSound;
        [LoogaBoxGroupEnd]
        [SerializeField] private bool _skipCloseSound;

        public string DisplayName => name;
        public string Description => _description;
        public bool SkipTransitions => _skipTransitions;
        public LoogaMenuTransitionMotion TransitionMotion => _transitionMotion;
        public float TransitionDelay => _transitionDelay;
        public float TransitionDistanceMultiplier => _transitionDistanceMultiplier;
        public bool SkipOpenSound => _skipOpenSound;
        public bool SkipCloseSound => _skipCloseSound;
    }

    /// <summary>Defines where a menu panel enters from and exits toward.</summary>
    public enum LoogaMenuTransitionMotion
    {
        Auto = 0,
        FadeOnly = 1,
        Left = 2,
        Right = 3,
        Top = 4,
        Bottom = 5
    }
}
