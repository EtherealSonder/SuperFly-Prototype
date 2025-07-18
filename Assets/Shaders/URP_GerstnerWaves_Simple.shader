Shader "Custom/URP_GerstnerWaves_Simple"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (0.3, 0.6, 1, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;

                // Optional fake Gerstner displacement placeholder
                pos.y += 0.1 * sin(_Time.y + pos.x * 0.1);

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texColor * _BaseColor;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
