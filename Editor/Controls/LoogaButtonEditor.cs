using LoogaSoft.UI.Controls;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor.Controls
{
    /// <summary>Draws button behavior and exposes only the active appearance owner.</summary>
    [CustomEditor(typeof(LoogaButton))]
    [CanEditMultipleObjects]
    public sealed class LoogaButtonEditor : UnityEditor.Editor
    {
        private SerializedProperty _styleSource;
        private SerializedProperty _style;
        private SerializedProperty _resolveTargetsAutomatically;
        private SerializedProperty _label;
        private SerializedProperty _icon;
        private SerializedProperty _interactable;
        private SerializedProperty _targetGraphic;
        private SerializedProperty _transition;
        private SerializedProperty _colors;
        private SerializedProperty _spriteState;
        private SerializedProperty _animationTriggers;
        private SerializedProperty _navigation;
        private SerializedProperty _onClick;

        private bool _showSetup;
        private bool _showNavigation;

        private void OnEnable()
        {
            _styleSource = serializedObject.FindProperty("_styleSource");
            _style = serializedObject.FindProperty("_style");
            _resolveTargetsAutomatically = serializedObject.FindProperty("_resolveTargetsAutomatically");
            _label = serializedObject.FindProperty("_label");
            _icon = serializedObject.FindProperty("_icon");
            _interactable = serializedObject.FindProperty("m_Interactable");
            _targetGraphic = serializedObject.FindProperty("m_TargetGraphic");
            _transition = serializedObject.FindProperty("m_Transition");
            _colors = serializedObject.FindProperty("m_Colors");
            _spriteState = serializedObject.FindProperty("m_SpriteState");
            _animationTriggers = serializedObject.FindProperty("m_AnimationTriggers");
            _navigation = serializedObject.FindProperty("m_Navigation");
            _onClick = serializedObject.FindProperty("m_OnClick");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScript();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_styleSource, new GUIContent("Source"));
            DrawAppearance();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_interactable);
            EditorGUILayout.PropertyField(_onClick);

            EditorGUILayout.Space(2f);
            _showNavigation = EditorGUILayout.Foldout(_showNavigation, "Navigation", true);
            if (_showNavigation)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_navigation);
                }
            }

            EditorGUILayout.Space(2f);
            _showSetup = EditorGUILayout.Foldout(_showSetup, "Setup", true);
            if (_showSetup)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (UsesDesignSystemAppearance())
                    {
                        EditorGUILayout.PropertyField(_targetGraphic);
                    }

                    EditorGUILayout.PropertyField(_resolveTargetsAutomatically, new GUIContent("Resolve Targets Automatically"));

                    using (new EditorGUI.DisabledScope(
                               !_resolveTargetsAutomatically.hasMultipleDifferentValues &&
                               _resolveTargetsAutomatically.boolValue))
                    {
                        EditorGUILayout.PropertyField(_label);
                        EditorGUILayout.PropertyField(_icon);
                    }
                }

                if (GUILayout.Button("Resolve Targets"))
                {
                    ResolveTargets();
                }
            }

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

        private void DrawAppearance()
        {
            bool designSystem = !_styleSource.hasMultipleDifferentValues &&
                                _styleSource.enumValueIndex == (int)LoogaControlStyleSource.DesignSystem;

            if (designSystem)
            {
                EditorGUILayout.PropertyField(_style);

                if (_style.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(
                        "Assign a button style to control this button's appearance.",
                        MessageType.Warning);
                    return;
                }

                EditorGUILayout.HelpBox(
                    "The assigned button style controls appearance. Button behavior remains local.",
                    MessageType.Info);
                return;
            }

            if (_styleSource.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Selected buttons use different appearance sources.",
                    MessageType.None);
                return;
            }

            DrawLocalAppearance();
        }

        private bool UsesDesignSystemAppearance()
        {
            return _styleSource.hasMultipleDifferentValues ||
                   _styleSource.enumValueIndex == (int)LoogaControlStyleSource.DesignSystem;
        }

        private void DrawLocalAppearance()
        {
            EditorGUILayout.PropertyField(_targetGraphic);
            EditorGUILayout.PropertyField(_transition);

            Selectable.Transition transition = (Selectable.Transition)_transition.enumValueIndex;
            switch (transition)
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

        private void DrawScript()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour((LoogaButton)target),
                    typeof(MonoScript),
                    false);
            }
        }

        private void ApplyStyles()
        {
            foreach (Object inspectedTarget in targets)
            {
                if (inspectedTarget is LoogaButton button)
                {
                    button.ApplyStyle();
                    EditorUtility.SetDirty(button);
                }
            }
        }

        private void ResolveTargets()
        {
            foreach (Object inspectedTarget in targets)
            {
                if (inspectedTarget is not LoogaButton button)
                {
                    continue;
                }

                Undo.RecordObject(button, "Resolve Looga Button Targets");
                button.ResolveTargets();
                button.ApplyStyle();
                EditorUtility.SetDirty(button);
            }

            serializedObject.Update();
        }
    }
}
