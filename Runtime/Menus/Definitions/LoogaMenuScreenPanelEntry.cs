using System;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [Serializable]
    public sealed class LoogaMenuScreenPanelEntry
    {
        [SerializeField] private LoogaMenuPanelDefinition _panel;
        [SerializeField] private LoogaMenuBlackboardParameter[] _parameters = Array.Empty<LoogaMenuBlackboardParameter>();

        public LoogaMenuPanelDefinition Panel => _panel;
        public LoogaMenuBlackboardParameter[] Parameters => _parameters;
    }

    [Serializable]
    public sealed class LoogaMenuBlackboardParameter
    {
        [SerializeField] private LoogaBlackboardKey _key;
        [SerializeField] private bool _boolValue;
        [SerializeField] private int _intValue;
        [SerializeField] private float _floatValue;
        [SerializeField] private string _stringValue;

        public LoogaBlackboardKey Key => _key;

        public bool TryGetValue(out LoogaBlackboardValue value)
        {
            if (_key == null)
            {
                value = default;
                return false;
            }

            value = _key.ValueType switch
            {
                LoogaBlackboardValueType.Bool => LoogaBlackboardValue.Bool(_boolValue),
                LoogaBlackboardValueType.Int => LoogaBlackboardValue.Int(_intValue),
                LoogaBlackboardValueType.Float => LoogaBlackboardValue.Float(_floatValue),
                LoogaBlackboardValueType.String => LoogaBlackboardValue.String(_stringValue),
                _ => default
            };

            return value.type == _key.ValueType;
        }
    }
}
