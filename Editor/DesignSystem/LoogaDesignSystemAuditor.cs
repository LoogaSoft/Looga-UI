using System;
using System.Collections.Generic;
using System.Text;
using LoogaSoft.UI.Design;
using TMPro;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Reports design-system drift without changing authored UI.</summary>
    internal static class LoogaDesignSystemAuditor
    {
        private const float SmallTextThreshold = 14f;

        private static readonly HashSet<string> DecorativeNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Bottom Line",
            "Direction",
            "Gradient",
            "Highlight",
            "Line",
            "Outline"
        };

        internal static Report Analyze(LoogaDesignSystem designSystem)
        {
            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            LoogaStyleBinding[] bindings = UnityEngine.Object.FindObjectsByType<LoogaStyleBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            HashSet<TMP_FontAsset> fonts = new HashSet<TMP_FontAsset>();
            HashSet<int> fontSizes = new HashSet<int>();
            HashSet<LoogaElementStyle> boundStyles = new HashSet<LoogaElementStyle>();
            int sceneTextObjects = 0;
            int defaultFontTexts = 0;
            int smallTexts = 0;
            int fractionalFontSizes = 0;
            int decorativeObjects = 0;
            int cloneObjects = 0;

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (!IsSceneObject(text))
                {
                    continue;
                }

                sceneTextObjects++;

                if (text.font != null)
                {
                    fonts.Add(text.font);
                    if (text.font.name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        defaultFontTexts++;
                    }
                }

                float size = text.enableAutoSizing ? text.fontSizeMin : text.fontSize;
                fontSizes.Add(Mathf.RoundToInt(size * 10f));
                if (size > 0f && size < SmallTextThreshold)
                {
                    smallTexts++;
                }

                if (Mathf.Abs(size - Mathf.Round(size)) > 0.01f)
                {
                    fractionalFontSizes++;
                }
            }

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (!IsSceneObject(transform))
                {
                    continue;
                }

                if (DecorativeNames.Contains(transform.name))
                {
                    decorativeObjects++;
                }

                if (transform.name.EndsWith("(Clone)", StringComparison.Ordinal))
                {
                    cloneObjects++;
                }
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                LoogaStyleBinding binding = bindings[i];
                if (!IsSceneObject(binding) || binding.Style == null)
                {
                    continue;
                }

                LoogaThemeScope scope = LoogaThemeScope.FindNearest(binding.transform);
                if (scope != null && scope.DesignSystem == designSystem)
                {
                    boundStyles.Add(binding.Style);
                }
            }

            int generatedPublicStyleNames = 0;
            IReadOnlyList<LoogaElementStyle> publicStyles = designSystem != null
                ? designSystem.Styles
                : Array.Empty<LoogaElementStyle>();
            for (int i = 0; i < publicStyles.Count; i++)
            {
                if (publicStyles[i] != null && HasGeneratedName(publicStyles[i].name))
                {
                    generatedPublicStyleNames++;
                }
            }

            return new Report(
                sceneTextObjects,
                fonts.Count,
                fontSizes.Count,
                defaultFontTexts,
                smallTexts,
                fractionalFontSizes,
                boundStyles.Count,
                publicStyles.Count,
                generatedPublicStyleNames,
                decorativeObjects,
                cloneObjects);
        }

        private static bool IsSceneObject(Component component)
        {
            return component != null && component.gameObject.scene.IsValid();
        }

        private static bool HasGeneratedName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            int separator = name.LastIndexOf(' ');
            return separator >= 0 && int.TryParse(name.Substring(separator + 1), out _);
        }

        internal readonly struct Report
        {
            internal Report(
                int textObjects,
                int fontAssets,
                int fontSizes,
                int defaultFontTexts,
                int smallTexts,
                int fractionalFontSizes,
                int boundStyles,
                int publicStyles,
                int generatedPublicStyleNames,
                int decorativeObjects,
                int cloneObjects)
            {
                TextObjects = textObjects;
                FontAssets = fontAssets;
                FontSizes = fontSizes;
                DefaultFontTexts = defaultFontTexts;
                SmallTexts = smallTexts;
                FractionalFontSizes = fractionalFontSizes;
                BoundStyles = boundStyles;
                PublicStyles = publicStyles;
                GeneratedPublicStyleNames = generatedPublicStyleNames;
                DecorativeObjects = decorativeObjects;
                CloneObjects = cloneObjects;
            }

            internal int TextObjects { get; }
            internal int FontAssets { get; }
            internal int FontSizes { get; }
            internal int DefaultFontTexts { get; }
            internal int SmallTexts { get; }
            internal int FractionalFontSizes { get; }
            internal int BoundStyles { get; }
            internal int PublicStyles { get; }
            internal int GeneratedPublicStyleNames { get; }
            internal int DecorativeObjects { get; }
            internal int CloneObjects { get; }

            internal string ToSummary()
            {
                return
                    $"Text: {TextObjects}  Fonts: {FontAssets}  Sizes: {FontSizes}  " +
                    $"Default font: {DefaultFontTexts}  Small text: {SmallTexts}\n" +
                    $"Bound styles: {BoundStyles}  Public styles: {PublicStyles}  " +
                    $"Generated public names: {GeneratedPublicStyleNames}\n" +
                    $"Decorative objects: {DecorativeObjects}  Serialized clones: {CloneObjects}";
            }

            internal string ToMarkdown(string title)
            {
                StringBuilder builder = new StringBuilder(2048);
                builder.Append("# ").AppendLine(title);
                builder.AppendLine();
                builder.AppendLine("This report measures loaded scenes. It does not change authored UI.");
                builder.AppendLine();
                builder.AppendLine("## Snapshot");
                builder.AppendLine();
                AppendMetric(builder, "Text objects", TextObjects);
                AppendMetric(builder, "Font assets", FontAssets);
                AppendMetric(builder, "Font sizes", FontSizes);
                AppendMetric(builder, "Text using the default TMP font", DefaultFontTexts);
                AppendMetric(builder, $"Text below {SmallTextThreshold:0} px", SmallTexts);
                AppendMetric(builder, "Text using fractional sizes", FractionalFontSizes);
                AppendMetric(builder, "Bound element styles", BoundStyles);
                AppendMetric(builder, "Public element styles", PublicStyles);
                AppendMetric(builder, "Public styles with generated names", GeneratedPublicStyleNames);
                AppendMetric(builder, "Repeated decorative objects", DecorativeObjects);
                AppendMetric(builder, "Serialized objects named (Clone)", CloneObjects);
                builder.AppendLine();
                builder.AppendLine("## Recommended Actions");
                builder.AppendLine();

                if (DefaultFontTexts > 0 || FontAssets > 3)
                {
                    builder.AppendLine("- Define a small font family and migrate default TMP text into semantic typography roles.");
                }

                if (FontSizes > 10 || FractionalFontSizes > 0)
                {
                    builder.AppendLine("- Replace incidental font sizes with a compact type scale.");
                }

                if (PublicStyles > 64 || GeneratedPublicStyleNames > 0)
                {
                    builder.AppendLine("- Promote a concise semantic style library. Keep captured one-off styles internal.");
                }

                if (DecorativeObjects > 200)
                {
                    builder.AppendLine("- Consolidate repeated lines, gradients, outlines, and highlights into reusable visuals.");
                }

                if (CloneObjects > 0)
                {
                    builder.AppendLine("- Check serialized clone objects. Keep templates authored and create repeated content at runtime.");
                }

                if (SmallTexts > 0)
                {
                    builder.AppendLine("- Reserve sub-14 px text for nonessential metadata. Verify it at the target resolution.");
                }

                builder.AppendLine("- Verify contrast, focus, disabled, selected, and error states without relying on color alone.");
                return builder.ToString();
            }

            private static void AppendMetric(StringBuilder builder, string label, int value)
            {
                builder.Append("- **").Append(label).Append(":** ").AppendLine(value.ToString());
            }
        }
    }
}
