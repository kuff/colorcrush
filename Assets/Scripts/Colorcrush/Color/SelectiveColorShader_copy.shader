Shader "Custom/SelectiveColorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AllowedColor1 ("Allowed Color 1", Color) = (1,1,1,1)
        _AllowedColor2 ("Allowed Color 2", Color) = (1,1,1,1)
        _AllowedColor3 ("Allowed Color 3", Color) = (1,1,1,1)
        _Tolerance ("Color Tolerance", Range(0, 1)) = 0.1
        _DefaultColor ("Default Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _AllowedColor1;
            float4 _AllowedColor2;
            float4 _AllowedColor3;
            float _Tolerance;
            float4 _DefaultColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            bool IsColorAllowed(float3 color, float3 allowedColor)
            {
                return distance(color, allowedColor) < _Tolerance;
            }

            bool IsWhite(float3 color)
            {
                return distance(color, float3(1, 1, 1)) < _Tolerance;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);

                if (IsWhite(texColor.rgb) ||
                    IsColorAllowed(texColor.rgb, _AllowedColor1.rgb) ||
                    IsColorAllowed(texColor.rgb, _AllowedColor2.rgb) ||
                    IsColorAllowed(texColor.rgb, _AllowedColor3.rgb))
                {
                    return texColor;
                }
                else
                {
                    return _DefaultColor; // Return default color for non-matching pixels
                }
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
