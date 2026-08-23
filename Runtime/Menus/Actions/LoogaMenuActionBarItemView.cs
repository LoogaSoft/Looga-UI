using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Action Bar Item View")]
    public sealed class LoogaMenuActionBarItemView : MonoBehaviour
    {
        private static readonly Dictionary<Type, PropertyInfo> TextProperties = new();

        [SerializeField] private Button _button;
        [SerializeField] private Component _bindingText;
        [SerializeField] private Component _labelText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private LoogaMenuActionDescriptor _action;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (_button != null)
            {
                _button.onClick.AddListener(Execute);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(Execute);
            }
        }

        public void Bind(LoogaMenuActionDescriptor action)
        {
            ResolveReferences();
            _action = action;
            SetText(_bindingText, action.Binding);
            SetText(_labelText, action.Label);

            if (_button != null)
            {
                _button.interactable = action.Enabled && action.Execute != null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = action.Enabled ? 1f : 0.45f;
            }
        }

        private void Execute()
        {
            if (_action.Enabled)
            {
                _action.Execute?.Invoke();
            }
        }

        private void ResolveReferences()
        {
            _button ??= GetComponent<Button>();
            _canvasGroup ??= GetComponent<CanvasGroup>();
        }

        private static void SetText(Component component, string value)
        {
            if (component == null)
                return;

            if (component is Text text)
            {
                text.text = value;
                return;
            }

            Type type = component.GetType();
            if (!TextProperties.TryGetValue(type, out PropertyInfo property))
            {
                property = type.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
                TextProperties[type] = property;
            }

            if (property != null && property.CanWrite && property.PropertyType == typeof(string))
            {
                property.SetValue(component, value);
            }
        }
    }
}
