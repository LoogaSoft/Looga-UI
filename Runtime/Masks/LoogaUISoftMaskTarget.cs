using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [ExecuteAlways]
    [AddComponentMenu("LoogaSoft/UI FX/Looga UI Soft Mask Target")]
    [RequireComponent(typeof(Graphic))]
    public sealed class LoogaUISoftMaskTarget : UIBehaviour, IMaterialModifier
    {
        readonly List<LoogaUISoftMask> _masks = new(LoogaUISoftMaskShaderIds.MaxMaskCount);

        Graphic _graphic;
        Material _replacement;
        Material _original;
        Shader _defaultShader;
        bool _managed;
        bool _destroying;

        Graphic Graphic => _graphic != null ? _graphic : (_graphic = GetComponent<Graphic>());

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            ResolveMasks();
            if (_masks.Count == 0 || !IsGraphicMaskable())
            {
                ReleaseReplacement();
                return baseMaterial;
            }

            Material replacement = GetOrCreateReplacement(baseMaterial);
            if (replacement == null)
            {
                return baseMaterial;
            }

            CopyOriginalProperties(baseMaterial, replacement);
            ApplyMaskParameters(replacement);
            return replacement;
        }

        public void SetManagedBy(LoogaUISoftMask mask)
        {
            _managed = true;
            SetMaterialDirty();
        }

        public void MaskMightHaveChanged()
        {
            SetMaterialDirty();

            if (_managed && !HasAutomaticMaskAncestor())
            {
                DestroySelf();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MaskMightHaveChanged();
        }

        protected override void OnDisable()
        {
            ReleaseReplacement();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            _destroying = true;
            ReleaseReplacement();
            base.OnDestroy();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            MaskMightHaveChanged();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            MaskMightHaveChanged();
        }

        void ResolveMasks()
        {
            _masks.Clear();
            Transform current = transform.parent;
            while (current != null && _masks.Count < LoogaUISoftMaskShaderIds.MaxMaskCount)
            {
                LoogaUISoftMask mask = current.GetComponent<LoogaUISoftMask>();
                if (mask != null && mask.IsMaskingEnabled)
                {
                    _masks.Add(mask);
                }

                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null && canvas.overrideSorting)
                {
                    break;
                }

                current = current.parent;
            }
        }

        bool HasAutomaticMaskAncestor()
        {
            Transform current = transform.parent;
            while (current != null)
            {
                LoogaUISoftMask mask = current.GetComponent<LoogaUISoftMask>();
                if (mask != null && mask.TargetMode == LoogaUISoftMaskTargetMode.AutomaticChildren)
                {
                    return true;
                }

                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null && canvas.overrideSorting)
                {
                    return false;
                }

                current = current.parent;
            }

            return false;
        }

        Material GetOrCreateReplacement(Material baseMaterial)
        {
            if (_replacement != null && ReferenceEquals(_original, baseMaterial))
            {
                return _replacement;
            }

            ReleaseReplacement();
            Shader shader = DefaultShader;
            if (shader == null)
            {
                Debug.LogWarning("Looga UI FX soft mask shader could not be found.", this);
                return null;
            }

            _original = baseMaterial;
            _replacement = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return _replacement;
        }

        Shader DefaultShader => _defaultShader != null
            ? _defaultShader
            : (_defaultShader = Shader.Find("Hidden/LoogaSoft/UI FX/Soft Masked UI"));

        void ApplyMaskParameters(Material material)
        {
            int count = Mathf.Min(_masks.Count, LoogaUISoftMaskShaderIds.MaxMaskCount);
            material.SetFloat(LoogaUISoftMaskShaderIds.MaskCount, count);

            for (int i = 0; i < count; i++)
            {
                _masks[i].ApplyMaterialParameters(material, i);
            }

            for (int i = count; i < LoogaUISoftMaskShaderIds.MaxMaskCount; i++)
            {
                ClearMaskParameters(material, i);
            }
        }

        static void ClearMaskParameters(Material material, int index)
        {
            material.SetTexture(LoogaUISoftMaskShaderIds.MaskTextures[index], Texture2D.whiteTexture);
            material.SetVector(LoogaUISoftMaskShaderIds.MaskRects[index], new Vector4(0f, 0f, 1f, 1f));
            material.SetVector(LoogaUISoftMaskShaderIds.MaskUvRects[index], new Vector4(0f, 0f, 1f, 1f));
            material.SetMatrix(LoogaUISoftMaskShaderIds.WorldToMasks[index], Matrix4x4.identity);
            material.SetColor(LoogaUISoftMaskShaderIds.ChannelWeightsList[index], new Color(0f, 0f, 0f, 1f));
            material.SetFloat(LoogaUISoftMaskShaderIds.Inverts[index], 0f);
            material.SetFloat(LoogaUISoftMaskShaderIds.InvertOutsides[index], 0f);
        }

        bool IsGraphicMaskable()
        {
            Graphic graphic = Graphic;
            if (graphic == null)
            {
                return false;
            }

            return graphic is not MaskableGraphic maskableGraphic || maskableGraphic.maskable;
        }

        void SetMaterialDirty()
        {
            if (Graphic != null)
            {
                Graphic.SetMaterialDirty();
            }
        }

        void ReleaseReplacement()
        {
            if (_replacement == null)
            {
                _original = null;
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_replacement);
            }
            else
            {
                DestroyImmediate(_replacement);
            }

            _replacement = null;
            _original = null;
        }

        void DestroySelf()
        {
            if (_destroying)
            {
                return;
            }

            _destroying = true;
            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }

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
    }
}
