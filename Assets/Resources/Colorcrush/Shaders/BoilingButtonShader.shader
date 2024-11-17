// Copyright (C) 2024 Peter Guld Leth

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
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
                // Early return if effect is disabled
                if (_EffectToggle < 1)
                {
                    return fixed4(_BackgroundColor.rgb, _BackgroundColor.a * _Alpha);
                }

                // Calculate UV distance from center
                float2 centeredUV = i.uv - 0.5;
                half dist = length(centeredUV);

                // Create animated ripple pattern using _Time
                half timeOffset = _Time.y * _Speed * 4.0;
                half ripplePattern = sin(dist * 12.0 - timeOffset);
                half rippleIntensity = ripplePattern * 0.1;

                // Create drop effect with ripple
                half dropEdge = _DropSize + rippleIntensity;
                half dropMask = smoothstep(dropEdge, _DropSize, dist);

                // Blend colors based on drop mask
                fixed4 finalColor = lerp(_AccentColor, _BackgroundColor, dropMask);
                finalColor.a *= _Alpha;

                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Unlit/Transparent"
}
