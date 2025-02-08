// Copyright (C) 2025 Peter Guld Leth

Shader "Colorcrush/BoilingButtonShader"
{
    Properties
    {
        _BackgroundColor ("Background Color", Color) = (0.125, 0.667, 0.176, 1.0)
        _AccentColor ("Accent Color", Color) = (0, 0, 0, 1)
        _EffectToggle ("Effect Toggle", Float) = 1.0
        _Speed ("Effect Speed", Float) = 2.0
        _DropSize ("Drop Size", Float) = 0.25
        _Alpha ("Alpha", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            fixed4 _BackgroundColor;
            fixed4 _AccentColor;
            half _EffectToggle;
            half _Speed;
            half _DropSize;
            half _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half dist = length(i.uv - 0.5);
                half ripplePattern = sin(dist * 12.0 - _Time.y * _Speed * 4.0) * 0.1;
                half dropMask = smoothstep(_DropSize + ripplePattern, _DropSize, dist);
                fixed4 finalColor = _EffectToggle < 1
                                        ? _BackgroundColor
                                        : lerp(_AccentColor, _BackgroundColor, dropMask);
                finalColor.a *= _Alpha;
                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}