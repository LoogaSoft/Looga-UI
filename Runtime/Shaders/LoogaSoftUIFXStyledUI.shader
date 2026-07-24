Shader "Hidden/LoogaSoft/UI FX/Styled UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LoogaGradientEnabled ("Gradient Enabled", Float) = 0
        _LoogaGradientStartColor ("Gradient Start Color", Color) = (1,1,1,0)
        _LoogaGradientEndColor ("Gradient End Color", Color) = (1,1,1,0)
        _LoogaGradientDirection ("Gradient Direction", Vector) = (0,1,0,0)
        _LoogaGradientIntensity ("Gradient Intensity", Float) = 1
        _LoogaShineEnabled ("Shine Enabled", Float) = 0
        _LoogaShineColor ("Shine Color", Color) = (1,1,1,0)
        _LoogaShineDirection ("Shine Direction", Vector) = (1,0,0,0)
        _LoogaShineWidth ("Shine Width", Float) = 0.18
        _LoogaShineSoftness ("Shine Softness", Float) = 0.45
        _LoogaShinePosition ("Shine Position", Float) = -0.5
        _LoogaUIRect ("UI Rect", Vector) = (0,0,1,1)
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
                float2 localPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _LoogaGradientEnabled;
            fixed4 _LoogaGradientStartColor;
            fixed4 _LoogaGradientEndColor;
            float4 _LoogaGradientDirection;
            float _LoogaGradientIntensity;
            float _LoogaShineEnabled;
            fixed4 _LoogaShineColor;
            float4 _LoogaShineDirection;
            float _LoogaShineWidth;
            float _LoogaShineSoftness;
            float _LoogaShinePosition;
            float4 _LoogaUIRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                OUT.localPosition = v.vertex.xy;
                return OUT;
            }

            float2 Local01(float2 localPosition)
            {
                return saturate((localPosition - _LoogaUIRect.xy) / max(_LoogaUIRect.zw - _LoogaUIRect.xy, 0.0001));
            }

            fixed4 ApplyGradient(fixed4 source, float2 local01)
            {
                if (_LoogaGradientEnabled < 0.5 || _LoogaGradientIntensity <= 0)
                {
                    return source;
                }

                float2 direction = normalize(_LoogaGradientDirection.xy);
                float gradientPosition = saturate(dot(local01 - 0.5, direction) + 0.5);
                fixed4 gradient = lerp(_LoogaGradientStartColor, _LoogaGradientEndColor, gradientPosition);
                fixed overlayAlpha = saturate(gradient.a * _LoogaGradientIntensity) * source.a;
                source.rgb = lerp(source.rgb, gradient.rgb, overlayAlpha);
                return source;
            }

            fixed4 ApplyShine(fixed4 source, float2 local01)
            {
                if (_LoogaShineEnabled < 0.5 || _LoogaShineColor.a <= 0)
                {
                    return source;
                }

                float2 direction = normalize(_LoogaShineDirection.xy);
                float bandPosition = dot(local01 - 0.5, direction) + 0.5;
                float halfWidth = max(_LoogaShineWidth * 0.5, 0.0001);
                float softness = max(_LoogaShineSoftness * _LoogaShineWidth, 0.0001);
                float distanceToBand = abs(bandPosition - _LoogaShinePosition);
                fixed band = 1 - smoothstep(halfWidth, halfWidth + softness, distanceToBand);
                fixed shineAlpha = band * _LoogaShineColor.a * source.a;
                source.rgb += _LoogaShineColor.rgb * shineAlpha;
                return source;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 result = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                float2 local01 = Local01(IN.localPosition);
                result = ApplyGradient(result, local01);
                result = ApplyShine(result, local01);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
