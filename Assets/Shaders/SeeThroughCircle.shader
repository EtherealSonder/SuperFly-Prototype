Shader "Custom/SeeThroughCircle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _Size ("Circle Size", Float) = 0.1
        _Position ("Circle Center (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _tint ("Tint Strength", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 200
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"   // ✅ Needed for TRANSFORM_TEX and UnityObjectToClipPos

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Size;
            float4 _Position;
            float _tint;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = o.vertex;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                screenUV = screenUV * 0.5 + 0.5;

                float dist = distance(screenUV, _Position.xy);
                float alpha = saturate(smoothstep(_Size, _Size - 0.05, dist));

                float4 tex = tex2D(_MainTex, i.uv);
                tex *= _Color;
                tex.a *= alpha;

                tex.rgb = lerp(tex.rgb, _Color.rgb, _tint);

                return tex;
            }
            ENDCG
        }
    }
}
