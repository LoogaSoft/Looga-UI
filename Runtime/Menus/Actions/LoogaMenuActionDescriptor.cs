using System;
using UnityEngine.InputSystem;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Describes one command or input hint presented by a menu action bar.
    /// </summary>
    public readonly struct LoogaMenuActionDescriptor
    {
        private readonly string _binding;
        private readonly InputActionReference _inputAction;

        public LoogaMenuActionDescriptor(
            string id,
            string label,
            string binding,
            Action execute,
            bool enabled = true,
            int sortOrder = 0)
        {
            Id = id;
            Label = label;
            _binding = binding;
            _inputAction = null;
            Execute = execute;
            Enabled = enabled;
            SortOrder = sortOrder;
        }

        public LoogaMenuActionDescriptor(
            string id,
            string label,
            InputActionReference inputAction,
            string fallbackBinding,
            Action execute,
            bool enabled = true,
            int sortOrder = 0)
        {
            Id = id;
            Label = label;
            _binding = fallbackBinding;
            _inputAction = inputAction;
            Execute = execute;
            Enabled = enabled;
            SortOrder = sortOrder;
        }

        public string Id { get; }
        public string Label { get; }
        public string Binding => ResolveBinding();
        public Action Execute { get; }
        public bool Enabled { get; }
        public int SortOrder { get; }

        private string ResolveBinding()
        {
            InputAction action = _inputAction != null ? _inputAction.action : null;
            if (action == null)
                return _binding;

            string display = action.GetBindingDisplayString();
            return string.IsNullOrWhiteSpace(display) ? _binding : display;
        }
    }
}
