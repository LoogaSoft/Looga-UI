using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("LoogaSoft/UI FX/Looga UI Shine")]
    public sealed class LoogaUIShine : UIBehaviour, IMaterialModifier
    {
        const float StartPosition = -0.5f;
        const float EndPosition = 1.5f;

        [Header("Shine")]
        [SerializeField, Tooltip("Color and opacity of the shine band.")]
        Color _color = new(1f, 1f, 1f, 0.45f);

        [SerializeField, Range(0f, 360f), Tooltip("Direction of the shine band in degrees.")]
        float _angle = 25f;

        [SerializeField, Range(0.01f, 1f), Tooltip("Width of the shine band across the graphic.")]
        float _width = 0.18f;

        [SerializeField, Range(0f, 1f), Tooltip("Softens the edges of the shine band.")]
        float _softness = 0.45f;

        [SerializeField, Range(StartPosition, EndPosition), Tooltip("Manual shine position. Useful for edit-mode preview or scripted control.")]
        float _position = StartPosition;

        [Header("Playback")]
        [SerializeField, Tooltip("Automatically play the shine sweep when the component is enabled.")]
        bool _playOnEnable = true;

        [SerializeField, Tooltip("Loop the shine sweep while the component remains enabled.")]
        bool _loop = true;

        [SerializeField, Min(0.01f), Tooltip("Seconds for one complete shine sweep.")]
        float _duration = 1.25f;

        [SerializeField, Min(0f), Tooltip("Seconds to wait before each shine sweep starts.")]
        float _delay = 0f;

        [SerializeField, Tooltip("Use unscaled time so UI shine animation keeps moving while game time is paused.")]
        bool _useUnscaledTime = true;

        Material _replacement;
        Material _original;
        Graphic _graphic;
        float _elapsed;
        bool _playing;

        Graphic Graphic => _graphic != null ? _graphic : (_graphic = GetComponent<Graphic>());

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || _color.a <= 0f)
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

        public void Play()
        {
            _elapsed = 0f;
            _playing = true;
            SetPosition(StartPosition);
        }

        public void Stop()
        {
            _playing = false;
            SetPosition(StartPosition);
        }

        public void SetPosition(float position)
        {
            _position = Mathf.Clamp(position, StartPosition, EndPosition);
            if (_replacement != null)
            {
                ApplyMaterialParameters(_replacement);
            }

            // The shine can be one link in Unity's material modifier chain. Marking the
            // graphic dirty keeps animated position changes correct regardless of order.
            SetDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isPlaying && _playOnEnable)
            {
                Play();
            }

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

        void Update()
        {
            if (!Application.isPlaying || !_playing)
            {
                return;
            }

            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _elapsed += deltaTime;

            if (_elapsed < _delay)
            {
                SetPosition(StartPosition);
                return;
            }

            float sweepTime = _elapsed - _delay;
            float normalized = Mathf.Clamp01(sweepTime / Mathf.Max(0.01f, _duration));
            SetPosition(Mathf.Lerp(StartPosition, EndPosition, normalized));

            if (normalized < 1f)
            {
                return;
            }

            if (_loop)
            {
                _elapsed = 0f;
                SetPosition(StartPosition);
            }
            else
            {
                _playing = false;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _duration = Mathf.Max(0.01f, _duration);
            _width = Mathf.Max(0.01f, _width);
            SetDirty();
        }
#endif

        void ApplyMaterialParameters(Material material)
        {
            float radians = _angle * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(radians), Mathf.Sin(radians));

            material.SetFloat(LoogaUIShineShaderIds.Enabled, 1f);
            material.SetColor(LoogaUIShineShaderIds.Color, _color);
            material.SetVector(LoogaUIShineShaderIds.Direction, direction);
            material.SetFloat(LoogaUIShineShaderIds.Width, _width);
            material.SetFloat(LoogaUIShineShaderIds.Softness, _softness);
            material.SetFloat(LoogaUIShineShaderIds.Position, _position);
            material.SetVector(LoogaUIShineShaderIds.Rect, ToVector(Graphic.rectTransform.rect));
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
