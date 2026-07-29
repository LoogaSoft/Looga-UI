using System;
#if LOOGA_UIFX_UNITASK_SUPPORT
using System.Threading;
using Cysharp.Threading.Tasks;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("LoogaSoft/UI FX/Looga UI Shadow")]
    public sealed class LoogaUIShadow : MonoBehaviour
    {
        const string ShadowObjectName = "Looga UI Shadow Renderer";
        const float MinResolutionScale = 0.125f;
        const float MaxResolutionScale = 1f;
        const int MaxGeneratedSize = 2048;

        [SerializeField] LoogaUIShadowMode _mode = LoogaUIShadowMode.Outer;
        [SerializeField] Color _color = new(0f, 0f, 0f, 0.45f);
        [SerializeField] Vector2 _offset = new(0f, -4f);
        [SerializeField, Min(0f)] float _softness = 12f;
        [SerializeField, Min(0f)] float _spread = 2f;
        [SerializeField, Range(MinResolutionScale, MaxResolutionScale)] float _resolutionScale = 0.5f;
        [SerializeField] LoogaUIShadowQuality _quality = LoogaUIShadowQuality.Medium;
        [SerializeField, Tooltip("Uses the source sprite alpha when building the shadow mask. Falls back to a rectangle if the texture cannot be sampled.")]
        bool _useSourceAlpha = true;
        [FormerlySerializedAs("_clipOuterShadowBehindSource")]
        [SerializeField, Tooltip("Removes outer shadow pixels that would render behind the source graphic.")]
        bool _clipSource = true;
        [SerializeField] bool _deallocateOnDisable = true;
#if LOOGA_UIFX_UNITASK_SUPPORT
        [SerializeField, Tooltip("Builds shadow pixels on UniTask's thread pool, then applies the generated texture on the main thread.")]
        bool _useAsyncRebuild = true;
#endif

        Graphic _graphic;
        RectTransform _rectTransform;
        RawImage _shadowImage;
        RectTransform _shadowRect;
        LayoutElement _shadowLayoutElement;
        GameObject _shadowObject;
        Texture2D _shadowTexture;
        Sprite _lastSprite;
        Rect _lastRect;
        int _lastSettingsHash;
        float _lastPadding;
        bool _dirty = true;
        bool _warnedUnreadableTexture;
#if LOOGA_UIFX_UNITASK_SUPPORT
        CancellationTokenSource _rebuildCancellation;
        bool _asyncRebuildInProgress;
        int _rebuildVersion;
#endif

        public LoogaUIShadowMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value)
                {
                    return;
                }

                _mode = value;
                MarkDirty();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                {
                    return;
                }

                _color = value;
                MarkDirty();
            }
        }

        public Vector2 Offset
        {
            get => _offset;
            set
            {
                if (_offset == value)
                {
                    return;
                }

                _offset = value;
                if (_mode == LoogaUIShadowMode.Inner)
                {
                    MarkDirty();
                }
                else
                {
                    UpdateRendererTransform();
                }
            }
        }

        public float Softness
        {
            get => _softness;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(_softness, value))
                {
                    return;
                }

                _softness = value;
                MarkDirty();
            }
        }

        public float Spread
        {
            get => _spread;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(_spread, value))
                {
                    return;
                }

                _spread = value;
                MarkDirty();
            }
        }

        public bool ClipSource
        {
            get => _clipSource;
            set
            {
                if (_clipSource == value)
                {
                    return;
                }

                _clipSource = value;
                MarkDirty();
            }
        }

        public bool ClipOuterShadowBehindSource
        {
            get => ClipSource;
            set => ClipSource = value;
        }

        void OnEnable()
        {
            CacheComponents();
            RegisterGraphicCallbacks();
            RegisterCanvasCallbacks();
            EnsureRenderer();
            MarkDirty();
        }

        void OnDisable()
        {
            UnregisterGraphicCallbacks();
            UnregisterCanvasCallbacks();
#if LOOGA_UIFX_UNITASK_SUPPORT
            CancelAsyncRebuild();
#endif

            if (_shadowImage != null)
            {
                _shadowImage.enabled = false;
            }

            if (_deallocateOnDisable)
            {
                ReleaseGeneratedTexture();
            }
        }

        void OnDestroy()
        {
            UnregisterGraphicCallbacks();
            UnregisterCanvasCallbacks();
#if LOOGA_UIFX_UNITASK_SUPPORT
            CancelAsyncRebuild();
#endif
            ReleaseGeneratedTexture();

            ReleaseRenderer();
        }

        void OnValidate()
        {
            _softness = Mathf.Max(0f, _softness);
            _spread = Mathf.Max(0f, _spread);
            _resolutionScale = Mathf.Clamp(_resolutionScale, MinResolutionScale, MaxResolutionScale);
            MarkDirty();
        }

        void OnRectTransformDimensionsChange()
        {
            MarkDirty();
        }

        void LateUpdate()
        {
            if (_graphic == null || _rectTransform == null)
            {
                CacheComponents();
            }

            if (_graphic == null || _rectTransform == null)
            {
                return;
            }

            EnsureRenderer();

            if (HasSourceChanged())
            {
                MarkDirty();
            }

            if (_dirty)
            {
#if LOOGA_UIFX_UNITASK_SUPPORT
                if (_useAsyncRebuild)
                {
                    StartAsyncRebuild();
                }
                else
#endif
                {
                    RebuildShadow();
                }
            }

            UpdateRendererTransform();
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        void CacheComponents()
        {
            _graphic = GetComponent<Graphic>();
            _rectTransform = transform as RectTransform;
        }

        void RegisterGraphicCallbacks()
        {
            if (_graphic == null)
            {
                return;
            }

            _graphic.RegisterDirtyMaterialCallback(MarkDirty);
            _graphic.RegisterDirtyVerticesCallback(MarkDirty);
        }

        void RegisterCanvasCallbacks()
        {
            Canvas.willRenderCanvases -= UpdateRendererTransform;
            Canvas.willRenderCanvases += UpdateRendererTransform;
        }

        void UnregisterGraphicCallbacks()
        {
            if (_graphic == null)
            {
                return;
            }

            _graphic.UnregisterDirtyMaterialCallback(MarkDirty);
            _graphic.UnregisterDirtyVerticesCallback(MarkDirty);
        }

        void UnregisterCanvasCallbacks()
        {
            Canvas.willRenderCanvases -= UpdateRendererTransform;
        }

        bool HasSourceChanged()
        {
            Image image = _graphic as Image;
            Sprite sprite = image != null ? image.sprite : GetSourceSprite();
            Rect rect = _rectTransform.rect;
            int settingsHash = GetSettingsHash();
            return _lastSprite != sprite || _lastRect != rect || _lastSettingsHash != settingsHash;
        }

        int GetSettingsHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _mode.GetHashCode();
                hash = hash * 31 + _color.GetHashCode();
                if (_mode == LoogaUIShadowMode.Inner)
                {
                    hash = hash * 31 + _offset.GetHashCode();
                }

                hash = hash * 31 + _softness.GetHashCode();
                hash = hash * 31 + _spread.GetHashCode();
                hash = hash * 31 + _resolutionScale.GetHashCode();
                hash = hash * 31 + _quality.GetHashCode();
                hash = hash * 31 + _useSourceAlpha.GetHashCode();
                hash = hash * 31 + _clipSource.GetHashCode();
                return hash;
            }
        }

        Sprite GetSourceSprite()
        {
            return _graphic is Image image ? image.sprite : null;
        }

        void EnsureRenderer()
        {
            if (_shadowImage != null)
            {
                _shadowImage.enabled = isActiveAndEnabled;
                EnsureIgnoredByLayout();
                return;
            }

            Transform parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            GameObject shadowObject = new(ShadowObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(LayoutElement));
            shadowObject.hideFlags = HideFlags.HideAndDontSave;
            _shadowRect = shadowObject.GetComponent<RectTransform>();
            _shadowImage = shadowObject.GetComponent<RawImage>();
            _shadowLayoutElement = shadowObject.GetComponent<LayoutElement>();
            _shadowObject = shadowObject;
            EnsureIgnoredByLayout();
            shadowObject.transform.SetParent(parent, false);
            shadowObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
            _shadowImage.raycastTarget = false;
            _shadowImage.maskable = _graphic is not MaskableGraphic maskableGraphic || maskableGraphic.maskable;
            _shadowImage.enabled = isActiveAndEnabled;
        }

        void EnsureIgnoredByLayout()
        {
            if (_shadowImage == null)
            {
                return;
            }

            if (_shadowLayoutElement == null)
            {
                _shadowLayoutElement = _shadowImage.GetComponent<LayoutElement>();
            }

            if (_shadowLayoutElement == null)
            {
                _shadowLayoutElement = _shadowImage.gameObject.AddComponent<LayoutElement>();
            }

            _shadowLayoutElement.ignoreLayout = true;
        }

        void RebuildShadow()
        {
            if (!TryCreateBuildRequest(out ShadowBuildRequest request))
            {
                return;
            }

            ApplyShadowTexture(request.Width, request.Height, BuildShadowPixels(request));
        }

        bool TryCreateBuildRequest(out ShadowBuildRequest request)
        {
            request = default;
            if (_shadowImage == null)
            {
                return false;
            }

            _dirty = false;
            _lastSprite = GetSourceSprite();
            _lastRect = _rectTransform.rect;
            _lastSettingsHash = GetSettingsHash();

            Vector2 size = _rectTransform.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                _shadowImage.texture = null;
                return false;
            }

            _lastPadding = Mathf.Ceil(_spread + _softness * GetBlurPasses() + 2f);
            int width = Mathf.Clamp(Mathf.CeilToInt((size.x + _lastPadding * 2f) * _resolutionScale), 1, MaxGeneratedSize);
            int height = Mathf.Clamp(Mathf.CeilToInt((size.y + _lastPadding * 2f) * _resolutionScale), 1, MaxGeneratedSize);
            float[] sourceAlpha = new float[width * height];
            WriteSourceAlpha(sourceAlpha, width, height, size);
            request = new ShadowBuildRequest(_mode, _color, _offset, _softness, _spread, _resolutionScale, GetBlurPasses(), width, height, sourceAlpha, _clipSource);
            return true;
        }

        void ApplyShadowTexture(int width, int height, Color32[] pixels)
        {
            Texture2D texture = GetOrCreateTexture(width, height);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            _shadowImage.texture = texture;
            _shadowImage.color = Color.white;
        }

#if LOOGA_UIFX_UNITASK_SUPPORT
        void StartAsyncRebuild()
        {
            if (_asyncRebuildInProgress || !TryCreateBuildRequest(out ShadowBuildRequest request))
            {
                return;
            }

            _asyncRebuildInProgress = true;
            _rebuildCancellation?.Cancel();
            _rebuildCancellation?.Dispose();
            _rebuildCancellation = new CancellationTokenSource();
            RebuildShadowAsync(request, ++_rebuildVersion, _rebuildCancellation.Token).Forget();
        }

        async UniTaskVoid RebuildShadowAsync(ShadowBuildRequest request, int version, CancellationToken cancellationToken)
        {
            try
            {
                Color32[] pixels = await UniTask.RunOnThreadPool(() => BuildShadowPixels(request), cancellationToken: cancellationToken);
                await UniTask.SwitchToMainThread(cancellationToken);

                if (this == null || cancellationToken.IsCancellationRequested || version != _rebuildVersion)
                {
                    return;
                }

                ApplyShadowTexture(request.Width, request.Height, pixels);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (this != null && version == _rebuildVersion)
                {
                    _asyncRebuildInProgress = false;
                }
            }
        }

        void CancelAsyncRebuild()
        {
            _rebuildCancellation?.Cancel();
            _rebuildCancellation?.Dispose();
            _rebuildCancellation = null;
            _asyncRebuildInProgress = false;
            _rebuildVersion++;
        }
#endif

        static Color32[] BuildShadowPixels(ShadowBuildRequest request)
        {
            return request.Mode == LoogaUIShadowMode.Inner
                ? BuildInnerShadowPixels(request)
                : BuildOuterShadowPixels(request);
        }

        static Color32[] BuildOuterShadowPixels(ShadowBuildRequest request)
        {
            float[] originalAlpha = request.SourceAlpha;
            float[] sourceAlpha = originalAlpha;
            int spreadRadius = Mathf.RoundToInt(request.Spread * request.ResolutionScale);
            if (spreadRadius > 0)
            {
                float[] spreadAlpha = new float[sourceAlpha.Length];
                Dilate(sourceAlpha, spreadAlpha, request.Width, request.Height, spreadRadius);
                sourceAlpha = spreadAlpha;
            }

            sourceAlpha = BlurAlpha(sourceAlpha, request.Width, request.Height, request.Softness, request.ResolutionScale, request.BlurPasses);
            Color32[] pixels = new Color32[sourceAlpha.Length];
            float alphaScale = Mathf.Clamp01(request.Color.a);
            byte r = FloatToByte(request.Color.r);
            byte g = FloatToByte(request.Color.g);
            byte b = FloatToByte(request.Color.b);
            int offsetX = Mathf.RoundToInt(request.Offset.x * request.ResolutionScale);
            int offsetY = Mathf.RoundToInt(request.Offset.y * request.ResolutionScale);

            for (int y = 0; y < request.Height; y++)
            {
                for (int x = 0; x < request.Width; x++)
                {
                    int index = y * request.Width + x;
                    float alpha = sourceAlpha[index];
                    if (request.ClipSource)
                    {
                        alpha *= 1f - SampleCoverage(originalAlpha, request.Width, request.Height, x + offsetX, y + offsetY);
                    }

                    pixels[index] = new Color32(r, g, b, FloatToByte(alpha * alphaScale));
                }
            }

            return pixels;
        }

        static Color32[] BuildInnerShadowPixels(ShadowBuildRequest request)
        {
            float[] sourceAlpha = request.SourceAlpha;
            int width = request.Width;
            int height = request.Height;
            float[] insideAlpha = new float[sourceAlpha.Length];
            float[] edgeAlpha = new float[sourceAlpha.Length];
            for (int i = 0; i < sourceAlpha.Length; i++)
            {
                insideAlpha[i] = sourceAlpha[i];
                edgeAlpha[i] = 1f - sourceAlpha[i];
            }

            int spreadRadius = Mathf.RoundToInt(request.Spread * request.ResolutionScale);
            if (spreadRadius > 0)
            {
                float[] spreadAlpha = new float[edgeAlpha.Length];
                Dilate(edgeAlpha, spreadAlpha, width, height, spreadRadius);
                edgeAlpha = spreadAlpha;
            }

            edgeAlpha = BlurAlpha(edgeAlpha, width, height, request.Softness, request.ResolutionScale, request.BlurPasses);

            Color32[] pixels = new Color32[sourceAlpha.Length];
            byte r = FloatToByte(request.Color.r);
            byte g = FloatToByte(request.Color.g);
            byte b = FloatToByte(request.Color.b);
            float alphaScale = Mathf.Clamp01(request.Color.a);
            int sampleOffsetX = Mathf.RoundToInt(request.Offset.x * request.ResolutionScale);
            int sampleOffsetY = Mathf.RoundToInt(request.Offset.y * request.ResolutionScale);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceIndex = y * width + x;
                    int sampleX = Mathf.Clamp(x - sampleOffsetX, 0, width - 1);
                    int sampleY = Mathf.Clamp(y - sampleOffsetY, 0, height - 1);
                    float alpha = insideAlpha[sourceIndex] * edgeAlpha[sampleY * width + sampleX] * alphaScale;
                    pixels[sourceIndex] = new Color32(r, g, b, FloatToByte(alpha));
                }
            }

            return pixels;
        }

        static float[] BlurAlpha(float[] sourceAlpha, int width, int height, float softness, float resolutionScale, int passes)
        {
            int blurRadius = Mathf.RoundToInt(softness * resolutionScale);
            if (blurRadius <= 0)
            {
                return sourceAlpha;
            }

            float[] temp = new float[sourceAlpha.Length];
            float[] blurred = new float[sourceAlpha.Length];

            for (int pass = 0; pass < passes; pass++)
            {
                BoxBlur(sourceAlpha, temp, blurred, width, height, blurRadius);
                (sourceAlpha, blurred) = (blurred, sourceAlpha);
            }

            return sourceAlpha;
        }

        void WriteSourceAlpha(float[] alpha, int width, int height, Vector2 sourceSize)
        {
            Image image = _graphic as Image;
            Sprite sprite = image != null ? image.sprite : GetSourceSprite();
            if (!_useSourceAlpha || sprite == null || sprite.texture == null)
            {
                WriteRectAlpha(alpha, width, height, sourceSize);
                return;
            }

            Texture2D texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            Color32[] sourcePixels;

            if (!TryReadTexturePixels(texture, out sourcePixels))
            {
                if (!_warnedUnreadableTexture)
                {
                    Debug.LogWarning($"LoogaUIShadow on '{name}' could not read the source texture. Enable Read/Write on the sprite texture or disable Use Source Alpha.", this);
                    _warnedUnreadableTexture = true;
                }

                WriteRectAlpha(alpha, width, height, sourceSize);
                return;
            }

            float horizontalPadding = _lastPadding * _resolutionScale;
            float verticalPadding = _lastPadding * _resolutionScale;
            float contentWidth = Mathf.Max(1f, width - horizontalPadding * 2f);
            float contentHeight = Mathf.Max(1f, height - verticalPadding * 2f);

            for (int y = 0; y < height; y++)
            {
                float localY = (y - verticalPadding) / contentHeight;
                if (localY < 0f || localY > 1f)
                {
                    continue;
                }

                for (int x = 0; x < width; x++)
                {
                    float localX = (x - horizontalPadding) / contentWidth;
                    if (localX < 0f || localX > 1f)
                    {
                        continue;
                    }

                    float imageLocalX = localX;
                    float imageLocalY = localY;
                    if (image != null && !TryResolveImageLocalUv(image, localX, localY, sourceSize, out imageLocalX, out imageLocalY))
                    {
                        continue;
                    }

                    if (image != null && !IsVisibleForFill(image, imageLocalX, imageLocalY))
                    {
                        continue;
                    }

                    Vector2 spriteUv = image != null ? ResolveImageUv(image, imageLocalX, imageLocalY, sourceSize) : new Vector2(localX, localY);
                    if (spriteUv.x < 0f || spriteUv.y < 0f)
                    {
                        continue;
                    }

                    int sourceX = Mathf.Clamp(Mathf.RoundToInt(textureRect.x + spriteUv.x * (textureRect.width - 1f)), 0, texture.width - 1);
                    int sourceY = Mathf.Clamp(Mathf.RoundToInt(textureRect.y + spriteUv.y * (textureRect.height - 1f)), 0, texture.height - 1);
                    alpha[y * width + x] = sourcePixels[sourceY * texture.width + sourceX].a / 255f;
                }
            }
        }

        static bool TryReadTexturePixels(Texture2D texture, out Color32[] pixels)
        {
            try
            {
                pixels = texture.GetPixels32();
                return true;
            }
            catch (Exception exception) when (exception is UnityException || exception is ArgumentException)
            {
            }

            return TryReadTexturePixelsFromRenderTexture(texture, out pixels);
        }

        static bool TryReadTexturePixelsFromRenderTexture(Texture2D texture, out Color32[] pixels)
        {
            pixels = null;
            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D readableTexture = null;

            try
            {
                renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;

                readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                readableTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0, false);
                readableTexture.Apply(false, false);
                pixels = readableTexture.GetPixels32();
                return true;
            }
            catch (Exception exception) when (exception is UnityException || exception is ArgumentException)
            {
                pixels = null;
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (readableTexture != null)
                {
                    DestroyGeneratedObject(readableTexture);
                }
            }
        }

        void WriteRectAlpha(float[] alpha, int width, int height, Vector2 sourceSize)
        {
            float horizontalPadding = _lastPadding * _resolutionScale;
            float verticalPadding = _lastPadding * _resolutionScale;
            float contentWidth = Mathf.Max(1f, sourceSize.x * _resolutionScale);
            float contentHeight = Mathf.Max(1f, sourceSize.y * _resolutionScale);
            int minX = Mathf.Clamp(Mathf.FloorToInt(horizontalPadding), 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(horizontalPadding + contentWidth), 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(verticalPadding), 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(verticalPadding + contentHeight), 0, height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    alpha[y * width + x] = 1f;
                }
            }
        }

        Texture2D GetOrCreateTexture(int width, int height)
        {
            if (_shadowTexture != null && _shadowTexture.width == width && _shadowTexture.height == height)
            {
                return _shadowTexture;
            }

            ReleaseGeneratedTexture();
            _shadowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{name} Shadow Texture",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            return _shadowTexture;
        }

        int GetBlurPasses()
        {
            return _quality switch
            {
                LoogaUIShadowQuality.Low => 1,
                LoogaUIShadowQuality.High => 3,
                _ => 2
            };
        }

        void UpdateRendererTransform()
        {
            if (_shadowRect == null || _rectTransform == null || transform.parent is not RectTransform parent)
            {
                return;
            }

            EnsureIgnoredByLayout();

            Vector2 padding = Vector2.one * (_lastPadding * 2f);
            Vector3 sourceCenter = parent.InverseTransformPoint(_rectTransform.TransformPoint(_rectTransform.rect.center));
            Vector2 offset = _mode == LoogaUIShadowMode.Outer ? _offset : Vector2.zero;

            _shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _shadowRect.pivot = new Vector2(0.5f, 0.5f);
            _shadowRect.sizeDelta = _rectTransform.rect.size + padding;
            _shadowRect.localPosition = new Vector3(sourceCenter.x + offset.x, sourceCenter.y + offset.y, _rectTransform.localPosition.z);
            _shadowRect.localScale = _rectTransform.localScale;
            _shadowRect.localRotation = _rectTransform.localRotation;
            SetRendererSibling();
        }

        void SetRendererSibling()
        {
            int sourceIndex = transform.GetSiblingIndex();
            int shadowIndex = _shadowRect.GetSiblingIndex();

            if (_mode == LoogaUIShadowMode.Inner)
            {
                int targetIndex = shadowIndex < sourceIndex ? sourceIndex : sourceIndex + 1;
                _shadowRect.SetSiblingIndex(targetIndex);
                return;
            }

            int outerTargetIndex = shadowIndex < sourceIndex ? sourceIndex - 1 : sourceIndex;
            _shadowRect.SetSiblingIndex(Mathf.Max(0, outerTargetIndex));
        }

        static void BoxBlur(float[] source, float[] temp, float[] target, int width, int height, int radius)
        {
            int diameter = radius * 2 + 1;

            for (int y = 0; y < height; y++)
            {
                float sum = 0f;
                int row = y * width;

                for (int x = -radius; x <= radius; x++)
                {
                    sum += SampleAlpha(source, width, height, x, y);
                }

                for (int x = 0; x < width; x++)
                {
                    temp[row + x] = sum / diameter;
                    int removeX = x - radius;
                    int addX = x + radius + 1;
                    sum += SampleAlpha(source, width, height, addX, y) - SampleAlpha(source, width, height, removeX, y);
                }
            }

            for (int x = 0; x < width; x++)
            {
                float sum = 0f;

                for (int y = -radius; y <= radius; y++)
                {
                    sum += SampleAlpha(temp, width, height, x, y);
                }

                for (int y = 0; y < height; y++)
                {
                    target[y * width + x] = sum / diameter;
                    int removeY = y - radius;
                    int addY = y + radius + 1;
                    sum += SampleAlpha(temp, width, height, x, addY) - SampleAlpha(temp, width, height, x, removeY);
                }
            }
        }

        static void Dilate(float[] source, float[] target, int width, int height, int radius)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float max = 0f;
                    int minY = Mathf.Max(0, y - radius);
                    int maxY = Mathf.Min(height - 1, y + radius);
                    int minX = Mathf.Max(0, x - radius);
                    int maxX = Mathf.Min(width - 1, x + radius);

                    for (int sampleY = minY; sampleY <= maxY; sampleY++)
                    {
                        int row = sampleY * width;
                        for (int sampleX = minX; sampleX <= maxX; sampleX++)
                        {
                            max = Mathf.Max(max, source[row + sampleX]);
                        }
                    }

                    target[y * width + x] = max;
                }
            }
        }

        static float SampleAlpha(float[] alpha, int width, int height, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return 0f;
            }

            return alpha[y * width + x];
        }

        static float SampleCoverage(float[] alpha, int width, int height, int x, int y)
        {
            return SampleAlpha(alpha, width, height, x, y) > 0.001f ? 1f : 0f;
        }

        static byte FloatToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        static Vector2 ResolveImageUv(Image image, float normalizedX, float normalizedY, Vector2 sourceSize)
        {
            return image.type switch
            {
                Image.Type.Sliced => ResolveSlicedUv(image, normalizedX, normalizedY, sourceSize),
                Image.Type.Tiled => ResolveTiledUv(image, normalizedX, normalizedY, sourceSize),
                _ => new Vector2(normalizedX, normalizedY)
            };
        }

        static bool TryResolveImageLocalUv(Image image, float normalizedX, float normalizedY, Vector2 sourceSize, out float imageX, out float imageY)
        {
            imageX = normalizedX;
            imageY = normalizedY;

            if (!image.preserveAspect || image.sprite == null || image.type == Image.Type.Sliced || image.type == Image.Type.Tiled)
            {
                return true;
            }

            float spriteWidth = Mathf.Max(0.0001f, image.sprite.rect.width);
            float spriteHeight = Mathf.Max(0.0001f, image.sprite.rect.height);
            float spriteAspect = spriteWidth / spriteHeight;
            float rectWidth = Mathf.Max(0.0001f, sourceSize.x);
            float rectHeight = Mathf.Max(0.0001f, sourceSize.y);
            float rectAspect = rectWidth / rectHeight;

            if (rectAspect > spriteAspect)
            {
                float imageWidth = rectHeight * spriteAspect;
                float left = (rectWidth - imageWidth) * 0.5f;
                float localX = normalizedX * rectWidth;
                if (localX < left || localX > left + imageWidth)
                {
                    return false;
                }

                imageX = Mathf.InverseLerp(left, left + imageWidth, localX);
                return true;
            }

            float imageHeight = rectWidth / spriteAspect;
            float bottom = (rectHeight - imageHeight) * 0.5f;
            float localY = normalizedY * rectHeight;
            if (localY < bottom || localY > bottom + imageHeight)
            {
                return false;
            }

            imageY = Mathf.InverseLerp(bottom, bottom + imageHeight, localY);
            return true;
        }

        static Vector2 ResolveSlicedUv(Image image, float normalizedX, float normalizedY, Vector2 sourceSize)
        {
            Sprite sprite = image.sprite;
            if (sprite == null)
            {
                return new Vector2(normalizedX, normalizedY);
            }

            Vector4 border = sprite.border;
            Rect textureRect = sprite.textureRect;
            float pixelsPerUnit = GetMultipliedPixelsPerUnit(image);
            Vector4 localBorder = new(border.x / pixelsPerUnit, border.y / pixelsPerUnit, border.z / pixelsPerUnit, border.w / pixelsPerUnit);

            ClampBorderToSize(ref localBorder.x, ref localBorder.z, sourceSize.x);
            ClampBorderToSize(ref localBorder.y, ref localBorder.w, sourceSize.y);

            float localX = normalizedX * sourceSize.x;
            float localY = normalizedY * sourceSize.y;
            if (!image.fillCenter && IsInsideCenter(localX, localY, sourceSize, localBorder))
            {
                return new Vector2(-1f, -1f);
            }

            float uvX = ResolveSlicedAxis(localX, sourceSize.x, localBorder.x, localBorder.z, border.x, border.z, textureRect.width);
            float uvY = ResolveSlicedAxis(localY, sourceSize.y, localBorder.y, localBorder.w, border.y, border.w, textureRect.height);
            return new Vector2(uvX, uvY);
        }

        static Vector2 ResolveTiledUv(Image image, float normalizedX, float normalizedY, Vector2 sourceSize)
        {
            Sprite sprite = image.sprite;
            if (sprite == null)
            {
                return new Vector2(normalizedX, normalizedY);
            }

            float pixelsPerUnit = GetMultipliedPixelsPerUnit(image);
            Rect textureRect = sprite.textureRect;
            Vector4 border = sprite.border;
            Vector4 localBorder = new(border.x / pixelsPerUnit, border.y / pixelsPerUnit, border.z / pixelsPerUnit, border.w / pixelsPerUnit);

            ClampBorderToSize(ref localBorder.x, ref localBorder.z, sourceSize.x);
            ClampBorderToSize(ref localBorder.y, ref localBorder.w, sourceSize.y);

            float localX = normalizedX * sourceSize.x;
            float localY = normalizedY * sourceSize.y;
            if (!image.fillCenter && IsInsideCenter(localX, localY, sourceSize, localBorder))
            {
                return new Vector2(-1f, -1f);
            }

            float uvX = ResolveTiledAxis(localX, sourceSize.x, localBorder.x, localBorder.z, border.x, border.z, textureRect.width, pixelsPerUnit);
            float uvY = ResolveTiledAxis(localY, sourceSize.y, localBorder.y, localBorder.w, border.y, border.w, textureRect.height, pixelsPerUnit);
            return new Vector2(uvX, uvY);
        }

        static float GetMultipliedPixelsPerUnit(Image image)
        {
            return Mathf.Max(0.0001f, image.pixelsPerUnit * image.pixelsPerUnitMultiplier);
        }

        static bool IsInsideCenter(float localX, float localY, Vector2 sourceSize, Vector4 border)
        {
            return localX > border.x &&
                   localX < sourceSize.x - border.z &&
                   localY > border.y &&
                   localY < sourceSize.y - border.w;
        }

        static float ResolveSlicedAxis(float local, float size, float localStartBorder, float localEndBorder, float spriteStartBorder, float spriteEndBorder, float spriteSize)
        {
            if (local <= localStartBorder && localStartBorder > 0f)
            {
                return Mathf.Clamp01(local / localStartBorder * spriteStartBorder / spriteSize);
            }

            if (local >= size - localEndBorder && localEndBorder > 0f)
            {
                float distanceFromEnd = size - local;
                return Mathf.Clamp01(1f - distanceFromEnd / localEndBorder * spriteEndBorder / spriteSize);
            }

            float centerSize = Mathf.Max(0.0001f, size - localStartBorder - localEndBorder);
            float spriteCenterSize = Mathf.Max(0.0001f, spriteSize - spriteStartBorder - spriteEndBorder);
            float centerT = Mathf.Clamp01((local - localStartBorder) / centerSize);
            return Mathf.Clamp01((spriteStartBorder + centerT * spriteCenterSize) / spriteSize);
        }

        static float ResolveTiledAxis(float local, float size, float localStartBorder, float localEndBorder, float spriteStartBorder, float spriteEndBorder, float spriteSize, float pixelsPerUnit)
        {
            if (local <= localStartBorder && localStartBorder > 0f)
            {
                return Mathf.Clamp01(local / localStartBorder * spriteStartBorder / spriteSize);
            }

            if (local >= size - localEndBorder && localEndBorder > 0f)
            {
                float distanceFromEnd = size - local;
                return Mathf.Clamp01(1f - distanceFromEnd / localEndBorder * spriteEndBorder / spriteSize);
            }

            float spriteCenterSize = Mathf.Max(0.0001f, spriteSize - spriteStartBorder - spriteEndBorder);
            float localCenter = Mathf.Max(0f, local - localStartBorder) * pixelsPerUnit;
            return Mathf.Clamp01((spriteStartBorder + Mathf.Repeat(localCenter, spriteCenterSize)) / spriteSize);
        }

        static void ClampBorderToSize(ref float start, ref float end, float size)
        {
            float total = start + end;
            if (total <= size || total <= 0f)
            {
                return;
            }

            float scale = size / total;
            start *= scale;
            end *= scale;
        }

        static bool IsVisibleForFill(Image image, float normalizedX, float normalizedY)
        {
            if (image.type != Image.Type.Filled)
            {
                return true;
            }

            if (image.fillAmount <= 0f)
            {
                return false;
            }

            if (image.fillAmount >= 1f)
            {
                return true;
            }

            return image.fillMethod switch
            {
                Image.FillMethod.Horizontal => IsHorizontalFillVisible(image, normalizedX),
                Image.FillMethod.Vertical => IsVerticalFillVisible(image, normalizedY),
                Image.FillMethod.Radial90 => IsRadialFillVisible(image, normalizedX, normalizedY, 90f),
                Image.FillMethod.Radial180 => IsRadialFillVisible(image, normalizedX, normalizedY, 180f),
                Image.FillMethod.Radial360 => IsRadialFillVisible(image, normalizedX, normalizedY, 360f),
                _ => true
            };
        }

        static bool IsHorizontalFillVisible(Image image, float normalizedX)
        {
            bool fillFromRight = image.fillOrigin == (int)Image.OriginHorizontal.Right;
            return fillFromRight ? normalizedX >= 1f - image.fillAmount : normalizedX <= image.fillAmount;
        }

        static bool IsVerticalFillVisible(Image image, float normalizedY)
        {
            bool fillFromTop = image.fillOrigin == (int)Image.OriginVertical.Top;
            return fillFromTop ? normalizedY >= 1f - image.fillAmount : normalizedY <= image.fillAmount;
        }

        static bool IsRadialFillVisible(Image image, float normalizedX, float normalizedY, float totalDegrees)
        {
            Vector2 fromCenter = new(normalizedX - 0.5f, normalizedY - 0.5f);
            if (fromCenter.sqrMagnitude <= 0.000001f)
            {
                return true;
            }

            float angle = Mathf.Repeat(Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg + 360f, 360f);
            float origin = RadialOriginDegrees(image, totalDegrees);
            float delta = image.fillClockwise
                ? Mathf.Repeat(origin - angle + 360f, 360f)
                : Mathf.Repeat(angle - origin + 360f, 360f);
            return delta <= totalDegrees * image.fillAmount;
        }

        static float RadialOriginDegrees(Image image, float totalDegrees)
        {
            return totalDegrees switch
            {
                90f => image.fillOrigin switch
                {
                    (int)Image.Origin90.TopLeft => 180f,
                    (int)Image.Origin90.TopRight => 90f,
                    (int)Image.Origin90.BottomRight => 0f,
                    _ => 270f
                },
                180f => image.fillOrigin switch
                {
                    (int)Image.Origin180.Left => 180f,
                    (int)Image.Origin180.Right => 0f,
                    (int)Image.Origin180.Top => 90f,
                    _ => 270f
                },
                _ => image.fillOrigin switch
                {
                    (int)Image.Origin360.Right => 0f,
                    (int)Image.Origin360.Top => 90f,
                    (int)Image.Origin360.Left => 180f,
                    _ => 270f
                }
            };
        }

        static void DestroyGeneratedObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        void ReleaseGeneratedTexture()
        {
            if (_shadowTexture == null)
            {
                return;
            }

            Texture2D texture = _shadowTexture;
            _shadowTexture = null;
            DestroyGeneratedObject(texture);
        }

        void ReleaseRenderer()
        {
            GameObject shadowObject = _shadowObject != null ? _shadowObject : _shadowImage != null ? _shadowImage.gameObject : null;
            _shadowObject = null;
            _shadowImage = null;
            _shadowRect = null;
            _shadowLayoutElement = null;

            if (shadowObject == null)
            {
                return;
            }

            DestroyGeneratedObject(shadowObject);
        }

        readonly struct ShadowBuildRequest
        {
            public ShadowBuildRequest(
                LoogaUIShadowMode mode,
                Color color,
                Vector2 offset,
                float softness,
                float spread,
                float resolutionScale,
                int blurPasses,
                int width,
                int height,
                float[] sourceAlpha,
                bool clipSource)
            {
                Mode = mode;
                Color = color;
                Offset = offset;
                Softness = softness;
                Spread = spread;
                ResolutionScale = resolutionScale;
                BlurPasses = blurPasses;
                Width = width;
                Height = height;
                SourceAlpha = sourceAlpha;
                ClipSource = clipSource;
            }

            public readonly LoogaUIShadowMode Mode;
            public readonly Color Color;
            public readonly Vector2 Offset;
            public readonly float Softness;
            public readonly float Spread;
            public readonly float ResolutionScale;
            public readonly int BlurPasses;
            public readonly int Width;
            public readonly int Height;
            public readonly float[] SourceAlpha;
            public readonly bool ClipSource;
        }
    }
}
