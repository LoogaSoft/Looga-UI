using UnityEngine;

namespace LoogaSoft.UI.Extensions
{
    static class LoogaUISoftMaskShaderIds
    {
        public const int MaxMaskCount = 4;

        public static readonly int MaskCount = Shader.PropertyToID("_LoogaSoftMaskCount");
        public static readonly int MaskTexture = Shader.PropertyToID("_LoogaSoftMaskTex");
        public static readonly int MaskRect = Shader.PropertyToID("_LoogaSoftMaskRect");
        public static readonly int MaskUvRect = Shader.PropertyToID("_LoogaSoftMaskUVRect");
        public static readonly int WorldToMask = Shader.PropertyToID("_LoogaSoftMaskWorldToMask");
        public static readonly int ChannelWeights = Shader.PropertyToID("_LoogaSoftMaskChannelWeights");
        public static readonly int Invert = Shader.PropertyToID("_LoogaSoftMaskInvert");
        public static readonly int InvertOutside = Shader.PropertyToID("_LoogaSoftMaskInvertOutside");
        public static readonly int[] MaskTextures =
        {
            Shader.PropertyToID("_LoogaSoftMaskTex0"),
            Shader.PropertyToID("_LoogaSoftMaskTex1"),
            Shader.PropertyToID("_LoogaSoftMaskTex2"),
            Shader.PropertyToID("_LoogaSoftMaskTex3")
        };

        public static readonly int[] MaskRects =
        {
            Shader.PropertyToID("_LoogaSoftMaskRect0"),
            Shader.PropertyToID("_LoogaSoftMaskRect1"),
            Shader.PropertyToID("_LoogaSoftMaskRect2"),
            Shader.PropertyToID("_LoogaSoftMaskRect3")
        };

        public static readonly int[] MaskUvRects =
        {
            Shader.PropertyToID("_LoogaSoftMaskUVRect0"),
            Shader.PropertyToID("_LoogaSoftMaskUVRect1"),
            Shader.PropertyToID("_LoogaSoftMaskUVRect2"),
            Shader.PropertyToID("_LoogaSoftMaskUVRect3")
        };

        public static readonly int[] WorldToMasks =
        {
            Shader.PropertyToID("_LoogaSoftMaskWorldToMask0"),
            Shader.PropertyToID("_LoogaSoftMaskWorldToMask1"),
            Shader.PropertyToID("_LoogaSoftMaskWorldToMask2"),
            Shader.PropertyToID("_LoogaSoftMaskWorldToMask3")
        };

        public static readonly int[] ChannelWeightsList =
        {
            Shader.PropertyToID("_LoogaSoftMaskChannelWeights0"),
            Shader.PropertyToID("_LoogaSoftMaskChannelWeights1"),
            Shader.PropertyToID("_LoogaSoftMaskChannelWeights2"),
            Shader.PropertyToID("_LoogaSoftMaskChannelWeights3")
        };

        public static readonly int[] Inverts =
        {
            Shader.PropertyToID("_LoogaSoftMaskInvert0"),
            Shader.PropertyToID("_LoogaSoftMaskInvert1"),
            Shader.PropertyToID("_LoogaSoftMaskInvert2"),
            Shader.PropertyToID("_LoogaSoftMaskInvert3")
        };

        public static readonly int[] InvertOutsides =
        {
            Shader.PropertyToID("_LoogaSoftMaskInvertOutside0"),
            Shader.PropertyToID("_LoogaSoftMaskInvertOutside1"),
            Shader.PropertyToID("_LoogaSoftMaskInvertOutside2"),
            Shader.PropertyToID("_LoogaSoftMaskInvertOutside3")
        };
    }
}
