Shader "Custom/RevealBehindShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _CircleSize ("Circle Size", Float) = 0.0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Overlay"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            fixed4 _Color;
            float _CircleSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate the aspect ratio
                float2 aspectRatio = float2(_ScreenParams.x / _ScreenParams.y, 1.0);

                // Calculate the distance from the center of the screen
                float2 center = float2(0.5, 0.5);
                float2 uv = (i.screenPos.xy / i.screenPos.w - center) * aspectRatio + center;
                float dist = length((uv - center) * aspectRatio);

                // Calculate alpha with a hard edge
                float alpha = step(_CircleSize, dist);

                // Return the final color with adjusted transparency
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}