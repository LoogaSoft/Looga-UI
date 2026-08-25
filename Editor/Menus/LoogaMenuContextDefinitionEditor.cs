using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuContextDefinition))]
    public sealed class LoogaMenuContextDefinitionEditor : LoogaEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = LoogaUiToolkitStyle.CreateInspectorRoot();
            PropertyField description = LoogaUiToolkitStyle.CreatePropertyField(
                serializedObject,
                "_description");
            PropertyField regions = LoogaUiToolkitStyle.CreatePropertyField(
                serializedObject,
                "_regionOverrides",
                "Region Overrides");
            if (description != null)
                root.Add(description);
            if (regions != null)
                root.Add(regions);
            AddRetainedValidation(root, (LoogaMenuContextDefinition)target);
            root.Bind(serializedObject);
            return root;
        }

        private static void AddRetainedValidation(
            VisualElement root,
            LoogaMenuContextDefinition context)
        {
            foreach (LoogaMenuRegionOverride regionOverride in context.RegionOverrides)
            {
                if (regionOverride == null || regionOverride.Mode == LoogaMenuRegionMode.Inherit)
                    continue;

                if (regionOverride.Region == null)
                {
                    root.Add(new HelpBox(
                        "A context region is missing its region definition.",
                        HelpBoxMessageType.Error));
                }
                else if (regionOverride.Mode is LoogaMenuRegionMode.Override or LoogaMenuRegionMode.Add
                    && regionOverride.Content == null)
                {
                    root.Add(new HelpBox(
                        $"{regionOverride.Region.DisplayName} has no authored content.",
                        HelpBoxMessageType.Warning));
                }
                else if (regionOverride.Mode == LoogaMenuRegionMode.Add
                    && !regionOverride.Content.SupportsAdd)
                {
                    root.Add(new HelpBox(
                        $"{regionOverride.Region.DisplayName} does not support Add. Use Override instead.",
                        HelpBoxMessageType.Error));
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScriptField();
            LoogaGUILayout.PropertyField(serializedObject.FindProperty("_description"));
            LoogaMenuScreenAuthoringGUI.DrawRegions(
                serializedObject.FindProperty("_regionOverrides"));
            serializedObject.ApplyModifiedProperties();
            DrawValidation((LoogaMenuContextDefinition)target);
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromScriptableObject((LoogaMenuContextDefinition)target),
                    typeof(MonoScript),
                    false);
            }
        }

        private static void DrawValidation(LoogaMenuContextDefinition context)
        {
            foreach (LoogaMenuRegionOverride regionOverride in context.RegionOverrides)
            {
                if (regionOverride == null || regionOverride.Mode == LoogaMenuRegionMode.Inherit)
                    continue;

                if (regionOverride.Region == null)
                {
                    EditorGUILayout.HelpBox(
                        "A context region is missing its region definition.",
                        MessageType.Error);
                    continue;
                }

                if (regionOverride.Mode is LoogaMenuRegionMode.Override or LoogaMenuRegionMode.Add
                    && regionOverride.Content == null)
                {
                    EditorGUILayout.HelpBox(
                        $"{regionOverride.Region.DisplayName} has no authored content.",
                        MessageType.Warning);
                    continue;
                }

                if (regionOverride.Mode == LoogaMenuRegionMode.Add
                    && !regionOverride.Content.SupportsAdd)
                {
                    EditorGUILayout.HelpBox(
                        $"{regionOverride.Region.DisplayName} does not support Add. Use Override instead.",
                        MessageType.Error);
                }

                if (regionOverride.Content != null
                    && !regionOverride.Region.ContentType.IsInstanceOfType(regionOverride.Content))
                {
                    EditorGUILayout.HelpBox(
                        $"{regionOverride.Region.DisplayName} contains the wrong content type.",
                        MessageType.Error);
                }
            }
        }
    }
}
