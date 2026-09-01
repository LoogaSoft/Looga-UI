using System;
using LoogaSoft.UI.Design;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Copies compatible component settings into newly created design assets.</summary>
    internal static class LoogaDesignAssetComponentCapture
    {
        internal static bool Supports(Type assetType)
        {
            return assetType == typeof(LoogaColorToken) ||
                   assetType == typeof(LoogaImageStyle) ||
                   assetType == typeof(LoogaTextStyle) ||
                   assetType == typeof(LoogaControlStyle);
        }

        internal static Component ResolveSource(UnityEngine.Object owner, Type assetType)
        {
            if (owner is not Component component)
            {
                return null;
            }

            SerializedObject serializedOwner = new(owner);
            serializedOwner.Update();
            string[] targetNames = assetType == typeof(LoogaColorToken)
                ? new[] { "_target", "_graphicTarget" }
                : assetType == typeof(LoogaImageStyle)
                    ? new[] { "_target", "_imageTarget" }
                    : assetType == typeof(LoogaTextStyle)
                        ? new[] { "_target", "_textTarget" }
                        : new[] { "_controlTarget", "_buttonTarget" };

            for (int i = 0; i < targetNames.Length; i++)
            {
                Component assigned = serializedOwner.FindProperty(targetNames[i])
                    ?.objectReferenceValue as Component;
                if (IsCompatible(assetType, assigned))
                {
                    return assigned;
                }
            }

            return assetType == typeof(LoogaColorToken)
                ? component.GetComponent<Graphic>()
                : assetType == typeof(LoogaImageStyle)
                    ? component.GetComponent<Image>()
                    : assetType == typeof(LoogaTextStyle)
                        ? component.GetComponent<TMP_Text>()
                        : ResolveControl(component);
        }

        internal static string GetOptionLabel(Type assetType)
        {
            if (assetType == typeof(LoogaColorToken))
            {
                return "Use Graphic Color";
            }

            if (assetType == typeof(LoogaImageStyle))
            {
                return "Use Image Settings";
            }

            if (assetType == typeof(LoogaTextStyle))
            {
                return "Use Text Settings";
            }

            return "Use Control Settings";
        }

        internal static void Apply(LoogaDesignAsset asset, Component componentSource)
        {
            if (componentSource == null)
            {
                return;
            }

            SerializedObject serializedAsset = new(asset);
            switch (asset)
            {
                case LoogaColorToken when componentSource is Graphic graphic:
                    serializedAsset.FindProperty("_defaultColor").colorValue = graphic.color;
                    break;
                case LoogaImageStyle when componentSource is Image image:
                    CaptureImage(serializedAsset, image);
                    break;
                case LoogaTextStyle when componentSource is TMP_Text text:
                    CaptureText(serializedAsset, text);
                    break;
                case LoogaControlStyle controlStyle when componentSource is Selectable selectable:
                    CaptureControl(controlStyle, serializedAsset, selectable);
                    break;
                default:
                    return;
            }

            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool IsCompatible(Type assetType, Component component)
        {
            if (assetType == typeof(LoogaColorToken))
            {
                return component is Graphic;
            }

            if (assetType == typeof(LoogaImageStyle))
            {
                return component is Image;
            }

            if (assetType == typeof(LoogaTextStyle))
            {
                return component is TMP_Text;
            }

            return assetType == typeof(LoogaControlStyle) &&
                   component is Selectable selectable &&
                   TryGetControlType(selectable, out _);
        }

        private static Selectable ResolveControl(Component component)
        {
            Selectable selectable = component.GetComponent<Selectable>();
            return TryGetControlType(selectable, out _) ? selectable : null;
        }

        private static void CaptureImage(SerializedObject style, Image image)
        {
            style.FindProperty("_sprite").objectReferenceValue =
                image.overrideSprite != null ? image.overrideSprite : image.sprite;
            style.FindProperty("_material").objectReferenceValue = image.material;
            style.FindProperty("_color").colorValue = image.color;
            style.FindProperty("_type").enumValueIndex = (int)image.type;
            style.FindProperty("_preserveAspect").boolValue = image.preserveAspect;
            style.FindProperty("_fillCenter").boolValue = image.fillCenter;
            style.FindProperty("_pixelsPerUnitMultiplier").floatValue = image.pixelsPerUnitMultiplier;
            style.FindProperty("_useSpriteMesh").boolValue = image.useSpriteMesh;
            style.FindProperty("_fillMethod").enumValueIndex = (int)image.fillMethod;
            style.FindProperty("_fillAmount").floatValue = image.fillAmount;
            style.FindProperty("_fillClockwise").boolValue = image.fillClockwise;
            style.FindProperty("_fillOrigin").intValue = image.fillOrigin;
            style.FindProperty("_alphaHitTestMinimumThreshold").floatValue =
                image.alphaHitTestMinimumThreshold;
        }

        private static void CaptureText(SerializedObject style, TMP_Text text)
        {
            SerializedProperty defaults = style.FindProperty("_defaults");
            defaults.FindPropertyRelative("_font").objectReferenceValue = text.font;
            defaults.FindPropertyRelative("_fontMaterial").objectReferenceValue = text.fontSharedMaterial;
            defaults.FindPropertyRelative("_fontSize").floatValue = text.fontSize;
            defaults.FindPropertyRelative("_fontStyle").intValue = (int)text.fontStyle;
            defaults.FindPropertyRelative("_enableAutoSizing").boolValue = text.enableAutoSizing;
            defaults.FindPropertyRelative("_minimumFontSize").floatValue = text.fontSizeMin;
            defaults.FindPropertyRelative("_maximumFontSize").floatValue = text.fontSizeMax;
            defaults.FindPropertyRelative("_characterSpacing").floatValue = text.characterSpacing;
            defaults.FindPropertyRelative("_wordSpacing").floatValue = text.wordSpacing;
            defaults.FindPropertyRelative("_lineSpacing").floatValue = text.lineSpacing;
            defaults.FindPropertyRelative("_paragraphSpacing").floatValue = text.paragraphSpacing;
        }

        private static void CaptureControl(
            LoogaControlStyle controlStyle,
            SerializedObject serializedStyle,
            Selectable selectable)
        {
            if (!TryGetControlType(selectable, out LoogaControlType controlType))
            {
                return;
            }

            serializedStyle.FindProperty("_controlType").enumValueIndex = (int)controlType;
            serializedStyle.FindProperty("_transition").enumValueIndex = (int)selectable.transition;

            ColorBlock colors = selectable.colors;
            serializedStyle.FindProperty("_normal").colorValue = colors.normalColor;
            serializedStyle.FindProperty("_highlighted").colorValue = colors.highlightedColor;
            serializedStyle.FindProperty("_pressed").colorValue = colors.pressedColor;
            serializedStyle.FindProperty("_selected").colorValue = colors.selectedColor;
            serializedStyle.FindProperty("_disabled").colorValue = colors.disabledColor;
            serializedStyle.FindProperty("_colorMultiplier").floatValue = colors.colorMultiplier;
            serializedStyle.FindProperty("_fadeDuration").floatValue = colors.fadeDuration;

            SpriteState sprites = selectable.spriteState;
            serializedStyle.FindProperty("_highlightedSprite").objectReferenceValue =
                sprites.highlightedSprite;
            serializedStyle.FindProperty("_pressedSprite").objectReferenceValue = sprites.pressedSprite;
            serializedStyle.FindProperty("_selectedSprite").objectReferenceValue = sprites.selectedSprite;
            serializedStyle.FindProperty("_disabledSprite").objectReferenceValue = sprites.disabledSprite;

            AnimationTriggers triggers = selectable.animationTriggers;
            SerializedProperty serializedTriggers = serializedStyle.FindProperty("_animationTriggers");
            SetTrigger(serializedTriggers, "m_NormalTrigger", triggers.normalTrigger);
            SetTrigger(serializedTriggers, "m_HighlightedTrigger", triggers.highlightedTrigger);
            SetTrigger(serializedTriggers, "m_PressedTrigger", triggers.pressedTrigger);
            SetTrigger(serializedTriggers, "m_SelectedTrigger", triggers.selectedTrigger);
            SetTrigger(serializedTriggers, "m_DisabledTrigger", triggers.disabledTrigger);

            CaptureControlVisuals(controlStyle, serializedStyle, selectable);
        }

        private static void CaptureControlVisuals(
            LoogaControlStyle controlStyle,
            SerializedObject serializedStyle,
            Selectable selectable)
        {
            SetImageStyle(
                controlStyle,
                serializedStyle.FindProperty("_background"),
                selectable.targetGraphic as Image,
                "Background");

            switch (selectable)
            {
                case Button button:
                    SetTextStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_buttonLabel"),
                        button.GetComponentInChildren<TMP_Text>(true),
                        "Label");
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_buttonIcon"),
                        FindButtonIcon(button),
                        "Icon");
                    break;
                case TMP_Dropdown dropdown:
                    SetTextStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_dropdownCaption"),
                        dropdown.captionText,
                        "Caption");
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_dropdownCaptionImage"),
                        dropdown.captionImage,
                        "Caption Image");
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_dropdownArrow"),
                        FindImage(dropdown.transform, "Arrow"),
                        "Arrow");
                    SetTextStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_dropdownItemText"),
                        dropdown.itemText,
                        "Item Text");
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_dropdownItemImage"),
                        dropdown.itemImage,
                        "Item Image");
                    break;
                case TMP_InputField inputField:
                    SetTextStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_inputText"),
                        inputField.textComponent,
                        "Input Text");
                    SetTextStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_inputPlaceholder"),
                        inputField.placeholder as TMP_Text,
                        "Placeholder");
                    break;
                case Scrollbar scrollbar:
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_scrollbarHandle"),
                        GetImage(scrollbar.handleRect),
                        "Handle");
                    break;
                case Slider slider:
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_sliderFill"),
                        GetImage(slider.fillRect),
                        "Fill");
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_sliderHandle"),
                        GetImage(slider.handleRect),
                        "Handle");
                    break;
                case Toggle toggle:
                    SetImageStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_toggleCheckmark"),
                        toggle.graphic as Image,
                        "Checkmark");
                    SetTextStyle(
                        controlStyle,
                        serializedStyle.FindProperty("_toggleLabel"),
                        toggle.GetComponentInChildren<TMP_Text>(true),
                        "Label");
                    break;
            }
        }

        private static void SetImageStyle(
            LoogaControlStyle owner,
            SerializedProperty reference,
            Image source,
            string role)
        {
            if (source == null)
            {
                return;
            }

            LoogaImageStyle imageStyle = CreateChildStyle<LoogaImageStyle>(owner, source.name, role);
            if (imageStyle == null)
            {
                return;
            }

            SerializedObject serializedImage = new(imageStyle);
            CaptureImage(serializedImage, source);
            serializedImage.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(imageStyle);
            reference.objectReferenceValue = imageStyle;
        }

        private static void SetTextStyle(
            LoogaControlStyle owner,
            SerializedProperty reference,
            TMP_Text source,
            string role)
        {
            if (source == null)
            {
                return;
            }

            LoogaTextStyle textStyle = CreateChildStyle<LoogaTextStyle>(owner, source.name, role);
            if (textStyle == null)
            {
                return;
            }

            SerializedObject serializedText = new(textStyle);
            CaptureText(serializedText, source);
            serializedText.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(textStyle);
            reference.objectReferenceValue = textStyle;
        }

        private static T CreateChildStyle<T>(
            LoogaControlStyle owner,
            string sourceName,
            string role)
            where T : LoogaDesignAsset
        {
            string path = AssetDatabase.GetAssetPath(owner);
            UnityEngine.Object container = AssetDatabase.LoadMainAssetAtPath(path);
            if (container == null)
            {
                return null;
            }

            T style = ScriptableObject.CreateInstance<T>();
            string requestedName = $"{sourceName} {role}";
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            string[] names = new string[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                names[i] = assets[i].name;
            }

            style.name = ObjectNames.GetUniqueName(names, requestedName);
            AssetDatabase.AddObjectToAsset(style, container);
            Undo.RegisterCreatedObjectUndo(style, $"Create {role} Style");
            return style;
        }

        private static Image FindButtonIcon(Button button)
        {
            Image[] images = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != button.targetGraphic)
                {
                    return images[i];
                }
            }

            return null;
        }

        private static Image FindImage(Transform root, string objectName)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (string.Equals(images[i].name, objectName, StringComparison.OrdinalIgnoreCase))
                {
                    return images[i];
                }
            }

            return null;
        }

        private static Image GetImage(RectTransform target)
        {
            return target != null ? target.GetComponent<Image>() : null;
        }

        private static void SetTrigger(
            SerializedProperty triggers,
            string propertyName,
            string value)
        {
            SerializedProperty trigger = triggers?.FindPropertyRelative(propertyName);
            if (trigger != null)
            {
                trigger.stringValue = value;
            }
        }

        private static bool TryGetControlType(
            Selectable selectable,
            out LoogaControlType controlType)
        {
            switch (selectable)
            {
                case Button:
                    controlType = LoogaControlType.Button;
                    return true;
                case TMP_Dropdown:
                    controlType = LoogaControlType.Dropdown;
                    return true;
                case TMP_InputField:
                    controlType = LoogaControlType.InputField;
                    return true;
                case Scrollbar:
                    controlType = LoogaControlType.Scrollbar;
                    return true;
                case Slider:
                    controlType = LoogaControlType.Slider;
                    return true;
                case Toggle:
                    controlType = LoogaControlType.Toggle;
                    return true;
                default:
                    controlType = default;
                    return false;
            }
        }
    }
}
