using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("LoogaSoft/UI FX/Looga UI Outline")]
    public sealed class LoogaUIOutline : BaseMeshEffect, IMaterialModifier
    {
        static readonly List<UIVertex> VertexBuffer = new();

        [Header("Outline")]
        [SerializeField, Tooltip("Color and opacity of the generated outline.")]
        Color _color = Color.white;

        [SerializeField, Min(0f), Tooltip("Outline thickness in local UI units. For normal overlay UI this roughly matches pixels.")]
        float _thickness = 2f;

        [SerializeField, Range(0f, 1f), Tooltip("Softens the edge of the outline. Zero is crisp, one is the softest edge.")]
        float _softness = 0.25f;

        [SerializeField, Tooltip("How many directions the shader samples around the source alpha.")]
        LoogaUIOutlineQuality _quality = LoogaUIOutlineQuality.EightDirection;

        [SerializeField, Tooltip("Expands the graphic mesh so the outline can render outside the original rect instead of being clipped.")]
        bool _expandMesh = true;

        [SerializeField, Tooltip("Draw the original graphic over the outline. Disable this for outline-only effects.")]
        bool _drawSource = true;

        Material _replacement;
        Material _original;
        Graphic _graphic;
        Rect _lastRect;
        Vector4 _lastUvRect;

        Graphic Graphic => _graphic != null ? _graphic : (_graphic = GetComponent<Graphic>());

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || _thickness <= 0f)
            {
                ReleaseReplacement();
                return baseMaterial;
            }

            if (_replacement == null || _original != baseMaterial)
            {
                ReleaseReplacement();
                _original = baseMaterial;
                Shader shader = Shader.Find("Hidden/LoogaSoft/UI FX/Outlined UI");
                if (shader == null)
                {
                    Debug.LogWarning("Looga UI FX outline shader could not be found.", this);
                    return baseMaterial;
                }

                _replacement = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            CopyOriginalProperties(baseMaterial, _replacement);
            ApplyMaterialParameters(_replacement);
            return _replacement;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || !_expandMesh || _thickness <= 0f || Graphic == null)
            {
                return;
            }

            Rect rect = Graphic.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            VertexBuffer.Clear();
            vh.GetUIVertexStream(VertexBuffer);
            if (VertexBuffer.Count == 0)
            {
                return;
            }

            Vector2 center = rect.center;
            Vector4 uvRect = CalculateUvRect(VertexBuffer);
            Vector2 uvSize = new(uvRect.z - uvRect.x, uvRect.w - uvRect.y);
            Vector2 uvPadding = new(
                rect.width > 0f ? _thickness / rect.width * uvSize.x : 0f,
                rect.height > 0f ? _thickness / rect.height * uvSize.y : 0f);

            for (int i = 0; i < VertexBuffer.Count; i++)
            {
                UIVertex vertex = VertexBuffer[i];
                Vector3 position = vertex.position;
                Vector2 direction = new(
                    position.x < center.x ? -1f : position.x > center.x ? 1f : 0f,
                    position.y < center.y ? -1f : position.y > center.y ? 1f : 0f);

                position.x += direction.x * _thickness;
                position.y += direction.y * _thickness;
                vertex.position = position;
                vertex.uv0 += new Vector4(direction.x * uvPadding.x, direction.y * uvPadding.y, 0f, 0f);
                VertexBuffer[i] = vertex;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(VertexBuffer);
            _lastRect = rect;
            _lastUvRect = uvRect;

            if (_replacement != null)
            {
                ApplyMaterialParameters(_replacement);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            ReleaseReplacement();
            base.OnDisable();
            SetDirty();
        }

        protected override void OnDestroy()
        {
            ReleaseReplacement();
            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _thickness = Mathf.Max(0f, _thickness);
            SetDirty();
        }
#endif

        void ApplyMaterialParameters(Material material)
        {
            Rect rect = Graphic != null ? Graphic.rectTransform.rect : _lastRect;
            Vector4 uvRect = _lastUvRect;
            if (uvRect == default)
            {
                uvRect = new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2 uvSize = new(uvRect.z - uvRect.x, uvRect.w - uvRect.y);
            Vector2 uvThickness = new(
                rect.width > 0f ? _thickness / rect.width * uvSize.x : 0f,
                rect.height > 0f ? _thickness / rect.height * uvSize.y : 0f);

            material.SetColor(LoogaUIOutlineShaderIds.Color, _color);
            material.SetVector(LoogaUIOutlineShaderIds.UvRect, uvRect);
            material.SetVector(LoogaUIOutlineShaderIds.Thickness, uvThickness);
            material.SetFloat(LoogaUIOutlineShaderIds.Softness, _softness);
            material.SetFloat(LoogaUIOutlineShaderIds.Quality, _quality == LoogaUIOutlineQuality.EightDirection ? 1f : 0f);
            material.SetFloat(LoogaUIOutlineShaderIds.DrawSource, _drawSource ? 1f : 0f);
        }

        void SetDirty()
        {
            if (Graphic == null)
            {
                return;
            }

            Graphic.SetVerticesDirty();
            Graphic.SetMaterialDirty();
        }

        void ReleaseReplacement()
        {
            if (_replacement == null)
            {
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

        static Vector4 CalculateUvRect(List<UIVertex> vertices)
        {
            Vector4 firstUv = vertices[0].uv0;
            Vector2 min = new(firstUv.x, firstUv.y);
            Vector2 max = min;

            for (int i = 1; i < vertices.Count; i++)
            {
                Vector4 vertexUv = vertices[i].uv0;
                Vector2 uv = new(vertexUv.x, vertexUv.y);
                min = Vector2.Min(min, uv);
                max = Vector2.Max(max, uv);
            }

            return new Vector4(min.x, min.y, max.x, max.y);
        }
    }
}


