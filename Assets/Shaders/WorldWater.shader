Shader "Custom/WorldWater"
{
    Properties
    {
        _BaseColor ("Water Color", Color) = (0.2, 0.5, 1, 0.5)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.8
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 3.0

        [Header(Normals)]
        [Normal]_NormalA ("Normal A", 2D) = "bump" {}
        [Normal]_NormalB ("Normal B", 2D) = "bump" {}
        _NormalSpeed ("Normal Speed", Vector) = (0.05, 0.03, -0.04, 0.02)
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1.0

        [Header(Foam)]
        _FoamTex ("Foam Texture", 2D) = "white" {}
        _FoamSpeed ("Foam Speed", Float) = 0.05
        _FoamScale ("Foam Scale", Float) = 0.5
        _FoamIntensity ("Foam Intensity", Range(0, 2)) = 1.0

        [Header(Intersection)]
        _IntersectionColor ("Intersection Color", Color) = (1, 1, 1, 1)
        _IntersectionHeight ("Intersection Height", Float) = 0.2
        _IntersectionFalloff ("Intersection Falloff", Float) = 0.5

        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (0.15, 0.2, 0.3, 1)
        _FogDistance ("Fog Distance", Float) = 50.0

        [Header(Waves)]
        _WaveStrength ("Wave Strength", Float) = 0.1
        _WaveScale ("Wave Scale", Float) = 0.2
        _WaveSpeed ("Wave Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
        #pragma target 3.0

        sampler2D _NormalA;
        sampler2D _NormalB;
        sampler2D _FoamTex;

        float4 _NormalSpeed;
        float _NormalStrength;

        float4 _BaseColor;
        float4 _FoamColor;
        float _Glossiness;
        float _Metallic;
        float _FresnelPower;

        float _FoamSpeed;
        float _FoamScale;
        float _FoamIntensity;

        float4 _IntersectionColor;
        float _IntersectionHeight;
        float _IntersectionFalloff;

        float4 _FogColor;
        float _FogDistance;

        float _WaveStrength;
        float _WaveScale;
        float _WaveSpeed;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        void vert(inout appdata_full v)
        {
            // Apply simple sine-based vertex wave animation
            float wave =
                sin(v.vertex.x * _WaveScale + _Time.y * _WaveSpeed) +
                cos(v.vertex.z * _WaveScale + _Time.y * _WaveSpeed);
            v.vertex.y += wave * _WaveStrength;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uvA = IN.worldPos.xz * 0.05 + _Time.y * _NormalSpeed.xy;
            float2 uvB = IN.worldPos.xz * 0.05 + _Time.y * _NormalSpeed.zw;

            float3 normalA = UnpackNormal(tex2D(_NormalA, uvA));
            float3 normalB = UnpackNormal(tex2D(_NormalB, uvB));
            o.Normal = normalize((normalA + normalB) * 0.5) * _NormalStrength;

            float fresnel = pow(1.0 - saturate(dot(IN.viewDir, o.Normal)), _FresnelPower);

            // Foam
            float2 foamUV = IN.worldPos.xz * _FoamScale + _Time.y * _FoamSpeed;
            float foamMask = tex2D(_FoamTex, foamUV).r;

            // Intersection glow by world height
            float intersectionMask = saturate((IN.worldPos.y - _IntersectionHeight) / _IntersectionFalloff);

            // Fog by camera distance
            float fogFactor = saturate(distance(_WorldSpaceCameraPos, IN.worldPos) / _FogDistance);

            // Blend base, foam, and intersection
            float3 baseFoam = lerp(_BaseColor.rgb, _FoamColor.rgb, foamMask * _FoamIntensity);
            float3 withIntersection = lerp(_IntersectionColor.rgb, baseFoam, intersectionMask);
            float3 finalColor = lerp(withIntersection, _FogColor.rgb, fogFactor);

            o.Albedo = finalColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _BaseColor.a * fresnel + (foamMask * _FoamColor.a);
            o.Emission = foamMask * _FoamIntensity;
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
