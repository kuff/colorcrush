// Copyright (C) 2024 Peter Guld Leth

Shader "Colorcrush/AnimatedGradientShader"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1, 0, 0, 1)
        _Color2 ("Color 2", Color) = (0, 1, 0, 1)
        _Color3 ("Color 3", Color) = (0, 0, 1, 1)
        _Color4 ("Color 4", Color) = (1, 1, 0, 1)
        _Speed ("Speed", Range(0.1, 5.0)) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            float _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _Speed;
                float t1 = abs(sin(time * 0.3));
                float t2 = abs(sin(time * 0.2 + 1.0));
                float t3 = abs(sin(time * 0.5 + 2.0));
                float t4 = abs(sin(time * 0.4 + 3.0));

                fixed4 color = _Color1 * t1 + _Color2 * t2 + _Color3 * t3 + _Color4 * t4;
                return color;
            }
            ENDCG
        }
    }
}
