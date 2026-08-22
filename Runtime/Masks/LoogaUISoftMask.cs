using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace LoogaSoft.UI.Extensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("LoogaSoft/UI/Looga UI Soft Mask")]
    public sealed class LoogaUISoftMask : UIBehaviour, ICanvasRaycastFilter
    {
        private static readonly List<LoogaUISoftMaskTarget> TargetBuffer = new();
        private static readonly List<Graphic> GraphicBuffer = new();

        [Header("Mask")]
        [SerializeField, Tooltip("Where the mask alpha comes from. Graphic uses the Image or RawImage on the mask object.")]
        private LoogaUISoftMaskSource _source = LoogaUISoftMaskSource.Graphic;

        [SerializeField, Tooltip("How child graphics become affected by this mask. Automatic Children is the normal designer workflow.")]
        private LoogaUISoftMaskTargetMode _targetMode = LoogaUISoftMaskTargetMode.AutomaticChildren;

        [SerializeField, Tooltip("Optional RectTransform used as the mask bounds. If empty, this object's RectTransform is used.")]
        private RectTransform _maskTransform;

        [SerializeField, Tooltip("Sprite used when Source is Sprite.")]
        private Sprite _sprite;

        [SerializeField, Tooltip("Texture or render texture used when Source is Texture.")]
        private Texture _texture;

        [SerializeField, Tooltip("Normalized UV rectangle used when Source is Texture.")]
        private Rect _textureUvRect = new(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("The mask texture channel used to calculate visibility.")]
        private LoogaUISoftMaskChannel _channel = LoogaUISoftMaskChannel.Alpha;

        [Header("Behavior")]
        [SerializeField, Tooltip("Invert visibility inside the mask bounds.")]
        private bool _invert;

        [SerializeField, Tooltip("Make pixels outside the mask bounds visible instead of hidden.")]
        private bool _invertOutside;

        [SerializeField, Tooltip("When enabled, pointer input is filtered by the mask alpha.")]
        private bool _affectRaycasts;

        [SerializeField, Range(0f, 1f), Tooltip("Minimum sampled mask value required for a raycast to pass. Requires a CPU-readable Texture2D source.")]
        private float _raycastThreshold = 0.1f;

        [SerializeField, Tooltip("Include inactive child graphics when automatically assigning mask targets.")]
        private bool _includeInactiveTargets;

        private readonly LoogaUISoftMaskMaterialCache _materials;
        private RectTransform _rectTransform;
        private Graphic _graphic;
        private Canvas _canvas;
        private bool _dirty = true;

        public LoogaUISoftMask()
        {
            _materials = new LoogaUISoftMaskMaterialCache(ApplyMaterialParameters);
        }

        public bool IsMaskingEnabled => isActiveAndEnabled && Canvas != null;
        public LoogaUISoftMaskTargetMode TargetMode => _targetMode;

        private RectTransform ActiveMaskTransform => _maskTransform != null ? _maskTransform : RectTransform;

        private RectTransform RectTransform => _rectTransform != null
            ? _rectTransform
            : (_rectTransform = transform as RectTransform);

        private Canvas Canvas => _canvas != null
            ? _canvas
            : (_canvas = GetComponentInParent<Canvas>());

        private Graphic Graphic => _graphic != null
            ? _graphic
            : (_graphic = ActiveMaskTransform != null ? ActiveMaskTransform.GetComponent<Graphic>() : null);

        public Material GetReplacement(Material original)
        {
            return IsMaskingEnabled ? _materials.Get(original) : original;
        }

        public void ReleaseReplacement(Material replacement)
        {
            _materials.Release(replacement);
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!_affectRaycasts)
            {
                return true;
            }

            RectTransform mask = ActiveMaskTransform;
            if (mask == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(mask, screenPoint, eventCamera, out Vector2 localPoint))
            {
                return false;
            }

            if (!mask.rect.Contains(localPoint))
            {
                return _invertOutside;
            }

            if (_raycastThreshold <= 0f)
            {
                return true;
            }

            if (!TrySampleMask(localPoint, out float value))
            {
                return true;
            }

            if (_invert)
            {
                value = 1f - value;
            }

            return value >= _raycastThreshold;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Canvas.willRenderCanvases += HandleWillRenderCanvases;
            RegisterGraphicCallbacks();
            MarkDirty();
            RefreshTargets();
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= HandleWillRenderCanvases;
            UnregisterGraphicCallbacks();
            NotifyTargets();
            _materials.Clear();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            NotifyTargets();
            _materials.Clear();
            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            MarkDirty();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            _canvas = null;
            MarkDirty();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            _canvas = null;
            MarkDirty();
            NotifyTargets();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            MarkDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            MarkDirty();
            NotifyTargets();
        }
#endif

        private void LateUpdate()
        {
            if (_targetMode == LoogaUISoftMaskTargetMode.AutomaticChildren)
            {
                RefreshTargets();
            }
        }

        private void HandleWillRenderCanvases()
        {
            if (!IsMaskingEnabled)
            {
                return;
            }

            if (_dirty || ActiveMaskTransform.hasChanged)
            {
                _dirty = false;
                ActiveMaskTransform.hasChanged = false;
                _materials.ApplyAll();
            }
        }

        private void RegisterGraphicCallbacks()
        {
            Graphic graphic = Graphic;
            if (graphic == null)
            {
                return;
            }

            graphic.RegisterDirtyMaterialCallback(MarkDirty);
            graphic.RegisterDirtyVerticesCallback(MarkDirty);
        }

        void UnregisterGraphicCallbacks()
        {
            if (_graphic == null)
            {
                return;
            }

            _graphic.UnregisterDirtyMaterialCallback(MarkDirty);
            _graphic.UnregisterDirtyVerticesCallback(MarkDirty);
            _graphic = null;
        }

        void RefreshTargets()
        {
            if (_targetMode != LoogaUISoftMaskTargetMode.AutomaticChildren)
            {
                return;
            }

            GetComponentsInChildren(_includeInactiveTargets, GraphicBuffer);
            for (int i = 0; i < GraphicBuffer.Count; i++)
            {
                Graphic childGraphic = GraphicBuffer[i];
                if (childGraphic == null || childGraphic.transform == transform)
                {
                    continue;
                }

                LoogaUISoftMaskTarget target = childGraphic.GetComponent<LoogaUISoftMaskTarget>();
                if (target == null)
                {
                    target = childGraphic.gameObject.AddComponent<LoogaUISoftMaskTarget>();
                    target.hideFlags = HideFlags.HideInInspector;
                }

                target.SetManagedBy(this);
            }

            GraphicBuffer.Clear();
        }

        void NotifyTargets()
        {
            transform.GetComponentsInChildren(true, TargetBuffer);
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                TargetBuffer[i]?.MaskMightHaveChanged();
            }

            TargetBuffer.Clear();
        }

        void ApplyMaterialParameters(Material material)
        {
            ApplyMaterialParameters(material, 0);
            material.SetFloat(LoogaUISoftMaskShaderIds.MaskCount, 1f);
        }

        internal void ApplyMaterialParameters(Material material, int index)
        {
            if (material == null)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, LoogaUISoftMaskShaderIds.MaxMaskCount - 1);
            material.SetTexture(LoogaUISoftMaskShaderIds.MaskTexture, ResolveTexture());
            material.SetVector(LoogaUISoftMaskShaderIds.MaskRect, ToVector(ActiveMaskTransform.rect));
            material.SetVector(LoogaUISoftMaskShaderIds.MaskUvRect, ResolveUvRect());
            material.SetMatrix(LoogaUISoftMaskShaderIds.WorldToMask, ResolveWorldToMaskMatrix());
            material.SetColor(LoogaUISoftMaskShaderIds.ChannelWeights, ResolveChannelWeights());
            material.SetFloat(LoogaUISoftMaskShaderIds.Invert, _invert ? 1f : 0f);
            material.SetFloat(LoogaUISoftMaskShaderIds.InvertOutside, _invertOutside ? 1f : 0f);

            material.SetTexture(LoogaUISoftMaskShaderIds.MaskTextures[index], ResolveTexture());
            material.SetVector(LoogaUISoftMaskShaderIds.MaskRects[index], ToVector(ActiveMaskTransform.rect));
            material.SetVector(LoogaUISoftMaskShaderIds.MaskUvRects[index], ResolveUvRect());
            material.SetMatrix(LoogaUISoftMaskShaderIds.WorldToMasks[index], ResolveWorldToMaskMatrix());
            material.SetColor(LoogaUISoftMaskShaderIds.ChannelWeightsList[index], ResolveChannelWeights());
            material.SetFloat(LoogaUISoftMaskShaderIds.Inverts[index], _invert ? 1f : 0f);
            material.SetFloat(LoogaUISoftMaskShaderIds.InvertOutsides[index], _invertOutside ? 1f : 0f);
        }

        Texture ResolveTexture()
        {
            return _source switch
            {
                LoogaUISoftMaskSource.Sprite => _sprite != null ? _sprite.texture : Texture2D.whiteTexture,
                LoogaUISoftMaskSource.Texture => _texture != null ? _texture : Texture2D.whiteTexture,
                _ => ResolveGraphicTexture()
            };
        }

        Texture ResolveGraphicTexture()
        {
            return Graphic switch
            {
                Image image when image.sprite != null => image.sprite.texture,
                RawImage rawImage when rawImage.texture != null => rawImage.texture,
                _ => Texture2D.whiteTexture
            };
        }

        Vector4 ResolveUvRect()
        {
            return _source switch
            {
                LoogaUISoftMaskSource.Sprite => SpriteUvRect(_sprite),
                LoogaUISoftMaskSource.Texture => ToVector(_textureUvRect),
                _ => GraphicUvRect()
            };
        }

        Vector4 GraphicUvRect()
        {
            return Graphic switch
            {
                Image image => SpriteUvRect(image.sprite),
                RawImage rawImage => ToVector(rawImage.uvRect),
                _ => new Vector4(0f, 0f, 1f, 1f)
            };
        }

        Matrix4x4 ResolveWorldToMaskMatrix()
        {
            Transform root = Canvas != null && Canvas.rootCanvas != null
                ? Canvas.rootCanvas.transform
                : transform.root;
            return ActiveMaskTransform.worldToLocalMatrix * root.localToWorldMatrix;
        }

        Color ResolveChannelWeights()
        {
            return _channel switch
            {
                LoogaUISoftMaskChannel.Red => new Color(1f, 0f, 0f, 0f),
                LoogaUISoftMaskChannel.Green => new Color(0f, 1f, 0f, 0f),
                LoogaUISoftMaskChannel.Blue => new Color(0f, 0f, 1f, 0f),
                LoogaUISoftMaskChannel.Grayscale => new Color(0.333333f, 0.333333f, 0.333333f, 0f),
                _ => new Color(0f, 0f, 0f, 1f)
            };
        }

        bool TrySampleMask(Vector2 localPoint, out float value)
        {
            value = 1f;
            Texture texture = ResolveTexture();
            if (texture is not Texture2D texture2D)
            {
                return false;
            }

            Rect rect = ActiveMaskTransform.rect;
            Vector2 normalized = new(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));
            Vector4 uvRect = ResolveUvRect();
            float u = Mathf.Lerp(uvRect.x, uvRect.z, normalized.x);
            float v = Mathf.Lerp(uvRect.y, uvRect.w, normalized.y);

            try
            {
                value = MaskValue(texture2D.GetPixelBilinear(u, v));
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        float MaskValue(Color color)
        {
            Color weights = ResolveChannelWeights();
            Color weighted = color * weights;
            return weighted.r + weighted.g + weighted.b + weighted.a;
        }

        static Vector4 SpriteUvRect(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Rect textureRect = sprite.textureRect;
            Texture texture = sprite.texture;
            return new Vector4(
                textureRect.xMin / texture.width,
                textureRect.yMin / texture.height,
                textureRect.xMax / texture.width,
                textureRect.yMax / texture.height);
        }

        static Vector4 ToVector(Rect rect)
        {
            return new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
        }
    }
}


