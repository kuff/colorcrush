// Copyright (C) 2024 Peter Guld Leth

Shader "Colorcrush/ColorTransposeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TargetColor ("Target Color", Color) = (1, 0, 0, 1) // Default red target color
        _SkinColor ("Skin Color", Color) = (0.97, 0.87, 0.25, 1) // Yellow skin color F8DE40
        _Tolerance ("Tolerance", Range(0, 1)) = 0.1 // Tolerance for color matching
        _WhiteTolerance ("White Tolerance", Range(0, 1)) = 0.1 // Tolerance for detecting white color
        _Alpha ("Alpha", Range(0, 1)) = 1.0 // Alpha value for the entire image
        _FillScale ("Fill Scale", Range(0, 1)) = 1.0 // Scale factor for the texture
        _FillColor ("Fill Color", Color) = (1, 1, 1, 1) // Fill color for scaled texture
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

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TargetColor;
            fixed4 _SkinColor;
            float _Tolerance;
            float _WhiteTolerance;
            float _Alpha;
            float _FillScale;
            fixed4 _FillColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.texcoord - 0.5;
                float2 scaledUV = centeredUV / _FillScale + 0.5;
                
                fixed4 originalColor = tex2D(_MainTex, i.texcoord);

                // Check if the pixel is within the original non-transparent area
                bool isWithinOriginalArea = originalColor.a > 0.1;

                // If the pixel is outside the scaled area and within the original area, use the fill color
                if ((scaledUV.x < 0 || scaledUV.x > 1 || scaledUV.y < 0 || scaledUV.y > 1) && isWithinOriginalArea)
                {
                    return _FillColor;
                }

                // If the original pixel is transparent (including very low alpha), keep it fully transparent
                if (!isWithinOriginalArea)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // Sample the scaled color only if it's within the texture bounds
                fixed4 scaledColor = (scaledUV.x >= 0 && scaledUV.x <= 1 && scaledUV.y >= 0 && scaledUV.y <= 1) 
                    ? tex2D(_MainTex, scaledUV) 
                    : _FillColor;

                // If the scaled pixel is transparent (including very low alpha) and within the original area, use the fill color
                if (scaledColor.a <= 0.1)
                {
                    return _FillColor;
                }

                // Check if the pixel is white
                float whiteDistance = distance(scaledColor.rgb, float3(1.0, 1.0, 1.0));
                if (whiteDistance < _WhiteTolerance)
                {
                    return fixed4(scaledColor.rgb, scaledColor.a * _Alpha); // Return original color if it's white
                }

                // Calculate the distance between the texture color and the skin color
                float skinDistance = distance(scaledColor.rgb, _SkinColor.rgb);

                if (skinDistance < _Tolerance)
                {
                    // If the pixel color is close to the skin color, change it to the target color
                    return fixed4(_TargetColor.rgb, scaledColor.a * _Alpha);
                }
                else
                {
                    // Otherwise, adjust the color relative to the new skin color
                    float3 colorDiff = scaledColor.rgb - _SkinColor.rgb;
                    fixed4 adjustedColor = fixed4(_TargetColor.rgb + colorDiff, scaledColor.a * _Alpha);
                    return adjustedColor;
                }
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}