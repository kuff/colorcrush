Shader "Custom/SelectiveColorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VisiblePixels ("Visible Pixels Texture", 2D) = "black" {} // A texture that encodes which pixels are visible
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
            sampler2D _VisiblePixels;
            fixed4 _DefaultColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                float visibility = tex2D(_VisiblePixels, i.texcoord).r;

                bool isWhite = (texColor.r > 0.95 && texColor.g > 0.95 && texColor.b > 0.95);

                if (visibility > 0.5 || isWhite)
                {
                    return texColor;
                }
                else
                {
                    return _DefaultColor;
                }
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
