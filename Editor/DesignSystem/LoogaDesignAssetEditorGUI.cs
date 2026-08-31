using System;
using System.Collections.Generic;
using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Draws shared design-asset inspector controls.</summary>
    internal static class LoogaDesignAssetEditorGUI
    {
        private const float NameButtonWidth = 44f;
        private const float ReferenceFoldoutInset = 12f;
        private const float ReferenceLabelWidth = 112f;
        private const float NewButtonWidth = 38f;
        private const float CloneButtonWidth = 45f;

        private static readonly GUIContent[] HorizontalOrigins =
        {
            new("Left"),
            new("Right")
        };

        private static readonly GUIContent[] VerticalOrigins =
        {
            new("Bottom"),
            new("Top")
        };

        private static readonly GUIContent[] Radial90Origins =
        {
            new("Bottom Left"),
            new("Top Left"),
            new("Top Right"),
            new("Bottom Right")
        };

        private static readonly GUIContent[] Radial180Origins =
        {
            new("Bottom"),
            new("Left"),
            new("Top"),
            new("Right")
        };

        private static readonly GUIContent[] Radial360Origins =
        {
            new("Bottom"),
            new("Right"),
            new("Top"),
            new("Left")
        };

        internal static void DrawAsset(LoogaDesignAsset asset)
        {
            DrawName(asset);
            EditorGUILayout.Space();
            DrawAssetFields(asset);
        }

        internal static void DrawName(LoogaDesignAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            string stateKey = $"LoogaDesignAssetName_{asset.GetInstanceID()}";
            string draftKey = $"{stateKey}_Draft";
            string controlName = $"{stateKey}_Field";
            bool editing = SessionState.GetBool(stateKey, false);
            string draft = SessionState.GetString(draftKey, asset.name);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Name");
                GUI.SetNextControlName(controlName);
                using (new EditorGUI.DisabledScope(!editing))
                {
                    draft = EditorGUILayout.TextField(editing ? draft : asset.name);
                }

                if (GUILayout.Button(editing ? "Save" : "Edit", GUILayout.Width(NameButtonWidth)))
                {
                    if (editing)
                    {
                        SaveName(asset, draft);
                        SessionState.EraseString(draftKey);
                        GUI.FocusControl(null);
                    }
                    else
                    {
                        draft = asset.name;
                        SessionState.SetString(draftKey, draft);
                        EditorGUI.FocusTextInControl(controlName);
                    }

                    editing = !editing;
                    SessionState.SetBool(stateKey, editing);
                }
            }

            if (editing)
            {
                SessionState.SetString(draftKey, draft);
                if (Event.current.type == EventType.KeyDown &&
                    GUI.GetNameOfFocusedControl() == controlName &&
                    (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    SaveName(asset, draft);
                    SessionState.SetBool(stateKey, false);
                    SessionState.EraseString(draftKey);
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }
        }

        internal static void DrawAssetFields(LoogaDesignAsset asset)
        {
            if (asset is LoogaImageStyle imageStyle)
            {
                DrawImageStyle(imageStyle);
                return;
            }

            if (asset is LoogaControlStyle controlStyle)
            {
                DrawControlStyle(controlStyle);
                return;
            }

            SerializedObject serializedAsset = new(asset);
            serializedAsset.Update();
            DrawVisibleProperties(serializedAsset);
            ApplyChanges(serializedAsset, asset);
        }

        internal static void DrawExposedReference(
            SerializedProperty property,
            string label,
            Type assetType)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                Rect foldoutRect = new(
                    row.x + ReferenceFoldoutInset,
                    row.y,
                    Mathf.Max(
                        0f,
                        Mathf.Min(ReferenceLabelWidth, row.width - ReferenceFoldoutInset)),
                    row.height);
                Rect cloneRect = new(row.xMax - CloneButtonWidth, row.y, CloneButtonWidth, row.height);
                Rect newRect = new(cloneRect.x - NewButtonWidth, row.y, NewButtonWidth, row.height);
                Rect fieldRect = new(
                    foldoutRect.xMax,
                    row.y,
                    Mathf.Max(0f, newRect.x - foldoutRect.xMax),
                    row.height);

                property.isExpanded = EditorGUI.Foldout(
                    foldoutRect,
                    property.isExpanded,
                    label,
                    true);

                EditorGUI.BeginChangeCheck();
                Object value = EditorGUI.ObjectField(
                    fieldRect,
                    property.objectReferenceValue,
                    assetType,
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    property.objectReferenceValue = value;
                }

                bool multipleTargets = property.serializedObject.isEditingMultipleObjects;
                using (new EditorGUI.DisabledScope(multipleTargets || assetType.IsAbstract))
                {
                    if (GUI.Button(newRect, "New", EditorStyles.miniButton))
                    {
                        LoogaDesignAssetReferenceDrawer.CreateAndAssign(property, assetType, null);
                        GUIUtility.ExitGUI();
                    }
                }

                bool canClone = !multipleTargets && property.objectReferenceValue is LoogaDesignAsset;
                using (new EditorGUI.DisabledScope(!canClone))
                {
                    if (GUI.Button(cloneRect, "Clone", EditorStyles.miniButton))
                    {
                        LoogaDesignAssetReferenceDrawer.CreateAndAssign(
                            property,
                            assetType,
                            property.objectReferenceValue as LoogaDesignAsset);
                        GUIUtility.ExitGUI();
                    }
                }

                if (property.isExpanded &&
                    !property.hasMultipleDifferentValues &&
                    property.objectReferenceValue is LoogaDesignAsset asset)
                {
                    EditorGUI.indentLevel++;
                    DrawAsset(asset);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private static void DrawImageStyle(LoogaImageStyle style)
        {
            SerializedObject serializedStyle = new(style);
            serializedStyle.Update();

            SerializedProperty spriteProperty = serializedStyle.FindProperty("_sprite");
            SerializedProperty typeProperty = serializedStyle.FindProperty("_type");
            EditorGUILayout.PropertyField(spriteProperty, new GUIContent("Source Image"));
            DrawProperties(serializedStyle, "_color", "_material");

            if (spriteProperty.objectReferenceValue is Sprite sprite)
            {
                EditorGUILayout.PropertyField(typeProperty, new GUIContent("Image Type"));
                Image.Type type = (Image.Type)typeProperty.enumValueIndex;
                switch (type)
                {
                    case Image.Type.Simple:
                        DrawProperties(serializedStyle, "_useSpriteMesh", "_preserveAspect");
                        break;
                    case Image.Type.Sliced:
                        DrawSlicedOrTiledFields(serializedStyle, sprite);
                        break;
                    case Image.Type.Tiled:
                        DrawSlicedOrTiledFields(serializedStyle, sprite);
                        break;
                    case Image.Type.Filled:
                        DrawFilledFields(serializedStyle);
                        break;
                }

                EditorGUILayout.PropertyField(
                    serializedStyle.FindProperty("_alphaHitTestMinimumThreshold"));
            }

            ApplyChanges(serializedStyle, style);
        }

        private static void DrawSlicedOrTiledFields(SerializedObject serializedStyle, Sprite sprite)
        {
            if (sprite.border.sqrMagnitude > 0f)
            {
                EditorGUILayout.PropertyField(serializedStyle.FindProperty("_fillCenter"));
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "This sprite has no border. Configure a border in the Sprite Editor for sliced or tiled rendering.",
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedStyle.FindProperty("_pixelsPerUnitMultiplier"));
        }

        private static void DrawFilledFields(SerializedObject serializedStyle)
        {
            EditorGUILayout.PropertyField(serializedStyle.FindProperty("_preserveAspect"));
            SerializedProperty methodProperty = serializedStyle.FindProperty("_fillMethod");
            SerializedProperty originProperty = serializedStyle.FindProperty("_fillOrigin");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(methodProperty);
            if (EditorGUI.EndChangeCheck())
            {
                originProperty.intValue = 0;
            }

            Image.FillMethod method = (Image.FillMethod)methodProperty.enumValueIndex;
            GUIContent[] originLabels = GetFillOriginLabels(method);
            originProperty.intValue = EditorGUILayout.Popup(
                new GUIContent("Fill Origin"),
                Mathf.Clamp(originProperty.intValue, 0, originLabels.Length - 1),
                originLabels);
            EditorGUILayout.PropertyField(serializedStyle.FindProperty("_fillAmount"));

            if (method > Image.FillMethod.Vertical)
            {
                EditorGUILayout.PropertyField(serializedStyle.FindProperty("_fillClockwise"));
            }
        }

        private static GUIContent[] GetFillOriginLabels(Image.FillMethod method)
        {
            return method switch
            {
                Image.FillMethod.Horizontal => HorizontalOrigins,
                Image.FillMethod.Vertical => VerticalOrigins,
                Image.FillMethod.Radial90 => Radial90Origins,
                Image.FillMethod.Radial180 => Radial180Origins,
                _ => Radial360Origins
            };
        }

        private static void DrawControlStyle(LoogaControlStyle style)
        {
            SerializedObject serializedStyle = new(style);
            serializedStyle.Update();

            SerializedProperty type = serializedStyle.FindProperty("_controlType");
            SerializedProperty transition = serializedStyle.FindProperty("_transition");
            EditorGUILayout.PropertyField(type);
            EditorGUILayout.PropertyField(serializedStyle.FindProperty("_background"));
            EditorGUILayout.PropertyField(transition);

            Selectable.Transition transitionValue = (Selectable.Transition)transition.enumValueIndex;
            switch (transitionValue)
            {
                case Selectable.Transition.ColorTint:
                    DrawProperties(
                        serializedStyle,
                        "_normal",
                        "_highlighted",
                        "_pressed",
                        "_selected",
                        "_disabled",
                        "_colorMultiplier",
                        "_fadeDuration");
                    break;
                case Selectable.Transition.SpriteSwap:
                    DrawProperties(
                        serializedStyle,
                        "_highlightedSprite",
                        "_pressedSprite",
                        "_selectedSprite",
                        "_disabledSprite");
                    break;
                case Selectable.Transition.Animation:
                    EditorGUILayout.PropertyField(
                        serializedStyle.FindProperty("_animationTriggers"),
                        true);
                    break;
            }

            EditorGUILayout.Space();
            switch ((LoogaControlType)type.enumValueIndex)
            {
                case LoogaControlType.Button:
                    DrawProperties(serializedStyle, "_buttonLabel", "_buttonIcon");
                    break;
                case LoogaControlType.Dropdown:
                    DrawProperties(
                        serializedStyle,
                        "_dropdownCaption",
                        "_dropdownCaptionImage",
                        "_dropdownArrow",
                        "_dropdownItemText",
                        "_dropdownItemImage");
                    break;
                case LoogaControlType.InputField:
                    DrawProperties(serializedStyle, "_inputText", "_inputPlaceholder");
                    break;
                case LoogaControlType.Scrollbar:
                    DrawProperties(serializedStyle, "_scrollbarHandle");
                    break;
                case LoogaControlType.Slider:
                    DrawProperties(serializedStyle, "_sliderFill", "_sliderHandle");
                    break;
                case LoogaControlType.Toggle:
                    DrawProperties(serializedStyle, "_toggleCheckmark", "_toggleLabel");
                    break;
            }

            ApplyChanges(serializedStyle, style);
        }

        private static void DrawVisibleProperties(SerializedObject serializedAsset)
        {
            SerializedProperty iterator = serializedAsset.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath != "m_Script")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
        }

        private static void DrawProperties(SerializedObject serializedAsset, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                EditorGUILayout.PropertyField(serializedAsset.FindProperty(names[i]), true);
            }
        }

        private static void ApplyChanges(SerializedObject serializedAsset, Object asset)
        {
            if (serializedAsset.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(asset);
            }
        }

        private static void SaveName(LoogaDesignAsset asset, string requestedName)
        {
            string trimmedName = requestedName?.Trim();
            if (string.IsNullOrEmpty(trimmedName) || trimmedName == asset.name)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            string uniqueName = MakeUniqueName(path, asset, trimmedName);
            Undo.RecordObject(asset, "Rename Design Sub-asset");
            asset.name = uniqueName;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            EditorApplication.RepaintProjectWindow();

            if (!string.IsNullOrEmpty(path) &&
                AssetDatabase.LoadMainAssetAtPath(path) is LoogaDesignSystem designSystem)
            {
                LoogaDesignSystemConsolidator.Synchronize(designSystem);
            }
        }

        private static string MakeUniqueName(string path, Object renamedAsset, string requestedName)
        {
            if (string.IsNullOrEmpty(path))
            {
                return requestedName;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<string> names = new();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != renamedAsset)
                {
                    names.Add(assets[i].name);
                }
            }

            return ObjectNames.GetUniqueName(names.ToArray(), requestedName);
        }
    }

    internal abstract class LoogaNamedDesignAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LoogaDesignAssetEditorGUI.DrawAsset((LoogaDesignAsset)target);
        }
    }

    [CustomEditor(typeof(LoogaColorToken))]
    internal sealed class LoogaColorTokenEditor : LoogaNamedDesignAssetEditor
    {
    }

    [CustomEditor(typeof(LoogaImageStyle))]
    internal sealed class LoogaImageStyleEditor : LoogaNamedDesignAssetEditor
    {
    }

    [CustomEditor(typeof(LoogaTextStyle))]
    internal sealed class LoogaTextStyleEditor : LoogaNamedDesignAssetEditor
    {
    }

    [CustomEditor(typeof(LoogaControlStyle))]
    internal sealed class LoogaControlStyleEditor : LoogaNamedDesignAssetEditor
    {
    }
}
