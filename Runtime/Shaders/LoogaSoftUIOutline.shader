Shader "Hidden/LoogaSoft/UI/Outlined UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LoogaOutlineColor ("Outline Color", Color) = (1,1,1,1)
        _LoogaOutlineUvRect ("Outline UV Rect", Vector) = (0,0,1,1)
        _LoogaOutlineThickness ("Outline Thickness", Vector) = (0.01,0.01,0,0)
        _LoogaOutlineSoftness ("Outline Softness", Float) = 0.25
        _LoogaOutlineQuality ("Outline Quality", Float) = 1
        _LoogaOutlineDrawSource ("Draw Source", Float) = 1
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _LoogaOutlineColor;
            float4 _LoogaOutlineUvRect;
            float4 _LoogaOutlineThickness;
            float _LoogaOutlineSoftness;
            float _LoogaOutlineQuality;
            float _LoogaOutlineDrawSource;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed AlphaAt(float2 uv)
            {
                float inside = step(_LoogaOutlineUvRect.x, uv.x)
                    * step(uv.x, _LoogaOutlineUvRect.z)
                    * step(_LoogaOutlineUvRect.y, uv.y)
                    * step(uv.y, _LoogaOutlineUvRect.w);
                return tex2D(_MainTex, uv).a * inside;
            }

            fixed NeighborAlpha(float2 uv, float2 thickness)
            {
                fixed alpha = 0;
                alpha = max(alpha, AlphaAt(uv + float2(thickness.x, 0)));
                alpha = max(alpha, AlphaAt(uv + float2(-thickness.x, 0)));
                alpha = max(alpha, AlphaAt(uv + float2(0, thickness.y)));
                alpha = max(alpha, AlphaAt(uv + float2(0, -thickness.y)));

                if (_LoogaOutlineQuality > 0.5)
                {
                    float2 diagonal = thickness * 0.70710678;
                    alpha = max(alpha, AlphaAt(uv + diagonal));
                    alpha = max(alpha, AlphaAt(uv - diagonal));
                    alpha = max(alpha, AlphaAt(uv + float2(diagonal.x, -diagonal.y)));
                    alpha = max(alpha, AlphaAt(uv + float2(-diagonal.x, diagonal.y)));
                }

                return alpha;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 source = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                fixed sourceAlpha = AlphaAt(IN.texcoord) * IN.color.a;
                fixed neighbor = NeighborAlpha(IN.texcoord, _LoogaOutlineThickness.xy);
                fixed outlineShape = saturate(neighbor - sourceAlpha);
                fixed softened = lerp(step(0.001, outlineShape), outlineShape, _LoogaOutlineSoftness);
                fixed outlineAlpha = softened * _LoogaOutlineColor.a;

                fixed4 outline = fixed4(_LoogaOutlineColor.rgb, outlineAlpha);
                fixed4 result = outline;

                if (_LoogaOutlineDrawSource > 0.5)
                {
                    result.rgb = lerp(outline.rgb, source.rgb, source.a);
                    result.a = max(outline.a, source.a);
                }

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

