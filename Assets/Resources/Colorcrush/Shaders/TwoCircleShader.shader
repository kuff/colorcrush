// Copyright (C) 2024 Peter Guld Leth

Shader "Colorcrush/TwoCirclesShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Circle1Color ("Circle 1 Color", Color) = (1,0,0,1)
        _Circle2Color ("Circle 2 Color", Color) = (0,0,1,1)
        _BackgroundColor ("Background Color", Color) = (0,0,0,0)
        _Circle1Radius ("Circle 1 Radius", Float) = 0.2
        _Circle2Radius ("Circle 2 Radius", Float) = 0.2
        _Circle1Position ("Circle 1 Position", Vector) = (0.3, 0.5, 0, 0)
        _Circle2Position ("Circle 2 Position", Vector) = (0.7, 0.5, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Circle1Color;
            float4 _Circle2Color;
            float4 _BackgroundColor;
            float _Circle1Radius;
            float _Circle2Radius;
            float4 _Circle1Position;
            float4 _Circle2Position;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Calculate distances to the centers of the circles
                float distToCircle1 = distance(uv, _Circle1Position.xy);
                float distToCircle2 = distance(uv, _Circle2Position.xy);

                // Check if pixel is within Circle 1
                if (distToCircle1 < _Circle1Radius)
                {
                    return _Circle1Color;
                }
                // Check if pixel is within Circle 2
                else if (distToCircle2 < _Circle2Radius)
                {
                    return _Circle2Color;
                }
                else
                {
                    return _BackgroundColor;
                }
            }
            ENDCG
        }
    }
}
