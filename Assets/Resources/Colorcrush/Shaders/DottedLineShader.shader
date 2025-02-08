// Copyright (C) 2025 Peter Guld Leth

Shader "Colorcrush/DottedLineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LineColor ("Line Color", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0,0,0,0)
        _LineThickness ("Line Thickness", Range(0.001, 0.1)) = 0.01
        _DotSpacing ("Dot Spacing", Range(0.01, 0.1)) = 0.05
        _XSize ("X Size", Range(0.01, 0.1)) = 0.05
        _XOffset ("X Offset from Bottom", Range(0, 0.2)) = 0.1
        _LineEndOffset ("Line End Offset", Range(0, 0.2)) = 0.15
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }
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
            float4 _LineColor;
            float4 _BackgroundColor;
            float _LineThickness;
            float _DotSpacing;
            float _XSize;
            float _XOffset;
            float _LineEndOffset;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            bool isInRotatedX(float2 uv, float2 center, float size)
            {
                float2 d = uv - center;
                float2 rotated = float2(
                    d.x * cos(radians(45)) - d.y * sin(radians(45)),
                    d.x * sin(radians(45)) + d.y * cos(radians(45))
                );
                rotated = abs(rotated);
                return (rotated.x < size && rotated.y < size && (rotated.x < _LineThickness || rotated.y <
                    _LineThickness));
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float lineX = 0.5;

                // Calculate the distance from the current pixel to the line
                float distToLine = abs(uv.x - lineX);

                // Create a dotted pattern
                float pattern = step(0.5, frac(uv.y / _DotSpacing));

                // Check if we're in the rotated X at the bottom
                float2 xCenter = float2(lineX, _XOffset + _XSize);
                bool inX = isInRotatedX(uv, xCenter, _XSize);

                // Determine if this pixel is within the line thickness and should be visible
                // The line starts from the bottom and ends before the X
                if ((distToLine < _LineThickness && pattern > 0 && uv.y > _LineEndOffset && uv.y < (1 - _XOffset -
                    _XSize * 2)) || inX)
                {
                    return _LineColor;
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