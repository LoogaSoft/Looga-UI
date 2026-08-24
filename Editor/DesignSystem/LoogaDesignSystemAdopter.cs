using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LoogaSoft.UI.Controls;
using LoogaSoft.UI.Design;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Builds a reusable design system from the visuals in loaded scenes.</summary>
    internal static class LoogaDesignSystemAdopter
    {
        internal const string DefaultOutputFolder = "Assets/Project/Databases/UI/Design System";

        private static readonly Dictionary<string, LoogaColorToken> ColorTokens =
            new Dictionary<string, LoogaColorToken>();
        private static readonly Dictionary<string, LoogaTypographyToken> TypographyTokens =
            new Dictionary<string, LoogaTypographyToken>();
        private static readonly Dictionary<string, LoogaElementStyle> ElementStyles =
            new Dictionary<string, LoogaElementStyle>();
        private static readonly Dictionary<string, LoogaButtonStyle> ButtonStyles =
            new Dictionary<string, LoogaButtonStyle>();
        private static readonly Dictionary<string, LoogaToggleStyle> ToggleStyles =
            new Dictionary<string, LoogaToggleStyle>();
        private static readonly Dictionary<string, LoogaSliderStyle> SliderStyles =
            new Dictionary<string, LoogaSliderStyle>();
        private static readonly Dictionary<string, LoogaScrollbarStyle> ScrollbarStyles =
            new Dictionary<string, LoogaScrollbarStyle>();
        private static readonly Dictionary<string, LoogaInputFieldStyle> InputFieldStyles =
            new Dictionary<string, LoogaInputFieldStyle>();
        private static readonly Dictionary<string, LoogaDropdownStyle> DropdownStyles =
            new Dictionary<string, LoogaDropdownStyle>();
        private static readonly HashSet<Component> OwnedVisuals = new HashSet<Component>();

        private static LoogaDesignSystem _designSystem;
        private static LoogaTheme _theme;
        private static string _assetPath;
        private static int _adoptedControls;
        private static int _adoptedElements;

        internal static AdoptionResult AdoptLoadedScenes(string outputFolder)
        {
            ResetState();
            EnsureFolder(outputFolder);

            _designSystem = ScriptableObject.CreateInstance<LoogaDesignSystem>();
            _designSystem.name = "Kubera UI Design System";
            _assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{outputFolder}/{_designSystem.name}.asset");
            AssetDatabase.CreateAsset(_designSystem, _assetPath);

            _theme = AddAsset<LoogaTheme>("Kubera Default Theme");
            SetObject(_designSystem, "_defaultTheme", _theme);

            AdoptButtons();
            AdoptToggles();
            AdoptSliders();
            AdoptScrollbars();
            AdoptInputFields();
            AdoptDropdowns();
            AdoptRemainingElements();
            AddThemeScopes();
            RegisterStyles();

            EditorUtility.SetDirty(_designSystem);
            EditorUtility.SetDirty(_theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
            MarkLoadedScenesDirty();
            Selection.activeObject = _designSystem;
            EditorGUIUtility.PingObject(_designSystem);

            return new AdoptionResult(
                _assetPath,
                _adoptedControls,
                _adoptedElements,
                ColorTokens.Count,
                TypographyTokens.Count,
                ButtonStyles.Count + ToggleStyles.Count + SliderStyles.Count +
                ScrollbarStyles.Count + InputFieldStyles.Count + DropdownStyles.Count);
        }

        private static void AdoptButtons()
        {
            LoogaButton[] controls = FindSceneObjects<LoogaButton>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaButton control = controls[i];
                control.ResolveTargets();
                TMP_Text label = control.GetComponentInChildren<TMP_Text>(true);
                Graphic icon = FindAdditionalGraphic(control.transform, control.targetGraphic, label);
                Own(control.targetGraphic, label, icon);

                string fingerprint = BuildControlFingerprint(control.targetGraphic, label, icon, control);
                if (!ButtonStyles.TryGetValue(fingerprint, out LoogaButtonStyle style))
                {
                    style = AddAsset<LoogaButtonStyle>(NextName("Button Style", ButtonStyles.Count));
                    ConfigureStyle(style,
                        ("_button", GetElementStyle("Button", control.targetGraphic, null, control)),
                        ("_label", GetElementStyle("Button Label", label, label, null)),
                        ("_icon", GetElementStyle("Button Icon", icon, null, null)));
                    ButtonStyles.Add(fingerprint, style);
                }

                Assign(control, style);
            }
        }

        private static void AdoptToggles()
        {
            LoogaToggle[] controls = FindSceneObjects<LoogaToggle>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaToggle control = controls[i];
                control.ResolveTargets();
                TMP_Text label = control.GetComponentInChildren<TMP_Text>(true);
                Own(control.targetGraphic, control.graphic, label);

                string fingerprint = BuildControlFingerprint(control.targetGraphic, control.graphic, label, control);
                if (!ToggleStyles.TryGetValue(fingerprint, out LoogaToggleStyle style))
                {
                    style = AddAsset<LoogaToggleStyle>(NextName("Toggle Style", ToggleStyles.Count));
                    ConfigureStyle(style,
                        ("_toggle", GetElementStyle("Toggle", control.targetGraphic, null, control)),
                        ("_checkmark", GetElementStyle("Toggle Checkmark", control.graphic, null, null)),
                        ("_label", GetElementStyle("Toggle Label", label, label, null)));
                    ToggleStyles.Add(fingerprint, style);
                }

                Assign(control, style);
            }
        }

        private static void AdoptSliders()
        {
            LoogaSlider[] controls = FindSceneObjects<LoogaSlider>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaSlider control = controls[i];
                control.ResolveTargets();
                Graphic fill = GetGraphic(control.fillRect);
                Graphic handle = GetGraphic(control.handleRect);
                Own(control.targetGraphic, fill, handle);

                string fingerprint = BuildControlFingerprint(control.targetGraphic, fill, handle, control);
                if (!SliderStyles.TryGetValue(fingerprint, out LoogaSliderStyle style))
                {
                    style = AddAsset<LoogaSliderStyle>(NextName("Slider Style", SliderStyles.Count));
                    ConfigureStyle(style,
                        ("_slider", GetElementStyle("Slider", control.targetGraphic, null, control)),
                        ("_fill", GetElementStyle("Slider Fill", fill, null, null)),
                        ("_handle", GetElementStyle("Slider Handle", handle, null, null)));
                    SliderStyles.Add(fingerprint, style);
                }

                Assign(control, style);
            }
        }

        private static void AdoptScrollbars()
        {
            LoogaScrollbar[] controls = FindSceneObjects<LoogaScrollbar>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaScrollbar control = controls[i];
                control.ResolveTargets();
                Graphic handle = GetGraphic(control.handleRect);
                Own(control.targetGraphic, handle);

                string fingerprint = BuildControlFingerprint(control.targetGraphic, handle, control);
                if (!ScrollbarStyles.TryGetValue(fingerprint, out LoogaScrollbarStyle style))
                {
                    style = AddAsset<LoogaScrollbarStyle>(NextName("Scrollbar Style", ScrollbarStyles.Count));
                    ConfigureStyle(style,
                        ("_scrollbar", GetElementStyle("Scrollbar", control.targetGraphic, null, control)),
                        ("_handle", GetElementStyle("Scrollbar Handle", handle, null, null)));
                    ScrollbarStyles.Add(fingerprint, style);
                }

                Assign(control, style);
            }
        }

        private static void AdoptInputFields()
        {
            LoogaInputField[] controls = FindSceneObjects<LoogaInputField>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaInputField control = controls[i];
                control.ResolveTargets();
                TMP_Text text = control.textComponent;
                TMP_Text placeholder = control.placeholder as TMP_Text;
                Own(control.targetGraphic, text, placeholder);

                string fingerprint = BuildControlFingerprint(control.targetGraphic, text, placeholder, control);
                if (!InputFieldStyles.TryGetValue(fingerprint, out LoogaInputFieldStyle style))
                {
                    style = AddAsset<LoogaInputFieldStyle>(NextName("Input Field Style", InputFieldStyles.Count));
                    ConfigureStyle(style,
                        ("_inputField", GetElementStyle("Input Field", control.targetGraphic, null, control)),
                        ("_text", GetElementStyle("Input Text", text, text, null)),
                        ("_placeholder", GetElementStyle("Input Placeholder", placeholder, placeholder, null)));
                    InputFieldStyles.Add(fingerprint, style);
                }

                Assign(control, style);
            }
        }

        private static void AdoptDropdowns()
        {
            LoogaDropdown[] controls = FindSceneObjects<LoogaDropdown>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaDropdown control = controls[i];
                control.ResolveTargets();
                Graphic arrow = FindDropdownArrow(control);
                Own(control.targetGraphic, control.captionText, control.captionImage, arrow,
                    control.itemText, control.itemImage);

                string fingerprint = BuildControlFingerprint(control.targetGraphic, control.captionText,
                    control.captionImage, arrow, control.itemText, control.itemImage, control);
                if (!DropdownStyles.TryGetValue(fingerprint, out LoogaDropdownStyle style))
                {
                    style = AddAsset<LoogaDropdownStyle>(NextName("Dropdown Style", DropdownStyles.Count));
                    ConfigureStyle(style,
                        ("_dropdown", GetElementStyle("Dropdown", control.targetGraphic, null, control)),
                        ("_caption", GetElementStyle("Dropdown Caption", control.captionText, control.captionText, null)),
                        ("_captionImage", GetElementStyle("Dropdown Caption Image", control.captionImage, null, null)),
                        ("_arrow", GetElementStyle("Dropdown Arrow", arrow, null, null)),
                        ("_item", GetElementStyle("Dropdown Item", control.itemText, control.itemText, null)),
                        ("_itemImage", GetElementStyle("Dropdown Item Image", control.itemImage, null, null)));
                    DropdownStyles.Add(fingerprint, style);
                }

                Assign(control, style);
            }
        }

        private static void AdoptRemainingElements()
        {
            HashSet<GameObject> candidates = new HashSet<GameObject>();
            AddCandidates(FindSceneObjects<Graphic>(), candidates);
            AddCandidates(FindSceneObjects<TMP_Text>(), candidates);
            AddCandidates(FindSceneObjects<Selectable>(), candidates);

            foreach (GameObject candidate in candidates)
            {
                Graphic graphic = candidate.GetComponent<Graphic>();
                TMP_Text text = candidate.GetComponent<TMP_Text>();
                Selectable selectable = candidate.GetComponent<Selectable>();
                if (IsOwned(graphic, text, selectable) || candidate.TryGetComponent(out LoogaStyleBinding _))
                {
                    continue;
                }

                LoogaElementStyle style = GetElementStyle("Element", graphic, text, selectable);
                if (style == null)
                {
                    continue;
                }

                LoogaStyleBinding binding = Undo.AddComponent<LoogaStyleBinding>(candidate);
                binding.SetStyle(style);
                EditorUtility.SetDirty(binding);
                _adoptedElements++;
            }
        }

        private static LoogaElementStyle GetElementStyle(
            string role,
            Graphic graphic,
            TMP_Text text,
            Selectable selectable)
        {
            if (graphic == null && text == null && selectable == null)
            {
                return null;
            }

            string fingerprint = BuildElementFingerprint(graphic, text, selectable);
            if (ElementStyles.TryGetValue(fingerprint, out LoogaElementStyle existing))
            {
                return existing;
            }

            LoogaElementStyle style = AddAsset<LoogaElementStyle>(NextName(role, ElementStyles.Count));
            SerializedObject serializedStyle = new SerializedObject(style);

            if (graphic != null)
            {
                Material material = GetExplicitMaterial(graphic);
                SetBool(serializedStyle, "_graphic._enabled", true);
                SetObject(serializedStyle, "_graphic._color", GetColorToken(graphic.color));
                SetBool(serializedStyle, "_graphic._overrideMaterial", material != null);
                SetObject(serializedStyle, "_graphic._material", material);
            }

            if (text != null)
            {
                SetBool(serializedStyle, "_text._enabled", true);
                SetObject(serializedStyle, "_text._typography", GetTypographyToken(text));
                SetObject(serializedStyle, "_text._color", GetColorToken(text.color));
                SetBool(serializedStyle, "_text._overrideAlignment", true);
                SetInt(serializedStyle, "_text._alignment", (int)text.alignment);
            }

            if (selectable != null)
            {
                ColorBlock colors = selectable.colors;
                SetBool(serializedStyle, "_selectable._enabled", true);
                SetInt(serializedStyle, "_selectable._transition", (int)selectable.transition);
                SetObject(serializedStyle, "_selectable._normal", GetColorToken(colors.normalColor));
                SetObject(serializedStyle, "_selectable._highlighted", GetColorToken(colors.highlightedColor));
                SetObject(serializedStyle, "_selectable._pressed", GetColorToken(colors.pressedColor));
                SetObject(serializedStyle, "_selectable._selected", GetColorToken(colors.selectedColor));
                SetObject(serializedStyle, "_selectable._disabled", GetColorToken(colors.disabledColor));
                SetFloat(serializedStyle, "_selectable._colorMultiplier", colors.colorMultiplier);
                SetFloat(serializedStyle, "_selectable._fadeDuration", colors.fadeDuration);
            }

            serializedStyle.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(style);
            ElementStyles.Add(fingerprint, style);
            return style;
        }

        private static LoogaColorToken GetColorToken(Color color)
        {
            string fingerprint = ColorFingerprint(color);
            if (ColorTokens.TryGetValue(fingerprint, out LoogaColorToken existing))
            {
                return existing;
            }

            LoogaColorToken token = AddAsset<LoogaColorToken>(NextName("Color", ColorTokens.Count));
            token.SetDefaultColor(color);
            _theme.SetColor(token, color);
            ColorTokens.Add(fingerprint, token);
            return token;
        }

        private static LoogaTypographyToken GetTypographyToken(TMP_Text text)
        {
            string fingerprint = BuildTypographyFingerprint(text);
            if (TypographyTokens.TryGetValue(fingerprint, out LoogaTypographyToken existing))
            {
                return existing;
            }

            LoogaTypographyToken token = AddAsset<LoogaTypographyToken>(
                NextName("Typography", TypographyTokens.Count));
            SerializedObject serializedToken = new SerializedObject(token);
            SetObject(serializedToken, "_defaults._font", text.font);
            SetFloat(serializedToken, "_defaults._fontSize", text.fontSize);
            SetInt(serializedToken, "_defaults._fontStyle", (int)text.fontStyle);
            SetFloat(serializedToken, "_defaults._characterSpacing", text.characterSpacing);
            SetFloat(serializedToken, "_defaults._wordSpacing", text.wordSpacing);
            SetFloat(serializedToken, "_defaults._lineSpacing", text.lineSpacing);
            SetFloat(serializedToken, "_defaults._paragraphSpacing", text.paragraphSpacing);
            serializedToken.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(token);
            TypographyTokens.Add(fingerprint, token);
            return token;
        }

        private static void AddThemeScopes()
        {
            Canvas[] canvases = FindSceneObjects<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (HasParentCanvas(canvas.transform))
                {
                    continue;
                }

                LoogaThemeScope scope = canvas.GetComponent<LoogaThemeScope>();
                if (scope == null)
                {
                    scope = Undo.AddComponent<LoogaThemeScope>(canvas.gameObject);
                }

                SerializedObject serializedScope = new SerializedObject(scope);
                SetObject(serializedScope, "_designSystem", _designSystem);
                serializedScope.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(scope);
            }
        }

        private static void RegisterStyles()
        {
            SerializedObject serializedSystem = new SerializedObject(_designSystem);
            SetArray(serializedSystem, "_styles", ElementStyles.Values);
            SetArray(serializedSystem, "_buttonStyles", ButtonStyles.Values);
            SetArray(serializedSystem, "_toggleStyles", ToggleStyles.Values);
            SetArray(serializedSystem, "_sliderStyles", SliderStyles.Values);
            SetArray(serializedSystem, "_scrollbarStyles", ScrollbarStyles.Values);
            SetArray(serializedSystem, "_inputFieldStyles", InputFieldStyles.Values);
            SetArray(serializedSystem, "_dropdownStyles", DropdownStyles.Values);
            serializedSystem.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(LoogaButton control, LoogaButtonStyle style)
        {
            Undo.RecordObject(control, "Adopt Button Style");
            control.SetStyle(style);
            control.SetStyleSource(LoogaControlStyleSource.DesignSystem);
            EditorUtility.SetDirty(control);
            _adoptedControls++;
        }

        private static void Assign(LoogaToggle control, LoogaToggleStyle style)
        {
            Undo.RecordObject(control, "Adopt Toggle Style");
            control.SetStyle(style);
            control.SetStyleSource(LoogaControlStyleSource.DesignSystem);
            EditorUtility.SetDirty(control);
            _adoptedControls++;
        }

        private static void Assign(LoogaSlider control, LoogaSliderStyle style)
        {
            Undo.RecordObject(control, "Adopt Slider Style");
            control.SetStyle(style);
            control.SetStyleSource(LoogaControlStyleSource.DesignSystem);
            EditorUtility.SetDirty(control);
            _adoptedControls++;
        }

        private static void Assign(LoogaScrollbar control, LoogaScrollbarStyle style)
        {
            Undo.RecordObject(control, "Adopt Scrollbar Style");
            control.SetStyle(style);
            control.SetStyleSource(LoogaControlStyleSource.DesignSystem);
            EditorUtility.SetDirty(control);
            _adoptedControls++;
        }

        private static void Assign(LoogaInputField control, LoogaInputFieldStyle style)
        {
            Undo.RecordObject(control, "Adopt Input Field Style");
            control.SetStyle(style);
            control.SetStyleSource(LoogaControlStyleSource.DesignSystem);
            EditorUtility.SetDirty(control);
            _adoptedControls++;
        }

        private static void Assign(LoogaDropdown control, LoogaDropdownStyle style)
        {
            Undo.RecordObject(control, "Adopt Dropdown Style");
            control.SetStyle(style);
            control.SetStyleSource(LoogaControlStyleSource.DesignSystem);
            EditorUtility.SetDirty(control);
            _adoptedControls++;
        }

        private static void ConfigureStyle(
            Object style,
            params (string propertyName, LoogaElementStyle elementStyle)[] values)
        {
            SerializedObject serializedStyle = new SerializedObject(style);
            for (int i = 0; i < values.Length; i++)
            {
                SetObject(serializedStyle, values[i].propertyName, values[i].elementStyle);
            }

            serializedStyle.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(style);
        }

        private static T AddAsset<T>(string name) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.AddObjectToAsset(asset, _designSystem);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T[] FindSceneObjects<T>() where T : Component
        {
            T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<T> sceneObjects = new List<T>(found.Length);
            for (int i = 0; i < found.Length; i++)
            {
                T component = found[i];
                if (component != null && component.gameObject.scene.IsValid())
                {
                    sceneObjects.Add(component);
                }
            }

            return sceneObjects.ToArray();
        }

        private static void AddCandidates<T>(T[] components, HashSet<GameObject> candidates)
            where T : Component
        {
            for (int i = 0; i < components.Length; i++)
            {
                candidates.Add(components[i].gameObject);
            }
        }

        private static void Own(params Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    OwnedVisuals.Add(components[i]);
                }
            }
        }

        private static bool IsOwned(params Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && OwnedVisuals.Contains(components[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static Graphic GetGraphic(RectTransform transform)
        {
            return transform != null ? transform.GetComponent<Graphic>() : null;
        }

        private static Material GetExplicitMaterial(Graphic graphic)
        {
            SerializedObject serializedGraphic = new SerializedObject(graphic);
            SerializedProperty material = serializedGraphic.FindProperty("m_Material");
            return material != null ? material.objectReferenceValue as Material : null;
        }

        private static Graphic FindAdditionalGraphic(
            Transform root,
            Graphic background,
            TMP_Text label)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic != background && graphic != label)
                {
                    return graphic;
                }
            }

            return null;
        }

        private static Graphic FindDropdownArrow(LoogaDropdown dropdown)
        {
            Graphic[] graphics = dropdown.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic candidate = graphics[i];
                bool belongsToTemplate = dropdown.template != null &&
                                         candidate.transform.IsChildOf(dropdown.template);
                if (candidate == dropdown.targetGraphic || candidate == dropdown.captionText ||
                    candidate == dropdown.captionImage || candidate == dropdown.itemText ||
                    candidate == dropdown.itemImage || belongsToTemplate)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static bool HasParentCanvas(Transform transform)
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent(out Canvas _))
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }

        private static string BuildControlFingerprint(params Component[] components)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component is TMP_Text text)
                {
                    builder.Append(BuildElementFingerprint(text, text, null));
                }
                else if (component is Selectable selectable)
                {
                    builder.Append(BuildElementFingerprint(selectable.targetGraphic, null, selectable));
                }
                else if (component is Graphic graphic)
                {
                    builder.Append(BuildElementFingerprint(graphic, null, null));
                }
                else
                {
                    builder.Append("null");
                }

                builder.Append('|');
            }

            return builder.ToString();
        }

        private static string BuildElementFingerprint(
            Graphic graphic,
            TMP_Text text,
            Selectable selectable)
        {
            StringBuilder builder = new StringBuilder();
            if (graphic != null)
            {
                Material material = GetExplicitMaterial(graphic);
                builder.Append("G:")
                    .Append(ColorFingerprint(graphic.color)).Append(':')
                    .Append(material != null ? material.GetInstanceID() : 0);
            }

            if (text != null)
            {
                builder.Append("T:")
                    .Append(BuildTypographyFingerprint(text)).Append(':')
                    .Append(ColorFingerprint(text.color)).Append(':')
                    .Append((int)text.alignment);
            }

            if (selectable != null)
            {
                ColorBlock colors = selectable.colors;
                builder.Append("S:")
                    .Append((int)selectable.transition).Append(':')
                    .Append(ColorFingerprint(colors.normalColor)).Append(':')
                    .Append(ColorFingerprint(colors.highlightedColor)).Append(':')
                    .Append(ColorFingerprint(colors.pressedColor)).Append(':')
                    .Append(ColorFingerprint(colors.selectedColor)).Append(':')
                    .Append(ColorFingerprint(colors.disabledColor)).Append(':')
                    .Append(Float(colors.colorMultiplier)).Append(':')
                    .Append(Float(colors.fadeDuration));
            }

            return builder.ToString();
        }

        private static string BuildTypographyFingerprint(TMP_Text text)
        {
            return new StringBuilder()
                .Append(text.font != null ? text.font.GetInstanceID() : 0).Append(':')
                .Append(Float(text.fontSize)).Append(':')
                .Append((int)text.fontStyle).Append(':')
                .Append(Float(text.characterSpacing)).Append(':')
                .Append(Float(text.wordSpacing)).Append(':')
                .Append(Float(text.lineSpacing)).Append(':')
                .Append(Float(text.paragraphSpacing))
                .ToString();
        }

        private static string ColorFingerprint(Color color)
        {
            return string.Join(",", Float(color.r), Float(color.g), Float(color.b), Float(color.a));
        }

        private static string Float(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string NextName(string role, int index)
        {
            return $"{role} {index + 1:00}";
        }

        private static void SetArray<T>(
            SerializedObject owner,
            string propertyName,
            ICollection<T> values)
            where T : Object
        {
            SerializedProperty property = RequireProperty(owner, propertyName);
            property.arraySize = values.Count;
            int index = 0;
            foreach (T value in values)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = value;
                index++;
            }
        }

        private static void SetObject(Object owner, string propertyName, Object value)
        {
            SerializedObject serializedOwner = new SerializedObject(owner);
            SetObject(serializedOwner, propertyName, value);
            serializedOwner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(SerializedObject owner, string propertyName, Object value)
        {
            RequireProperty(owner, propertyName).objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject owner, string propertyName, bool value)
        {
            RequireProperty(owner, propertyName).boolValue = value;
        }

        private static void SetInt(SerializedObject owner, string propertyName, int value)
        {
            RequireProperty(owner, propertyName).intValue = value;
        }

        private static void SetFloat(SerializedObject owner, string propertyName, float value)
        {
            RequireProperty(owner, propertyName).floatValue = value;
        }

        private static SerializedProperty RequireProperty(SerializedObject owner, string propertyName)
        {
            SerializedProperty property = owner.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{owner.targetObject.GetType().Name} does not contain {propertyName}.");
            }

            return property;
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = folder.Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new ArgumentException("The output folder must be inside Assets.", nameof(folder));
            }

            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string[] segments = normalized.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static void MarkLoadedScenesDirty()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private static void ResetState()
        {
            ColorTokens.Clear();
            TypographyTokens.Clear();
            ElementStyles.Clear();
            ButtonStyles.Clear();
            ToggleStyles.Clear();
            SliderStyles.Clear();
            ScrollbarStyles.Clear();
            InputFieldStyles.Clear();
            DropdownStyles.Clear();
            OwnedVisuals.Clear();
            _designSystem = null;
            _theme = null;
            _assetPath = null;
            _adoptedControls = 0;
            _adoptedElements = 0;
        }

        internal readonly struct AdoptionResult
        {
            internal AdoptionResult(
                string assetPath,
                int controls,
                int elements,
                int colors,
                int typography,
                int controlStyles)
            {
                AssetPath = assetPath;
                Controls = controls;
                Elements = elements;
                Colors = colors;
                Typography = typography;
                ControlStyles = controlStyles;
            }

            internal string AssetPath { get; }
            internal int Controls { get; }
            internal int Elements { get; }
            internal int Colors { get; }
            internal int Typography { get; }
            internal int ControlStyles { get; }
        }
    }
}
