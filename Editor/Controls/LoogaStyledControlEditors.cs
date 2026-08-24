using LoogaSoft.UI.Controls;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor.Controls
{
    public abstract class LoogaStyledControlEditor : UnityEditor.Editor
    {
        private static readonly string[] ExcludedProperties =
        {
            "m_Script",
            "_styleSource",
            "_style",
            "m_TargetGraphic",
            "m_Transition",
            "m_Colors",
            "m_SpriteState",
            "m_AnimationTriggers"
        };

        private SerializedProperty _styleSource;
        private SerializedProperty _style;
        private SerializedProperty _targetGraphic;
        private SerializedProperty _transition;
        private SerializedProperty _colors;
        private SerializedProperty _spriteState;
        private SerializedProperty _animationTriggers;

        protected virtual string[] AdditionalExcludedProperties => System.Array.Empty<string>();

        protected virtual void OnEnable()
        {
            _styleSource = serializedObject.FindProperty("_styleSource");
            _style = serializedObject.FindProperty("_style");
            _targetGraphic = serializedObject.FindProperty("m_TargetGraphic");
            _transition = serializedObject.FindProperty("m_Transition");
            _colors = serializedObject.FindProperty("m_Colors");
            _spriteState = serializedObject.FindProperty("m_SpriteState");
            _animationTriggers = serializedObject.FindProperty("m_AnimationTriggers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_styleSource, new GUIContent("Source"));
            DrawAppearance();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
            DrawPropertiesExcluding(serializedObject, BuildExclusions());

            EditorGUILayout.Space(4f);
            DrawSetup();

            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed)
            {
                ApplyStyles();
            }

            if (GUILayout.Button("Apply Style"))
            {
                ApplyStyles();
            }
        }

        protected abstract void ApplyStyle(Object inspectedTarget);

        protected abstract void ResolveTargets(Object inspectedTarget);

        protected virtual void DrawSetupFields()
        {
        }

        private void DrawAppearance()
        {
            bool designSystem = !_styleSource.hasMultipleDifferentValues &&
                                _styleSource.enumValueIndex == (int)LoogaControlStyleSource.DesignSystem;
            if (designSystem)
            {
                EditorGUILayout.PropertyField(_style);
                if (_style.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Assign a control style to manage this appearance.", MessageType.Warning);
                }

                return;
            }

            if (_styleSource.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Selected controls use different appearance sources.", MessageType.None);
                return;
            }

            EditorGUILayout.PropertyField(_targetGraphic);
            EditorGUILayout.PropertyField(_transition);
            switch ((Selectable.Transition)_transition.enumValueIndex)
            {
                case Selectable.Transition.ColorTint:
                    EditorGUILayout.PropertyField(_colors);
                    break;
                case Selectable.Transition.SpriteSwap:
                    EditorGUILayout.PropertyField(_spriteState);
                    break;
                case Selectable.Transition.Animation:
                    EditorGUILayout.PropertyField(_animationTriggers);
                    break;
            }
        }

        private void DrawSetup()
        {
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            DrawSetupFields();
            if (!GUILayout.Button("Resolve Targets"))
            {
                return;
            }

            foreach (Object inspectedTarget in targets)
            {
                Undo.RecordObject(inspectedTarget, "Resolve Looga Control Targets");
                ResolveTargets(inspectedTarget);
                ApplyStyle(inspectedTarget);
                EditorUtility.SetDirty(inspectedTarget);
            }

            serializedObject.Update();
        }

        private string[] BuildExclusions()
        {
            string[] additional = AdditionalExcludedProperties;
            string[] combined = new string[ExcludedProperties.Length + additional.Length];
            ExcludedProperties.CopyTo(combined, 0);
            additional.CopyTo(combined, ExcludedProperties.Length);
            return combined;
        }

        private void ApplyStyles()
        {
            foreach (Object inspectedTarget in targets)
            {
                ApplyStyle(inspectedTarget);
                EditorUtility.SetDirty(inspectedTarget);
            }
        }
    }

    [CustomEditor(typeof(LoogaToggle)), CanEditMultipleObjects]
    public sealed class LoogaToggleEditor : LoogaStyledControlEditor
    {
        protected override string[] AdditionalExcludedProperties => new[] { "_label" };
        protected override void ApplyStyle(Object inspectedTarget) => ((LoogaToggle)inspectedTarget).ApplyStyle();
        protected override void ResolveTargets(Object inspectedTarget) => ((LoogaToggle)inspectedTarget).ResolveTargets();
        protected override void DrawSetupFields() => EditorGUILayout.PropertyField(serializedObject.FindProperty("_label"));
    }

    [CustomEditor(typeof(LoogaSlider)), CanEditMultipleObjects]
    public sealed class LoogaSliderEditor : LoogaStyledControlEditor
    {
        protected override string[] AdditionalExcludedProperties => new[] { "_fillGraphic", "_handleGraphic" };
        protected override void ApplyStyle(Object inspectedTarget) => ((LoogaSlider)inspectedTarget).ApplyStyle();
        protected override void ResolveTargets(Object inspectedTarget) => ((LoogaSlider)inspectedTarget).ResolveTargets();
        protected override void DrawSetupFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_fillGraphic"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_handleGraphic"));
        }
    }

    [CustomEditor(typeof(LoogaScrollbar)), CanEditMultipleObjects]
    public sealed class LoogaScrollbarEditor : LoogaStyledControlEditor
    {
        protected override string[] AdditionalExcludedProperties => new[] { "_handleGraphic" };
        protected override void ApplyStyle(Object inspectedTarget) => ((LoogaScrollbar)inspectedTarget).ApplyStyle();
        protected override void ResolveTargets(Object inspectedTarget) => ((LoogaScrollbar)inspectedTarget).ResolveTargets();
        protected override void DrawSetupFields() => EditorGUILayout.PropertyField(serializedObject.FindProperty("_handleGraphic"));
    }

    [CustomEditor(typeof(LoogaInputField)), CanEditMultipleObjects]
    public sealed class LoogaInputFieldEditor : LoogaStyledControlEditor
    {
        protected override string[] AdditionalExcludedProperties => new[] { "_placeholderText" };
        protected override void ApplyStyle(Object inspectedTarget) => ((LoogaInputField)inspectedTarget).ApplyStyle();
        protected override void ResolveTargets(Object inspectedTarget) => ((LoogaInputField)inspectedTarget).ResolveTargets();
        protected override void DrawSetupFields() => EditorGUILayout.PropertyField(serializedObject.FindProperty("_placeholderText"));
    }

    [CustomEditor(typeof(LoogaDropdown)), CanEditMultipleObjects]
    public sealed class LoogaDropdownEditor : LoogaStyledControlEditor
    {
        protected override string[] AdditionalExcludedProperties => new[] { "_arrow" };
        protected override void ApplyStyle(Object inspectedTarget) => ((LoogaDropdown)inspectedTarget).ApplyStyle();
        protected override void ResolveTargets(Object inspectedTarget) => ((LoogaDropdown)inspectedTarget).ResolveTargets();
        protected override void DrawSetupFields() => EditorGUILayout.PropertyField(serializedObject.FindProperty("_arrow"));
    }
}
