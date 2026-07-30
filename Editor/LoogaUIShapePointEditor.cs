using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor
{
    [CustomEditor(typeof(LoogaUICustomShape))]
    sealed class LoogaUICustomShapePointEditor : LoogaUIShapePointEditor
    {
    }

    [CustomEditor(typeof(LoogaUILineRenderer))]
    sealed class LoogaUILineRendererPointEditor : LoogaUIShapePointEditor
    {
    }

    abstract class LoogaUIShapePointEditor : UnityEditor.Editor
    {
        const float HandleVisualSize = 0.055f;
        const float AddHandleVisualSize = 0.04f;

        SerializedProperty _points;
        SerializedProperty _cornerRadii;
        SerializedProperty _cornerRadius;
        int _selectedPoint = -1;

        void OnEnable()
        {
            _points = serializedObject.FindProperty("_points");
            _cornerRadii = serializedObject.FindProperty("_cornerRadii");
            _cornerRadius = serializedObject.FindProperty("_cornerRadius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Point"))
                {
                    AddPointAtEnd();
                }

                using (new EditorGUI.DisabledScope(_selectedPoint < 0 || _selectedPoint >= _points.arraySize))
                {
                    if (GUILayout.Button("Remove Selected"))
                    {
                        RemovePoint(_selectedPoint);
                        _selectedPoint = Mathf.Clamp(_selectedPoint - 1, -1, _points.arraySize - 1);
                    }
                }
            }

            if (_cornerRadii != null && _cornerRadius != null && GUILayout.Button("Apply Default Radius To All Points"))
            {
                ApplyDefaultCornerRadius();
            }

            if (_selectedPoint >= 0 && _selectedPoint < _points.arraySize)
            {
                EditorGUILayout.LabelField("Selected Point", _selectedPoint.ToString());
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            if (targets.Length != 1 || _points == null)
            {
                return;
            }

            serializedObject.Update();
            if (target is not Graphic graphic || graphic.rectTransform == null)
            {
                return;
            }

            RectTransform rectTransform = graphic.rectTransform;
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            DrawInsertHandles(graphic, rectTransform, rect);
            Handles.color = new Color(0.25f, 0.6f, 1f, 0.95f);
            for (int i = 0; i < _points.arraySize; i++)
            {
                SerializedProperty pointProperty = _points.GetArrayElementAtIndex(i);
                Vector2 normalizedPoint = pointProperty.vector2Value;
                Vector3 worldPoint = rectTransform.TransformPoint(ToLocalPoint(rect, normalizedPoint));
                float size = HandleUtility.GetHandleSize(worldPoint) * HandleVisualSize;

                if (Handles.Button(worldPoint, SceneView.currentDrawingSceneView.camera.transform.rotation, size * 0.8f, size * 0.8f, Handles.DotHandleCap))
                {
                    _selectedPoint = i;
                    Repaint();
                }

                EditorGUI.BeginChangeCheck();
                Vector3 movedWorldPoint = Handles.FreeMoveHandle(worldPoint, size, Vector3.zero, Handles.DotHandleCap);
                Handles.Label(worldPoint, i.ToString());
                if (!EditorGUI.EndChangeCheck())
                {
                    continue;
                }

                Undo.RecordObject(target, "Move UI Shape Point");
                Vector2 movedLocalPoint = rectTransform.InverseTransformPoint(movedWorldPoint);
                pointProperty.vector2Value = ToNormalizedPoint(rect, movedLocalPoint);
                serializedObject.ApplyModifiedProperties();
                graphic.SetVerticesDirty();
                EditorUtility.SetDirty(target);
            }
        }

        void DrawInsertHandles(Graphic graphic, RectTransform rectTransform, Rect rect)
        {
            if (_points.arraySize < 2)
            {
                return;
            }

            bool closed = target is LoogaUICustomShape;
            int segmentCount = closed ? _points.arraySize : _points.arraySize - 1;
            Handles.color = new Color(0.3f, 0.9f, 0.55f, 0.85f);
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 a = _points.GetArrayElementAtIndex(i).vector2Value;
                Vector2 b = _points.GetArrayElementAtIndex((i + 1) % _points.arraySize).vector2Value;
                Vector3 worldA = rectTransform.TransformPoint(ToLocalPoint(rect, a));
                Vector3 worldB = rectTransform.TransformPoint(ToLocalPoint(rect, b));
                Vector3 midpoint = (worldA + worldB) * 0.5f;
                float size = HandleUtility.GetHandleSize(midpoint) * AddHandleVisualSize;

                if (!Handles.Button(midpoint, SceneView.currentDrawingSceneView.camera.transform.rotation, size, size, Handles.RectangleHandleCap))
                {
                    continue;
                }

                InsertPoint(i + 1, ToNormalizedPoint(rect, rectTransform.InverseTransformPoint(midpoint)));
                _selectedPoint = i + 1;
                graphic.SetVerticesDirty();
                EditorUtility.SetDirty(target);
                Event.current.Use();
                break;
            }
        }

        void AddPointAtEnd()
        {
            Vector2 point = _points.arraySize > 0 ? _points.GetArrayElementAtIndex(_points.arraySize - 1).vector2Value + new Vector2(0.08f, 0f) : Vector2.zero;
            InsertPoint(_points.arraySize, point);
            _selectedPoint = _points.arraySize - 1;
        }

        void InsertPoint(int index, Vector2 point)
        {
            Undo.RecordObject(target, "Add UI Shape Point");
            serializedObject.Update();
            index = Mathf.Clamp(index, 0, _points.arraySize);
            _points.InsertArrayElementAtIndex(index);
            _points.GetArrayElementAtIndex(index).vector2Value = point;
            if (_cornerRadii != null)
            {
                _cornerRadii.InsertArrayElementAtIndex(index);
                _cornerRadii.GetArrayElementAtIndex(index).floatValue = 0f;
            }

            serializedObject.ApplyModifiedProperties();
        }

        void RemovePoint(int index)
        {
            Undo.RecordObject(target, "Remove UI Shape Point");
            serializedObject.Update();
            if (index < 0 || index >= _points.arraySize)
            {
                return;
            }

            _points.DeleteArrayElementAtIndex(index);
            if (_cornerRadii != null && index < _cornerRadii.arraySize)
            {
                _cornerRadii.DeleteArrayElementAtIndex(index);
            }

            serializedObject.ApplyModifiedProperties();
            if (target is Graphic graphic)
            {
                graphic.SetVerticesDirty();
            }
        }

        void ApplyDefaultCornerRadius()
        {
            Undo.RecordObject(target, "Apply UI Shape Corner Radius");
            serializedObject.Update();
            for (int i = 0; i < _cornerRadii.arraySize; i++)
            {
                _cornerRadii.GetArrayElementAtIndex(i).floatValue = _cornerRadius.floatValue;
            }

            serializedObject.ApplyModifiedProperties();
            if (target is Graphic graphic)
            {
                graphic.SetVerticesDirty();
            }
        }

        static Vector2 ToLocalPoint(Rect rect, Vector2 normalizedPoint)
        {
            return new Vector2(rect.center.x + normalizedPoint.x * rect.width, rect.center.y + normalizedPoint.y * rect.height);
        }

        static Vector2 ToNormalizedPoint(Rect rect, Vector2 localPoint)
        {
            return new Vector2(
                rect.width > 0f ? (localPoint.x - rect.center.x) / rect.width : 0f,
                rect.height > 0f ? (localPoint.y - rect.center.y) / rect.height : 0f);
        }
    }
}
