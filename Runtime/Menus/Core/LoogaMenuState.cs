using System;
using System.Collections.Generic;

namespace LoogaSoft.Menu
{
    public readonly struct LoogaMenuState
    {
        public LoogaMenuState(IReadOnlyList<LoogaMenuScreenDefinition> openScreens,
            LoogaMenuInputPolicy inputPolicy)
        {
            OpenScreens = openScreens ?? Array.Empty<LoogaMenuScreenDefinition>();
            InputPolicy = inputPolicy;
        }

        public IReadOnlyList<LoogaMenuScreenDefinition> OpenScreens { get; }
        public LoogaMenuInputPolicy InputPolicy { get; }
        public bool HasOpenScreens => OpenScreens.Count > 0;
        public bool ShowsCursor => InputPolicy != null && InputPolicy.ShowsCursor;
        public bool BlocksGameplayInput => InputPolicy != null && InputPolicy.BlocksGameplayInput;
    }
}