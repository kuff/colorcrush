// Copyright (C) 2025 Peter Guld Leth

Shader "Colorcrush/RadarChartShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LevelCount ("Level Count", Range(1, 10)) = 5
        _LineWidth ("Line Width", Range(0.001, 0.1)) = 0.02
        _Alpha ("Alpha", Range(0, 1)) = 1.0 // Alpha value for the entire image
        _BackgroundColor ("Background Color", Color) = (0.5, 0.5, 0.5, 1) // Color for non-transparent pixels
        _LineColor ("Line Color", Color) = (0.3, 0.3, 0.3, 1) // Color for chart lines
        _FillColor ("Fill Color", Color) = (1, 0, 0, 0.5) // Color for the filled area
        _Axis1 ("Axis 1", Range(0, 1)) = 0.5
        _Axis2 ("Axis 2", Range(0, 1)) = 0.5
        _Axis3 ("Axis 3", Range(0, 1)) = 0.5
        _Axis4 ("Axis 4", Range(0, 1)) = 0.5
        _Axis5 ("Axis 5", Range(0, 1)) = 0.5
        _Axis6 ("Axis 6", Range(0, 1)) = 0.5
        _Axis7 ("Axis 7", Range(0, 1)) = 0.5
        _Axis8 ("Axis 8", Range(0, 1)) = 0.5
        _PulseEffect ("Pulse Effect", Range(0, 1)) = 0 // Toggle for pulse effect
        _PulseSpeed ("Pulse Speed", Range(0.1, 5.0)) = 1.0 // Speed of the pulse effect
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _LevelCount;
            float _LineWidth;
            float _Alpha;
            float4 _BackgroundColor;
            float4 _LineColor;
            float4 _FillColor;
            float _Axis1, _Axis2, _Axis3, _Axis4, _Axis5, _Axis6, _Axis7, _Axis8;
            float _PulseEffect;
            float _PulseSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            #define PI 3.14159265359
            #define AXIS_COUNT 8

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            float drawLine(float2 uv, float2 lineStart, float2 lineEnd)
            {
                float2 lineDir = lineEnd - lineStart;
                float2 perpDir = float2(-lineDir.y, lineDir.x);
                float dist = abs(dot(uv - lineStart, normalize(perpDir)));
                return step(_LineWidth, dist) ? 0.0 : 1.0;
            }

            float drawCircle(float2 uv, float radius)
            {
                float dist = abs(length(uv) - radius);
                return step(_LineWidth, dist) ? 0.0 : 1.0;
            }

            float2 getPointOnAxis(int axisIndex)
            {
                float angle = axisIndex * (2.0 * PI / AXIS_COUNT);
                float radius;
                if (axisIndex == 0) radius = _Axis1;
                else if (axisIndex == 1) radius = _Axis2;
                else if (axisIndex == 2) radius = _Axis3;
                else if (axisIndex == 3) radius = _Axis4;
                else if (axisIndex == 4) radius = _Axis5;
                else if (axisIndex == 5) radius = _Axis6;
                else if (axisIndex == 6) radius = _Axis7;
                else radius = _Axis8;
                return float2(cos(angle), sin(angle)) * radius;
            }

            bool isInsidePolygon(float2 p, float2 points[AXIS_COUNT])
            {
                bool inside = false;
                for (int i = 0, j = AXIS_COUNT - 1; i < AXIS_COUNT; j = i++)
                {
                    if (((points[i].y > p.y) != (points[j].y > p.y)) &&
                        (p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[
                            i].x))
                    {
                        inside = !inside;
                    }
                }
                return inside;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 texColor = tex2D(_MainTex, i.texcoord);

                // If the pixel is fully transparent, return it unchanged
                if (texColor.a == 0.0)
                {
                    return texColor;
                }

                // Center and scale the UV coordinates
                float2 uv = i.texcoord * 2.0 - 1.0;

                float radarLine = 0.0;

                // Draw axis lines
                for (int a = 0; a < AXIS_COUNT; a++)
                {
                    float angle = a * (2.0 * PI / AXIS_COUNT);
                    float2 axisEnd = float2(cos(angle), sin(angle));
                    radarLine = max(radarLine, drawLine(uv, float2(0, 0), axisEnd));
                }

                // Draw level circles
                for (int l = 1; l <= _LevelCount; l++)
                {
                    float radius = l / _LevelCount;
                    radarLine = max(radarLine, drawCircle(uv, radius));
                }

                // Get points on each axis
                float2 points[AXIS_COUNT];
                for (int p = 0; p < AXIS_COUNT; p++)
                {
                    points[p] = getPointOnAxis(p);
                }

                // Check if the current pixel is inside the polygon
                bool inside = isInsidePolygon(uv, points);

                // Blend the radar lines with the background
                fixed4 finalColor = lerp(_BackgroundColor, _LineColor, radarLine);

                // Apply fill color if inside the polygon
                if (inside)
                {
                    finalColor = lerp(finalColor, _FillColor, _FillColor.a);
                }

                // Draw points on each axis
                for (int d = 0; d < AXIS_COUNT; d++)
                {
                    float pointDist = length(uv - points[d]);
                    if (pointDist < _LineWidth)
                    {
                        finalColor = _LineColor * 0.5; // Darker color
                    }
                }

                // Apply the alpha value
                finalColor.a *= texColor.a * _Alpha;

                // Apply pulse effect if enabled
                if (_PulseEffect > 0.0)
                {
                    float pulse = frac(_Time.y * _PulseSpeed); // Pulse value between 0 and 1
                    float pulseRadius = pulse; // Adjust the pulse radius as needed
                    float pulseDist = length(uv);
                    float pulseWidth = 0.05; // Width of the pulse ring
                    float fadeOut = smoothstep(0.8, 1.0, pulseDist); // Fade out as it approaches the edge

                    if (abs(pulseDist - pulseRadius) < pulseWidth)
                    {
                        finalColor.rgb = lerp(finalColor.rgb, _LineColor.rgb, 1.0 - fadeOut);
                    }
                }

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}