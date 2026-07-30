using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor
{
    [CustomEditor(typeof(LoogaLayout))]
    sealed class LoogaLayoutEditor : UnityEditor.Editor
    {
        SerializedProperty _mode;
        SerializedProperty _width;
        SerializedProperty _height;
        SerializedProperty _fixedSize;
        SerializedProperty _minimumSize;
        SerializedProperty _maximumSize;
        SerializedProperty _padding;
        SerializedProperty _childAlignment;
        SerializedProperty _childWidth;
        SerializedProperty _childHeight;
        SerializedProperty _fixedChildSize;
        SerializedProperty _spacing;
        SerializedProperty _lineSpacing;
        SerializedProperty _reverseOrder;
        SerializedProperty _gridConstraint;
        SerializedProperty _gridConstraintCount;
        SerializedProperty _gridCellMode;
        SerializedProperty _gridCellSize;
        SerializedProperty _gridSpacing;

        bool _showDiagnostics;

        void OnEnable()
        {
            _mode = serializedObject.FindProperty("_mode");
            _width = serializedObject.FindProperty("_width");
            _height = serializedObject.FindProperty("_height");
            _fixedSize = serializedObject.FindProperty("_fixedSize");
            _minimumSize = serializedObject.FindProperty("_minimumSize");
            _maximumSize = serializedObject.FindProperty("_maximumSize");
            _padding = serializedObject.FindProperty("m_Padding");
            _childAlignment = serializedObject.FindProperty("m_ChildAlignment");
            _childWidth = serializedObject.FindProperty("_childWidth");
            _childHeight = serializedObject.FindProperty("_childHeight");
            _fixedChildSize = serializedObject.FindProperty("_fixedChildSize");
            _spacing = serializedObject.FindProperty("_spacing");
            _lineSpacing = serializedObject.FindProperty("_lineSpacing");
            _reverseOrder = serializedObject.FindProperty("_reverseOrder");
            _gridConstraint = serializedObject.FindProperty("_gridConstraint");
            _gridConstraintCount = serializedObject.FindProperty("_gridConstraintCount");
            _gridCellMode = serializedObject.FindProperty("_gridCellMode");
            _gridCellSize = serializedObject.FindProperty("_gridCellSize");
            _gridSpacing = serializedObject.FindProperty("_gridSpacing");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScript();

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Arrangement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_mode);
            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_childAlignment, new GUIContent("Alignment"));

            LoogaLayoutMode mode = (LoogaLayoutMode)_mode.enumValueIndex;
            DrawArrangement(mode);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Container Sizing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_width);
            EditorGUILayout.PropertyField(_height);
            DrawContainerSizeFields();

            if (mode != LoogaLayoutMode.Grid)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Child Sizing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_childWidth);
                EditorGUILayout.PropertyField(_childHeight);

                if (UsesFixedChildSize())
                {
                    EditorGUILayout.PropertyField(_fixedChildSize);
                }
            }

            serializedObject.ApplyModifiedProperties();

            DrawValidation(mode);
            DrawDiagnostics();
        }

        void DrawArrangement(LoogaLayoutMode mode)
        {
            switch (mode)
            {
                case LoogaLayoutMode.Horizontal:
                case LoogaLayoutMode.Vertical:
                    EditorGUILayout.PropertyField(_spacing);
                    EditorGUILayout.PropertyField(_reverseOrder);
                    break;

                case LoogaLayoutMode.Grid:
                    EditorGUILayout.PropertyField(_gridConstraint);

                    if ((LoogaGridConstraint)_gridConstraint.enumValueIndex != LoogaGridConstraint.Flexible)
                    {
                        EditorGUILayout.PropertyField(_gridConstraintCount, new GUIContent("Count"));
                    }

                    EditorGUILayout.PropertyField(_gridCellMode);
                    if ((LoogaGridCellMode)_gridCellMode.enumValueIndex == LoogaGridCellMode.Fixed)
                    {
                        EditorGUILayout.PropertyField(_gridCellSize);
                    }

                    EditorGUILayout.PropertyField(_gridSpacing);
                    EditorGUILayout.PropertyField(_reverseOrder);
                    break;

                case LoogaLayoutMode.Flow:
                    EditorGUILayout.PropertyField(_spacing, new GUIContent("Item Spacing"));
                    EditorGUILayout.PropertyField(_lineSpacing);
                    EditorGUILayout.PropertyField(_reverseOrder);
                    break;

                case LoogaLayoutMode.Overlay:
                    EditorGUILayout.HelpBox("Overlay places every child in the same content area using the selected alignment.", MessageType.None);
                    break;
            }
        }

        void DrawContainerSizeFields()
        {
            LoogaLayoutSizeMode width = (LoogaLayoutSizeMode)_width.enumValueIndex;
            LoogaLayoutSizeMode height = (LoogaLayoutSizeMode)_height.enumValueIndex;

            if (width == LoogaLayoutSizeMode.Fixed || height == LoogaLayoutSizeMode.Fixed)
            {
                EditorGUILayout.PropertyField(_fixedSize);
            }

            if (width == LoogaLayoutSizeMode.ClampedContent || height == LoogaLayoutSizeMode.ClampedContent)
            {
                EditorGUILayout.PropertyField(_minimumSize);
                EditorGUILayout.PropertyField(_maximumSize);
            }
        }

        void DrawValidation(LoogaLayoutMode mode)
        {
            LoogaLayout layout = (LoogaLayout)target;

            if (layout.TryGetComponent(out ContentSizeFitter _))
            {
                EditorGUILayout.HelpBox(
                    "Remove Content Size Fitter. Looga Layout already measures its children and can size this container.",
                    MessageType.Error);
            }

            if (layout.TryGetComponent(out HorizontalOrVerticalLayoutGroup _)
                || layout.TryGetComponent(out GridLayoutGroup _))
            {
                EditorGUILayout.HelpBox(
                    "Another layout group is controlling the same children. Keep only one layout controller on this object.",
                    MessageType.Error);
            }

            LoogaLayoutSizeMode width = (LoogaLayoutSizeMode)_width.enumValueIndex;
            LoogaLayoutSizeMode height = (LoogaLayoutSizeMode)_height.enumValueIndex;
            LoogaLayoutChildSizeMode childWidth = (LoogaLayoutChildSizeMode)_childWidth.enumValueIndex;
            LoogaLayoutChildSizeMode childHeight = (LoogaLayoutChildSizeMode)_childHeight.enumValueIndex;

            if (SizesToContent(width) && childWidth == LoogaLayoutChildSizeMode.Fill)
            {
                EditorGUILayout.HelpBox(
                    "Width fits its children while child widths fill the available width. Change one side to Content to avoid a sizing cycle.",
                    MessageType.Warning);
            }

            if (SizesToContent(height) && childHeight == LoogaLayoutChildSizeMode.Fill)
            {
                EditorGUILayout.HelpBox(
                    "Height fits its children while child heights fill the available height. Change one side to Content to avoid a sizing cycle.",
                    MessageType.Warning);
            }

            if (mode == LoogaLayoutMode.Grid
                && (LoogaGridConstraint)_gridConstraint.enumValueIndex == LoogaGridConstraint.Flexible
                && SizesToContent(width))
            {
                EditorGUILayout.HelpBox(
                    "A flexible grid needs an authored or parent-provided width to determine its column count.",
                    MessageType.Warning);
            }

            if (mode == LoogaLayoutMode.Flow && SizesToContent(width))
            {
                EditorGUILayout.HelpBox(
                    "A Flow layout with content-sized width naturally becomes one unwrapped row. Use Authored, Fill Parent, or Clamped Content width to wrap.",
                    MessageType.Info);
            }
        }

        void DrawDiagnostics()
        {
            _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics, "Calculated Size", true);
            if (!_showDiagnostics)
            {
                return;
            }

            LoogaLayout layout = (LoogaLayout)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector2Field("Content Minimum", layout.ContentMinimum);
                EditorGUILayout.Vector2Field("Content Preferred", layout.ContentPreferred);
                EditorGUILayout.Vector2Field("Reported Minimum", layout.ReportedMinimum);
                EditorGUILayout.Vector2Field("Reported Preferred", layout.ReportedPreferred);
            }
        }

        void DrawScript()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour((LoogaLayout)target),
                    typeof(MonoScript),
                    false);
            }
        }

        bool UsesFixedChildSize()
        {
            return (LoogaLayoutChildSizeMode)_childWidth.enumValueIndex == LoogaLayoutChildSizeMode.Fixed
                || (LoogaLayoutChildSizeMode)_childHeight.enumValueIndex == LoogaLayoutChildSizeMode.Fixed;
        }

        static bool SizesToContent(LoogaLayoutSizeMode mode)
        {
            return mode is LoogaLayoutSizeMode.Content or LoogaLayoutSizeMode.ClampedContent;
        }
    }
}
