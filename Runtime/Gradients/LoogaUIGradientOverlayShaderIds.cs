using UnityEngine;

namespace LoogaSoft.UIFX
{
    static class LoogaUIGradientOverlayShaderIds
    {
        public static readonly int Enabled = Shader.PropertyToID("_LoogaGradientEnabled");
        public static readonly int StartColor = Shader.PropertyToID("_LoogaGradientStartColor");
        public static readonly int EndColor = Shader.PropertyToID("_LoogaGradientEndColor");
        public static readonly int Direction = Shader.PropertyToID("_LoogaGradientDirection");
        public static readonly int Intensity = Shader.PropertyToID("_LoogaGradientIntensity");
        public static readonly int Rect = Shader.PropertyToID("_LoogaUIRect");
    }
}
