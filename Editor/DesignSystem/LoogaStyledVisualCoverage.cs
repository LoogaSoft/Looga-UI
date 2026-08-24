using System.Collections.Generic;
using LoogaSoft.UI.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Finds visuals whose appearance is owned by a typed Looga control.</summary>
    internal static class LoogaStyledVisualCoverage
    {
        internal static void Collect(HashSet<GameObject> coveredObjects)
        {
            CollectButtons(coveredObjects);
            CollectToggles(coveredObjects);
            CollectSliders(coveredObjects);
            CollectScrollbars(coveredObjects);
            CollectInputFields(coveredObjects);
            CollectDropdowns(coveredObjects);
        }

        private static void CollectButtons(HashSet<GameObject> coveredObjects)
        {
            LoogaButton[] controls = FindSceneObjects<LoogaButton>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaButton control = controls[i];
                if (!UsesDesignSystem(control.StyleSource, control.Style))
                {
                    continue;
                }

                control.ResolveTargets();
                TMP_Text label = control.GetComponentInChildren<TMP_Text>(true);
                Graphic icon = FindAdditionalGraphic(control.transform, control.targetGraphic, label);
                Add(coveredObjects, control.targetGraphic, label, icon);
            }
        }

        private static void CollectToggles(HashSet<GameObject> coveredObjects)
        {
            LoogaToggle[] controls = FindSceneObjects<LoogaToggle>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaToggle control = controls[i];
                if (!UsesDesignSystem(control.StyleSource, control.Style))
                {
                    continue;
                }

                control.ResolveTargets();
                Add(coveredObjects, control.targetGraphic, control.graphic,
                    control.GetComponentInChildren<TMP_Text>(true));
            }
        }

        private static void CollectSliders(HashSet<GameObject> coveredObjects)
        {
            LoogaSlider[] controls = FindSceneObjects<LoogaSlider>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaSlider control = controls[i];
                if (!UsesDesignSystem(control.StyleSource, control.Style))
                {
                    continue;
                }

                control.ResolveTargets();
                Add(coveredObjects, control.targetGraphic, GetGraphic(control.fillRect),
                    GetGraphic(control.handleRect));
            }
        }

        private static void CollectScrollbars(HashSet<GameObject> coveredObjects)
        {
            LoogaScrollbar[] controls = FindSceneObjects<LoogaScrollbar>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaScrollbar control = controls[i];
                if (!UsesDesignSystem(control.StyleSource, control.Style))
                {
                    continue;
                }

                control.ResolveTargets();
                Add(coveredObjects, control.targetGraphic, GetGraphic(control.handleRect));
            }
        }

        private static void CollectInputFields(HashSet<GameObject> coveredObjects)
        {
            LoogaInputField[] controls = FindSceneObjects<LoogaInputField>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaInputField control = controls[i];
                if (!UsesDesignSystem(control.StyleSource, control.Style))
                {
                    continue;
                }

                control.ResolveTargets();
                Add(coveredObjects, control.targetGraphic, control.textComponent,
                    control.placeholder as TMP_Text);
            }
        }

        private static void CollectDropdowns(HashSet<GameObject> coveredObjects)
        {
            LoogaDropdown[] controls = FindSceneObjects<LoogaDropdown>();
            for (int i = 0; i < controls.Length; i++)
            {
                LoogaDropdown control = controls[i];
                if (!UsesDesignSystem(control.StyleSource, control.Style))
                {
                    continue;
                }

                control.ResolveTargets();
                Add(coveredObjects, control.targetGraphic, control.captionText,
                    control.captionImage, FindDropdownArrow(control), control.itemText,
                    control.itemImage);
            }
        }

        private static bool UsesDesignSystem(LoogaControlStyleSource source, Object style)
        {
            return source == LoogaControlStyleSource.DesignSystem && style != null;
        }

        private static T[] FindSceneObjects<T>() where T : Component
        {
            T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<T> sceneObjects = new List<T>(found.Length);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].gameObject.scene.IsValid())
                {
                    sceneObjects.Add(found[i]);
                }
            }

            return sceneObjects.ToArray();
        }

        private static void Add(HashSet<GameObject> coveredObjects, params Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    coveredObjects.Add(components[i].gameObject);
                }
            }
        }

        private static Graphic GetGraphic(RectTransform transform)
        {
            return transform != null ? transform.GetComponent<Graphic>() : null;
        }

        private static Graphic FindAdditionalGraphic(Transform root, Graphic background, TMP_Text label)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != background && graphics[i] != label)
                {
                    return graphics[i];
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
    }
}
