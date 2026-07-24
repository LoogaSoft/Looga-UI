using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    sealed class LoogaUISoftMaskMaterialCache
    {
        readonly Action<Material> _applyParameters;
        readonly List<Entry> _entries = new();
        Shader _defaultShader;

        public LoogaUISoftMaskMaterialCache(Action<Material> applyParameters)
        {
            _applyParameters = applyParameters;
        }

        public Material Get(Material original)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (!ReferenceEquals(entry.Original, original))
                {
                    continue;
                }
                if (entry.Replacement != null)
                {
                    CopyOriginalProperties(original, entry.Replacement);
                    _applyParameters(entry.Replacement);
                }

                return entry.Replacement;
            }

            Material replacement = CreateReplacement(original);
            if (replacement != null)
            {
                replacement.hideFlags = HideFlags.HideAndDontSave;
                _applyParameters(replacement);
            }

            _entries.Add(new Entry(original, replacement));
            return replacement;
        }

        public void Release(Material replacement)
        {
            // Replacement variants are owned by the mask cache and reused while the mask is alive.
            // They are released together in Clear(), which avoids reference-count churn from
            // Unity calling IMaterialModifier multiple times during canvas rebuilds.
        }

        public void ApplyAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Material replacement = _entries[i].Replacement;
                if (replacement != null)
                {
                    _applyParameters(replacement);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                DestroyMaterial(_entries[i].Replacement);
            }

            _entries.Clear();
        }

        Material CreateReplacement(Material original)
        {
            if (original != null && original.HasProperty(LoogaUISoftMaskShaderIds.MaskTexture))
            {
                return new Material(original);
            }

            Shader shader = DefaultShader;
            if (shader == null)
            {
                Debug.LogWarning("Looga UI FX soft mask shader could not be found.");
                return null;
            }

            Material replacement = new(shader);
            CopyOriginalProperties(original, replacement);
            return replacement;
        }

        Shader DefaultShader => _defaultShader != null
            ? _defaultShader
            : (_defaultShader = Shader.Find("Hidden/LoogaSoft/UI FX/Soft Masked UI"));

        static void CopyOriginalProperties(Material original, Material replacement)
        {
            if (original != null)
            {
                replacement.CopyPropertiesFromMaterial(original);
            }
            else
            {
                replacement.CopyPropertiesFromMaterial(Canvas.GetDefaultCanvasMaterial());
            }
        }

        static void DestroyMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(material);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        sealed class Entry
        {
            public readonly Material Original;
            public readonly Material Replacement;

            public Entry(Material original, Material replacement)
            {
                Original = original;
                Replacement = replacement;
            }
        }
    }
}


