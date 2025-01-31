// Copyright (C) 2025 Peter Guld Leth

Shader "Colorcrush/RollingGradientShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TimeSpeed ("Time Speed", Range(0.1, 10)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2_f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D main_tex;
            float time_speed;

            v2_f vert(const appdata_t v)
            {
                v2_f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(const v2_f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                const float time = _Time.y * time_speed;

                // Create a smooth rolling gradient
                float r = 0.5 + 0.5 * sin(p.x * 3.0 + time);
                float g = 0.5 + 0.5 * sin(p.y * 3.0 + time + 1.0);
                float b = 0.5 + 0.5 * sin((p.x + p.y) * 3.0 + time + 2.0);

                return fixed4(r, g, b, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}