Shader "Hidden/LoogaSoft/UI/Soft Masked UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LoogaSoftMaskCount ("Soft Mask Count", Float) = 1
        [PerRendererData] _LoogaSoftMaskTex ("Soft Mask", 2D) = "white" {}
        _LoogaSoftMaskRect ("Soft Mask Rect", Vector) = (0,0,1,1)
        _LoogaSoftMaskUVRect ("Soft Mask UV Rect", Vector) = (0,0,1,1)
        _LoogaSoftMaskChannelWeights ("Soft Mask Channel Weights", Color) = (0,0,0,1)
        _LoogaSoftMaskInvert ("Soft Mask Invert", Float) = 0
        _LoogaSoftMaskInvertOutside ("Soft Mask Invert Outside", Float) = 0
        [PerRendererData] _LoogaSoftMaskTex0 ("Soft Mask 0", 2D) = "white" {}
        [PerRendererData] _LoogaSoftMaskTex1 ("Soft Mask 1", 2D) = "white" {}
        [PerRendererData] _LoogaSoftMaskTex2 ("Soft Mask 2", 2D) = "white" {}
        [PerRendererData] _LoogaSoftMaskTex3 ("Soft Mask 3", 2D) = "white" {}
        _LoogaSoftMaskRect0 ("Soft Mask Rect 0", Vector) = (0,0,1,1)
        _LoogaSoftMaskRect1 ("Soft Mask Rect 1", Vector) = (0,0,1,1)
        _LoogaSoftMaskRect2 ("Soft Mask Rect 2", Vector) = (0,0,1,1)
        _LoogaSoftMaskRect3 ("Soft Mask Rect 3", Vector) = (0,0,1,1)
        _LoogaSoftMaskUVRect0 ("Soft Mask UV Rect 0", Vector) = (0,0,1,1)
        _LoogaSoftMaskUVRect1 ("Soft Mask UV Rect 1", Vector) = (0,0,1,1)
        _LoogaSoftMaskUVRect2 ("Soft Mask UV Rect 2", Vector) = (0,0,1,1)
        _LoogaSoftMaskUVRect3 ("Soft Mask UV Rect 3", Vector) = (0,0,1,1)
        _LoogaSoftMaskChannelWeights0 ("Soft Mask Channel Weights 0", Color) = (0,0,0,1)
        _LoogaSoftMaskChannelWeights1 ("Soft Mask Channel Weights 1", Color) = (0,0,0,1)
        _LoogaSoftMaskChannelWeights2 ("Soft Mask Channel Weights 2", Color) = (0,0,0,1)
        _LoogaSoftMaskChannelWeights3 ("Soft Mask Channel Weights 3", Color) = (0,0,0,1)
        _LoogaSoftMaskInvert0 ("Soft Mask Invert 0", Float) = 0
        _LoogaSoftMaskInvert1 ("Soft Mask Invert 1", Float) = 0
        _LoogaSoftMaskInvert2 ("Soft Mask Invert 2", Float) = 0
        _LoogaSoftMaskInvert3 ("Soft Mask Invert 3", Float) = 0
        _LoogaSoftMaskInvertOutside0 ("Soft Mask Invert Outside 0", Float) = 0
        _LoogaSoftMaskInvertOutside1 ("Soft Mask Invert Outside 1", Float) = 0
        _LoogaSoftMaskInvertOutside2 ("Soft Mask Invert Outside 2", Float) = 0
        _LoogaSoftMaskInvertOutside3 ("Soft Mask Invert Outside 3", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 maskPosition0 : TEXCOORD2;
                float4 maskPosition1 : TEXCOORD3;
                float4 maskPosition2 : TEXCOORD4;
                float4 maskPosition3 : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            sampler2D _LoogaSoftMaskTex;
            float4 _LoogaSoftMaskRect;
            float4 _LoogaSoftMaskUVRect;
            float4x4 _LoogaSoftMaskWorldToMask;
            float4 _LoogaSoftMaskChannelWeights;
            float _LoogaSoftMaskInvert;
            float _LoogaSoftMaskInvertOutside;

            float _LoogaSoftMaskCount;
            sampler2D _LoogaSoftMaskTex0;
            sampler2D _LoogaSoftMaskTex1;
            sampler2D _LoogaSoftMaskTex2;
            sampler2D _LoogaSoftMaskTex3;
            float4 _LoogaSoftMaskRect0;
            float4 _LoogaSoftMaskRect1;
            float4 _LoogaSoftMaskRect2;
            float4 _LoogaSoftMaskRect3;
            float4 _LoogaSoftMaskUVRect0;
            float4 _LoogaSoftMaskUVRect1;
            float4 _LoogaSoftMaskUVRect2;
            float4 _LoogaSoftMaskUVRect3;
            float4x4 _LoogaSoftMaskWorldToMask0;
            float4x4 _LoogaSoftMaskWorldToMask1;
            float4x4 _LoogaSoftMaskWorldToMask2;
            float4x4 _LoogaSoftMaskWorldToMask3;
            float4 _LoogaSoftMaskChannelWeights0;
            float4 _LoogaSoftMaskChannelWeights1;
            float4 _LoogaSoftMaskChannelWeights2;
            float4 _LoogaSoftMaskChannelWeights3;
            float _LoogaSoftMaskInvert0;
            float _LoogaSoftMaskInvert1;
            float _LoogaSoftMaskInvert2;
            float _LoogaSoftMaskInvert3;
            float _LoogaSoftMaskInvertOutside0;
            float _LoogaSoftMaskInvertOutside1;
            float _LoogaSoftMaskInvertOutside2;
            float _LoogaSoftMaskInvertOutside3;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                OUT.maskPosition0 = mul(_LoogaSoftMaskWorldToMask0, v.vertex);
                OUT.maskPosition1 = mul(_LoogaSoftMaskWorldToMask1, v.vertex);
                OUT.maskPosition2 = mul(_LoogaSoftMaskWorldToMask2, v.vertex);
                OUT.maskPosition3 = mul(_LoogaSoftMaskWorldToMask3, v.vertex);
                return OUT;
            }

            float2 MaskUV(float2 maskPosition, float4 rect, float4 uvRect)
            {
                float2 normalized = (maskPosition - rect.xy) / max(rect.zw - rect.xy, 0.0001);
                return lerp(uvRect.xy, uvRect.zw, normalized);
            }

            fixed MaskValue(float2 maskPosition, sampler2D maskTexture, float4 rect, float4 uvRect, float4 channelWeights, float invert, float invertOutside)
            {
                float isInside = UnityGet2DClipping(maskPosition, rect);
                fixed4 sampledMask = tex2D(maskTexture, MaskUV(maskPosition, rect, uvRect));
                fixed weightedMask = dot(sampledMask * channelWeights, fixed4(1, 1, 1, 1));
                fixed inside = lerp(weightedMask, 1 - weightedMask, invert);
                return lerp(invertOutside, inside, isInside);
            }

            fixed CombinedMask(v2f IN)
            {
                fixed mask = 1;

                if (_LoogaSoftMaskCount > 0.5)
                {
                    mask *= MaskValue(IN.maskPosition0.xy, _LoogaSoftMaskTex0, _LoogaSoftMaskRect0, _LoogaSoftMaskUVRect0, _LoogaSoftMaskChannelWeights0, _LoogaSoftMaskInvert0, _LoogaSoftMaskInvertOutside0);
                }

                if (_LoogaSoftMaskCount > 1.5)
                {
                    mask *= MaskValue(IN.maskPosition1.xy, _LoogaSoftMaskTex1, _LoogaSoftMaskRect1, _LoogaSoftMaskUVRect1, _LoogaSoftMaskChannelWeights1, _LoogaSoftMaskInvert1, _LoogaSoftMaskInvertOutside1);
                }

                if (_LoogaSoftMaskCount > 2.5)
                {
                    mask *= MaskValue(IN.maskPosition2.xy, _LoogaSoftMaskTex2, _LoogaSoftMaskRect2, _LoogaSoftMaskUVRect2, _LoogaSoftMaskChannelWeights2, _LoogaSoftMaskInvert2, _LoogaSoftMaskInvertOutside2);
                }

                if (_LoogaSoftMaskCount > 3.5)
                {
                    mask *= MaskValue(IN.maskPosition3.xy, _LoogaSoftMaskTex3, _LoogaSoftMaskRect3, _LoogaSoftMaskUVRect3, _LoogaSoftMaskChannelWeights3, _LoogaSoftMaskInvert3, _LoogaSoftMaskInvertOutside3);
                }

                return mask;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                color.a *= CombinedMask(IN);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
