using System;
using System.Collections.Generic;
using LoogaSoft.UI.Controls;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor.Controls
{
    /// <summary>Converts Unity controls while preserving serialized component identity and references.</summary>
    public static class LoogaControlMigration
    {
        private const string SelectionMenuPath = "GameObject/LoogaSoft/UI/Convert Selected Controls";
        private const string LoadedScenesMenuPath = "LoogaSoft/UI/Controls/Convert Loaded Scene Controls";

        private static readonly Dictionary<Type, MonoScript> ScriptCache = new Dictionary<Type, MonoScript>();

        [MenuItem("CONTEXT/Button/Convert To Looga Button", true)]
        private static bool ValidateButton(MenuCommand command) => command.context is Button and not LoogaButton;
        [MenuItem("CONTEXT/Button/Convert To Looga Button")]
        private static void ConvertButton(MenuCommand command) => Convert((Button)command.context, typeof(LoogaButton));

        [MenuItem("CONTEXT/Toggle/Convert To Looga Toggle", true)]
        private static bool ValidateToggle(MenuCommand command) => command.context is Toggle and not LoogaToggle;
        [MenuItem("CONTEXT/Toggle/Convert To Looga Toggle")]
        private static void ConvertToggle(MenuCommand command) => Convert((Toggle)command.context, typeof(LoogaToggle));

        [MenuItem("CONTEXT/Slider/Convert To Looga Slider", true)]
        private static bool ValidateSlider(MenuCommand command) => command.context is Slider and not LoogaSlider;
        [MenuItem("CONTEXT/Slider/Convert To Looga Slider")]
        private static void ConvertSlider(MenuCommand command) => Convert((Slider)command.context, typeof(LoogaSlider));

        [MenuItem("CONTEXT/Scrollbar/Convert To Looga Scrollbar", true)]
        private static bool ValidateScrollbar(MenuCommand command) => command.context is Scrollbar and not LoogaScrollbar;
        [MenuItem("CONTEXT/Scrollbar/Convert To Looga Scrollbar")]
        private static void ConvertScrollbar(MenuCommand command) => Convert((Scrollbar)command.context, typeof(LoogaScrollbar));

        [MenuItem("CONTEXT/TMP_InputField/Convert To Looga Input Field", true)]
        private static bool ValidateInput(MenuCommand command) => command.context is TMP_InputField and not LoogaInputField;
        [MenuItem("CONTEXT/TMP_InputField/Convert To Looga Input Field")]
        private static void ConvertInput(MenuCommand command) => Convert((TMP_InputField)command.context, typeof(LoogaInputField));

        [MenuItem("CONTEXT/TMP_Dropdown/Convert To Looga Dropdown", true)]
        private static bool ValidateDropdown(MenuCommand command) => command.context is TMP_Dropdown and not LoogaDropdown;
        [MenuItem("CONTEXT/TMP_Dropdown/Convert To Looga Dropdown")]
        private static void ConvertDropdown(MenuCommand command) => Convert((TMP_Dropdown)command.context, typeof(LoogaDropdown));

        [MenuItem(SelectionMenuPath, true)]
        private static bool ValidateSelection()
        {
            return Array.Exists(Selection.gameObjects, HasConvertibleControl);
        }

        [MenuItem(SelectionMenuPath, false, 15)]
        private static void ConvertSelection()
        {
            HashSet<Selectable> controls = new HashSet<Selectable>();
            foreach (GameObject selectedObject in Selection.gameObjects)
            {
                controls.UnionWith(selectedObject.GetComponentsInChildren<Selectable>(true));
            }

            UnityEngine.Object previousSelection = Selection.activeObject;
            int undoGroup = BeginUndoGroup("Convert Selected UI Controls");
            foreach (Selectable control in controls)
            {
                Type targetType = GetTargetType(control);
                if (targetType != null)
                {
                    Convert(control, targetType, false);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeObject = previousSelection;
        }

        [MenuItem(LoadedScenesMenuPath, false, 110)]
        private static void ConvertLoadedScenes()
        {
            List<Selectable> controls = FindLoadedSceneControls();
            if (controls.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Convert Loaded Scene Controls",
                    "The loaded scenes contain no Unity controls that need conversion.",
                    "OK");
                return;
            }

            string summary = BuildSummary(controls);
            bool confirmed = EditorUtility.DisplayDialog(
                "Convert Loaded Scene Controls",
                $"Convert {controls.Count} control(s) in every loaded scene?\n\n{summary}\n\n" +
                "Inactive objects are included. Existing appearance is preserved in Local style mode. " +
                "The entire pass can be undone as one operation.",
                "Convert All",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            UnityEngine.Object previousSelection = Selection.activeObject;
            int undoGroup = BeginUndoGroup("Convert Loaded Scene UI Controls");
            int convertedCount = 0;

            try
            {
                for (int index = 0; index < controls.Count; index++)
                {
                    Selectable control = controls[index];
                    EditorUtility.DisplayProgressBar(
                        "Converting UI Controls",
                        control != null ? control.name : "Control",
                        (index + 1f) / controls.Count);

                    Type targetType = GetTargetType(control);
                    if (targetType != null && Convert(control, targetType, false))
                    {
                        convertedCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeObject = previousSelection;
            }

            string result = $"Converted {convertedCount} of {controls.Count} loaded-scene UI controls.";
            Debug.Log($"Looga UI: {result} Existing appearance remains in Local style mode.");
            EditorUtility.DisplayDialog("UI Control Conversion Complete", result, "OK");
        }

        private static bool HasConvertibleControl(GameObject gameObject)
        {
            Selectable[] controls = gameObject.GetComponentsInChildren<Selectable>(true);
            return Array.Exists(controls, control => GetTargetType(control) != null);
        }

        private static List<Selectable> FindLoadedSceneControls()
        {
            List<Selectable> controls = new List<Selectable>();
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Selectable[] sceneControls = root.GetComponentsInChildren<Selectable>(true);
                    foreach (Selectable control in sceneControls)
                    {
                        if (GetTargetType(control) != null)
                        {
                            controls.Add(control);
                        }
                    }
                }
            }

            return controls;
        }

        private static string BuildSummary(IReadOnlyList<Selectable> controls)
        {
            int buttons = 0;
            int toggles = 0;
            int sliders = 0;
            int scrollbars = 0;
            int inputs = 0;
            int dropdowns = 0;

            foreach (Selectable control in controls)
            {
                switch (control)
                {
                    case Button:
                        buttons++;
                        break;
                    case Toggle:
                        toggles++;
                        break;
                    case Slider:
                        sliders++;
                        break;
                    case Scrollbar:
                        scrollbars++;
                        break;
                    case TMP_InputField:
                        inputs++;
                        break;
                    case TMP_Dropdown:
                        dropdowns++;
                        break;
                }
            }

            return $"Buttons: {buttons}\n" +
                   $"Toggles: {toggles}\n" +
                   $"Sliders: {sliders}\n" +
                   $"Scrollbars: {scrollbars}\n" +
                   $"Input fields: {inputs}\n" +
                   $"Dropdowns: {dropdowns}";
        }

        private static Type GetTargetType(Selectable control)
        {
            return control switch
            {
                LoogaButton or LoogaToggle or LoogaSlider or LoogaScrollbar or LoogaInputField or LoogaDropdown => null,
                Button => typeof(LoogaButton),
                Toggle => typeof(LoogaToggle),
                Slider => typeof(LoogaSlider),
                Scrollbar => typeof(LoogaScrollbar),
                TMP_InputField => typeof(LoogaInputField),
                TMP_Dropdown => typeof(LoogaDropdown),
                _ => null
            };
        }

        private static bool Convert(Selectable control, Type targetType, bool selectResult = true)
        {
            if (control == null || targetType == null)
            {
                return false;
            }

            MonoScript script = GetScript(targetType);
            if (script == null)
            {
                Debug.LogError($"Looga UI could not locate the {targetType.Name} script.", control);
                return false;
            }

            GameObject owner = control.gameObject;
            Undo.RecordObject(control, $"Convert To {targetType.Name}");
            SerializedObject serializedControl = new SerializedObject(control);
            serializedControl.FindProperty("m_Script").objectReferenceValue = script;
            serializedControl.ApplyModifiedPropertiesWithoutUndo();

            Component converted = owner.GetComponent(targetType);
            if (converted == null)
            {
                Debug.LogError($"Looga UI could not convert {owner.name} to {targetType.Name}.", owner);
                return false;
            }

            SerializedObject serializedConverted = new SerializedObject(converted);
            SerializedProperty styleSource = serializedConverted.FindProperty("_styleSource");
            if (styleSource != null)
            {
                styleSource.enumValueIndex = (int)LoogaControlStyleSource.Local;
            }

            serializedConverted.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(converted);
            EditorUtility.SetDirty(converted);

            if (owner.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(owner.scene);
            }

            if (selectResult)
            {
                Selection.activeObject = converted;
            }

            return true;
        }

        private static int BeginUndoGroup(string name)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
            return undoGroup;
        }

        private static MonoScript GetScript(Type targetType)
        {
            if (!ScriptCache.TryGetValue(targetType, out MonoScript script) || script == null)
            {
                script = FindScript(targetType);
                ScriptCache[targetType] = script;
            }

            return script;
        }

        private static MonoScript FindScript(Type targetType)
        {
            string[] candidates = AssetDatabase.FindAssets($"{targetType.Name} t:MonoScript");
            foreach (string candidate in candidates)
            {
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(candidate));
                if (script != null && script.GetClass() == targetType)
                {
                    return script;
                }
            }

            return null;
        }
    }
}
