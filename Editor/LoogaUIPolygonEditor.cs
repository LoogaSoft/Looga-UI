using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UIFX.Editor
{
    [CustomEditor(typeof(LoogaUIPolygon))]
    sealed class LoogaUIPolygonEditor : UnityEditor.Editor
    {
        SerializedProperty _cornerRadius;
        SerializedProperty _cornerRadii;

        void OnEnable()
        {
            _cornerRadius = serializedObject.FindProperty("_cornerRadius");
            _cornerRadii = serializedObject.FindProperty("_cornerRadii");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (_cornerRadius != null && _cornerRadii != null && _cornerRadii.arraySize > 0 && GUILayout.Button("Apply Default Radius To All Corners"))
            {
                for (int i = 0; i < _cornerRadii.arraySize; i++)
                {
                    _cornerRadii.GetArrayElementAtIndex(i).floatValue = _cornerRadius.floatValue;
                }

                if (target is Graphic graphic)
                {
                    graphic.SetVerticesDirty();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
