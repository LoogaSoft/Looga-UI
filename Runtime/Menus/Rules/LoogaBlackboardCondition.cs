using System;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public enum LoogaBlackboardComparison
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual
    }

    [Serializable]
    public sealed class LoogaBlackboardCondition
    {
        [SerializeField] private LoogaBlackboardKey _key;
        [SerializeField] private LoogaBlackboardComparison _comparison = LoogaBlackboardComparison.Equals;
        [SerializeField] private bool _expectedBool;
        [SerializeField] private int _expectedInt;
        [SerializeField] private float _expectedFloat;
        [SerializeField] private string _expectedString;
        [SerializeField] private string _failureReason;

        [NonSerialized] private string _cachedFailureReason;

        public LoogaBlackboardKey Key => _key;
        public string FailureReason => _cachedFailureReason ?? ResolveFailureReason();

        internal void Warmup()
        {
            _cachedFailureReason = ResolveFailureReason();
        }

        public bool Evaluate(ILoogaBlackboardReader blackboard)
        {
            if (_key == null || blackboard == null || !blackboard.TryGetValue(_key, out LoogaBlackboardValue value))
                return false;

            if (value.type != _key.ValueType)
                return false;

            return _key.ValueType switch
            {
                LoogaBlackboardValueType.Bool => CompareBool(value.boolValue),
                LoogaBlackboardValueType.Int => CompareNumber(value.intValue, _expectedInt),
                LoogaBlackboardValueType.Float => CompareNumber(value.floatValue, _expectedFloat),
                LoogaBlackboardValueType.String => CompareString(value.stringValue),
                _ => false
            };
        }

        private string ResolveFailureReason()
        {
            if (!string.IsNullOrWhiteSpace(_failureReason))
                return _failureReason;

            return _key != null ? $"{_key.DisplayName} condition failed." : "Blackboard condition failed.";
        }

        private bool CompareBool(bool current)
        {
            return _comparison switch
            {
                LoogaBlackboardComparison.Equals => current == _expectedBool,
                LoogaBlackboardComparison.NotEquals => current != _expectedBool,
                _ => false
            };
        }

        private bool CompareNumber(float current, float expected)
        {
            return _comparison switch
            {
                LoogaBlackboardComparison.Equals => Mathf.Approximately(current, expected),
                LoogaBlackboardComparison.NotEquals => !Mathf.Approximately(current, expected),
                LoogaBlackboardComparison.GreaterThan => current > expected,
                LoogaBlackboardComparison.GreaterOrEqual => current >= expected,
                LoogaBlackboardComparison.LessThan => current < expected,
                LoogaBlackboardComparison.LessOrEqual => current <= expected,
                _ => false
            };
        }

        private bool CompareString(string current)
        {
            return _comparison switch
            {
                LoogaBlackboardComparison.Equals => string.Equals(current, _expectedString, StringComparison.Ordinal),
                LoogaBlackboardComparison.NotEquals => !string.Equals(current, _expectedString, StringComparison.Ordinal),
                _ => false
            };
        }
    }
}