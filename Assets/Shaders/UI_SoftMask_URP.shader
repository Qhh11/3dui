Shader "UI/Soft Mask URP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Toggle] _ClampHoriz ("Clamp Alpha Horizontally", Float) = 0
        [Toggle] _ClampVert ("Clamp Alpha Vertically", Float) = 0
        [Toggle] _UseAlphaChannel ("Use Mask Alpha Channel", Float) = 0
        _MaskRotation ("Mask Rotation in Radians", Float) = 0
        _AlphaTex ("Alpha Mask", 2D) = "white" {}
        _ClampBorder ("Clamping Border", Float) = 0.01
        [KeywordEnum(X, Y, Z)] _Axis ("Alpha Mapping Axis", Float) = 2
        _IsThisText ("Is This Text?", Float) = 0

        [Toggle] _ScreenSpaceUI ("Screen Space UI", Float) = 0

        // UGUI Mask / Stencil support
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)

        [Toggle(UNITY_UI_CLIP_RECT)] _UseUIClipRect ("Use RectMask2D", Float) = 0
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
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
        ZTest Always
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _CLAMPHORIZ_ON
            #pragma shader_feature_local _CLAMPVERT_ON
            #pragma shader_feature_local _USEALPHACHANNEL_ON
            #pragma shader_feature_local _AXIS_X _AXIS_Y _AXIS_Z
            #pragma shader_feature_local _SCREENSPACEUI_ON

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
                float4 _AlphaTex_ST;
                float4 _ClipRect;
                float _ClampBorder;
                float _MaskRotation;
                float _IsThisText;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uvMain : TEXCOORD0;
                float2 uvAlpha : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
            };

            float Get2DClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
                return inside.x * inside.y;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs posInput = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = posInput.positionCS;
                o.worldPosition = mul(GetObjectToWorldMatrix(), v.positionOS);

                o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;

                float2 alphaUV = float2(0, 0);

                #if defined(_SCREENSPACEUI_ON)
                    #if defined(_AXIS_X)
                        alphaUV = v.positionOS.zy;
                    #elif defined(_AXIS_Y)
                        alphaUV = v.positionOS.xz;
                    #else
                        alphaUV = v.positionOS.xy;
                    #endif
                #else
                    #if defined(_AXIS_X)
                        alphaUV = o.worldPosition.zy;
                    #elif defined(_AXIS_Y)
                        alphaUV = o.worldPosition.xz;
                    #else
                        alphaUV = o.worldPosition.xy;
                    #endif
                #endif

                float s = sin(_MaskRotation);
                float c = cos(_MaskRotation);
                float2x2 rot = float2x2(c, -s, s, c);
                alphaUV = mul(alphaUV, rot);

                o.uvAlpha = alphaUV * _AlphaTex_ST.xy + _AlphaTex_ST.zw;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 alphaCoords = i.uvAlpha;

                #if defined(_CLAMPHORIZ_ON)
                    alphaCoords.x = clamp(alphaCoords.x, _ClampBorder, 1.0 - _ClampBorder);
                #endif

                #if defined(_CLAMPVERT_ON)
                    alphaCoords.y = clamp(alphaCoords.y, _ClampBorder, 1.0 - _ClampBorder);
                #endif

                half4 texcol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvMain);
                texcol.a *= i.color.a;
                texcol.rgb = saturate(texcol.rgb + _IsThisText) * i.color.rgb;

                half4 mask = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, alphaCoords);

                #if defined(_USEALPHACHANNEL_ON)
                    texcol.a *= mask.a;
                #else
                    texcol.a *= mask.r;
                #endif

                #if defined(UNITY_UI_CLIP_RECT)
                    texcol.a *= Get2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #if defined(UNITY_UI_ALPHACLIP)
                    clip(texcol.a - 0.001);
                #endif

                texcol.rgb *= texcol.a;
                return texcol;
            }

            ENDHLSL
        }
    }
}