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

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);

                // If the pixel is fully transparent, return it unchanged
                if (texColor.a == 0.0)
                {
                    return texColor;
                }

                // Check if the pixel is white
                float whiteDistance = distance(texColor.rgb, float3(1.0, 1.0, 1.0));
                if (whiteDistance < _WhiteTolerance)
                {
                    return fixed4(texColor.rgb, texColor.a * _Alpha); // Return original color if it's white
                }

                // Calculate the distance between the texture color and the skin color
                float skinDistance = distance(texColor.rgb, _SkinColor.rgb);

                if (skinDistance < _Tolerance)
                {
                    // If the pixel color is close to the skin color, change it to the target color
                    return fixed4(_TargetColor.rgb, texColor.a * _Alpha);
                }
                else
                {
                    // Otherwise, adjust the color relative to the new skin color
                    float3 colorDiff = texColor.rgb - _SkinColor.rgb;
                    fixed4 adjustedColor = fixed4(_TargetColor.rgb + colorDiff, texColor.a * _Alpha);
                    return adjustedColor;
                }
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}