// 相机遮挡淡出：把「相机与角色之间」挡视线的障碍物画成半透明。
// 只给 PlayerCameraRig 运行时的那份材质用，不参与美术资源。
// 说明：
//   1) 不走 URP 的 Lit，因为淡出材质需要「共享一份、靠 MaterialPropertyBlock 改透明度」，
//      自己算一份极简光照（主光半兰伯特 + 环境 SH），淡出期间画面不至于变平；
//   2) ZWrite Off + Transparent 队列：物体在半透明队列后渲染，能正确盖在角色上；
//   3) 不带 ShadowCaster，所以淡出物不会投出实心阴影（半透明物体投实心影会很怪）；
//   4) 两面子（Cull Mode）用 _Cull 控制，默认 Back，相机钻到墙里时会看到「穿墙」效果。
Shader "UnderWord/CameraObstacleFade"
{
    Properties
    {
        _BaseMap ("贴图", 2D) = "white" {}
        _BaseColor ("颜色 (A = 透明度)", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ObstacleFade"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.normalWS = normals.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();

                // 半兰伯特：背光面保留 40% 亮度，淡出时不出现死黑面
                half lambert = saturate(dot(normalWS, mainLight.direction)) * 0.6h + 0.4h;
                half3 ambient = SampleSH(normalWS);
                half3 color = baseColor.rgb * (mainLight.color * lambert + ambient);

                return half4(color, baseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
