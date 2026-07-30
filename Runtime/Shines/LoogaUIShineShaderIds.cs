using UnityEngine;

namespace LoogaSoft.UI.Extensions
{
    static class LoogaUIShineShaderIds
    {
        public static readonly int Enabled = Shader.PropertyToID("_LoogaShineEnabled");
        public static readonly int Color = Shader.PropertyToID("_LoogaShineColor");
        public static readonly int Direction = Shader.PropertyToID("_LoogaShineDirection");
        public static readonly int Width = Shader.PropertyToID("_LoogaShineWidth");
        public static readonly int Softness = Shader.PropertyToID("_LoogaShineSoftness");
        public static readonly int Position = Shader.PropertyToID("_LoogaShinePosition");
        public static readonly int Rect = Shader.PropertyToID("_LoogaUIRect");
    }
}
