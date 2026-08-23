using LoogaSoft.Inspector.Editor;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuOpenButton))]
    public sealed class LoogaMenuOpenButtonEditor : LoogaEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            EditorGUILayout.Space(1f);
            DrawHeaderAttributes(target.GetType());
            DrawLoogaProperty("_destination");

            SerializedProperty useActiveMenuRoot = DrawLoogaProperty("_useActiveMenuRoot");
            if (!useActiveMenuRoot.boolValue)
                DrawLoogaProperty("_menuRoot");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
