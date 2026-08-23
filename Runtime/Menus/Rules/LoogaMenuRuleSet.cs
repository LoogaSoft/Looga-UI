using System;
using LoogaSoft.Blackboard;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Rule Set", menuName = "LoogaSoft/UI/Menus/Rule Set")]
    public sealed class LoogaMenuRuleSet : ScriptableObject
    {
        [LoogaBoxGroup("Requirements")]
        [TooltipBox("A rule set gates menu screens and content entries using typed blackboard conditions.")]
        [SerializeField] private LoogaMenuRuleMode _mode = LoogaMenuRuleMode.AllMustPass;
        [LoogaBoxGroupEnd]
        [SerializeField] private LoogaBlackboardCondition[] _conditions = Array.Empty<LoogaBlackboardCondition>();

        [NonSerialized] private LoogaBlackboardCondition _firstCondition;
        [NonSerialized] private int _assignedConditionCount;
        [NonSerialized] private bool _cacheReady;

        public LoogaMenuRuleMode Mode => _mode;
        public LoogaBlackboardCondition[] Conditions => _conditions;

        private void OnEnable()
        {
            Warmup();
        }

        private void OnValidate()
        {
            Warmup();
        }

        public void Warmup()
        {
            _firstCondition = null;
            _assignedConditionCount = 0;

            if (_conditions != null)
            {
                for (int i = 0; i < _conditions.Length; i++)
                {
                    LoogaBlackboardCondition condition = _conditions[i];
                    if (condition == null)
                        continue;

                    _firstCondition ??= condition;
                    _assignedConditionCount++;
                    condition.Warmup();
                }
            }

            _cacheReady = true;
        }

        public bool CanOpen(ILoogaBlackboardReader blackboard, out string failureReason)
        {
            failureReason = string.Empty;

            if (!_cacheReady)
                Warmup();

            if (_conditions == null || _assignedConditionCount == 0)
                return true;

            bool anyPassed = false;
            for (int i = 0; i < _conditions.Length; i++)
            {
                LoogaBlackboardCondition condition = _conditions[i];
                if (condition == null)
                    continue;

                bool passed = condition.Evaluate(blackboard);
                anyPassed |= passed;

                if (_mode == LoogaMenuRuleMode.AllMustPass && !passed)
                {
                    failureReason = condition.FailureReason;
                    return false;
                }
            }

            if (_mode == LoogaMenuRuleMode.AnyCanPass && !anyPassed)
            {
                failureReason = _firstCondition != null ? _firstCondition.FailureReason : "No rule condition passed.";
                return false;
            }

            return true;
        }
    }
}
