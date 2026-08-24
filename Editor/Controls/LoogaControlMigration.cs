using System;
using System.Collections.Generic;
using LoogaSoft.UI.Controls;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor.Controls
{
    /// <summary>Converts Unity controls while preserving serialized component identity and references.</summary>
    public static class LoogaControlMigration
    {
        private const string SelectionMenuPath = "GameObject/LoogaSoft/UI/Convert Selected Controls";

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

            foreach (Selectable control in controls)
            {
                Type targetType = GetTargetType(control);
                if (targetType != null)
                {
                    Convert(control, targetType);
                }
            }
        }

        private static bool HasConvertibleControl(GameObject gameObject)
        {
            Selectable[] controls = gameObject.GetComponentsInChildren<Selectable>(true);
            return Array.Exists(controls, control => GetTargetType(control) != null);
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

        private static void Convert(Selectable control, Type targetType)
        {
            if (control == null || targetType == null)
            {
                return;
            }

            MonoScript script = FindScript(targetType);
            if (script == null)
            {
                Debug.LogError($"Looga UI could not locate the {targetType.Name} script.", control);
                return;
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
                return;
            }

            SerializedObject serializedConverted = new SerializedObject(converted);
            serializedConverted.FindProperty("_styleSource").enumValueIndex = (int)LoogaControlStyleSource.Local;
            serializedConverted.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(converted);
            EditorUtility.SetDirty(converted);

            if (owner.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(owner.scene);
            }

            Selection.activeObject = converted;
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
