// 过渡面板的流光背景：一张 Image 靠改 UV 偏移无限滚动。
// 为什么不用 Mobile/Particles/Additive：
//   工程是 URP，内置管线的 Mobile/Particles/* 在 URP 下会走 InternalErrorShader（洋红），
//   所以这里用 URP Core.hlsl 写一个等价物 —— Additive 混合（SrcAlpha One）+ 顶点里做 TRANSFORM_TEX，
//   这样 material.mainTextureOffset 才有作用（UI/Default 不做 UV 变换，改 offset 看不出效果）。
// 用法：Image.material 换成引用本 Shader 的材质，脚本里 mainTextureOffset += speed * dt。
Shader "UnderWord/UI/ScrollAdditive"
{
    Properties
    {
        [PerRendererData] _MainTex ("贴图", 2D) = "white" {}
        _Color ("染色", Color) = (1, 1, 1, 1)

        // 以下 5 个是 UI 被 Mask 裁切时需要的模板参数，缺了会被遮罩裁不到
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
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
        ZWrite Off
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "ScrollAdditive"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // UI/Default 不做这一步，所以那边改 mainTextureOffset 是没效果的
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
