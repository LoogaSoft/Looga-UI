using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Defines one named shared UI slot.</summary>
    public sealed class LoogaMenuRegionDefinition : ScriptableObject
    {
        [SerializeField] private LoogaMenuRegionContent _defaultContent;

        public string DisplayName => name;
        public LoogaMenuRegionContent DefaultContent => _defaultContent;
        public System.Type ContentType => _defaultContent != null
            ? _defaultContent.GetType()
            : typeof(LoogaMenuPanelRegionContent);
    }
}
