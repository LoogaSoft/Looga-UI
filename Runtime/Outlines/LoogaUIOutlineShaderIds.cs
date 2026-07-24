using UnityEngine;

namespace LoogaSoft.UIFX
{
    static class LoogaUIOutlineShaderIds
    {
        public static readonly int Color = Shader.PropertyToID("_LoogaOutlineColor");
        public static readonly int UvRect = Shader.PropertyToID("_LoogaOutlineUvRect");
        public static readonly int Thickness = Shader.PropertyToID("_LoogaOutlineThickness");
        public static readonly int Softness = Shader.PropertyToID("_LoogaOutlineSoftness");
        public static readonly int Quality = Shader.PropertyToID("_LoogaOutlineQuality");
        public static readonly int DrawSource = Shader.PropertyToID("_LoogaOutlineDrawSource");
    }
}
