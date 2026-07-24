using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("LoogaSoft/UI FX/Looga UI Gradient Overlay")]
    public sealed class LoogaUIGradientOverlay : UIBehaviour, IMaterialModifier
    {
        [Header("Gradient")]
        [SerializeField, Tooltip("Color used at the start of the gradient direction.")]
        Color _startColor = new(1f, 1f, 1f, 0f);

        [SerializeField, Tooltip("Color used at the end of the gradient direction.")]
        Color _endColor = new(1f, 1f, 1f, 0.35f);

        [SerializeField, Range(0f, 360f), Tooltip("Gradient direction in degrees.")]
        float _angle = 90f;

        [SerializeField, Range(0f, 1f), Tooltip("Overall strength of the gradient overlay.")]
        float _intensity = 1f;

        Material _replacement;
        Material _original;
        Graphic _graphic;

        Graphic Graphic => _graphic != null ? _graphic : (_graphic = GetComponent<Graphic>());

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || _intensity <= 0f)
            {
                ReleaseReplacement();
                return baseMaterial;
            }

            if (_replacement == null || _original != baseMaterial)
            {
                ReleaseReplacement();
                _original = baseMaterial;
                Shader shader = Shader.Find("Hidden/LoogaSoft/UI FX/Styled UI");
                if (shader == null)
                {
                    Debug.LogWarning("Looga UI FX styled UI shader could not be found.", this);
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

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif

        void ApplyMaterialParameters(Material material)
        {
            float radians = _angle * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(radians), Mathf.Sin(radians));

            material.SetFloat(LoogaUIGradientOverlayShaderIds.Enabled, 1f);
            material.SetColor(LoogaUIGradientOverlayShaderIds.StartColor, _startColor);
            material.SetColor(LoogaUIGradientOverlayShaderIds.EndColor, _endColor);
            material.SetVector(LoogaUIGradientOverlayShaderIds.Direction, direction);
            material.SetFloat(LoogaUIGradientOverlayShaderIds.Intensity, _intensity);
            material.SetVector(LoogaUIGradientOverlayShaderIds.Rect, ToVector(Graphic.rectTransform.rect));
        }

        void SetDirty()
        {
            if (Graphic == null)
            {
                return;
            }

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

        static Vector4 ToVector(Rect rect)
        {
            return new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
        }
    }
}
