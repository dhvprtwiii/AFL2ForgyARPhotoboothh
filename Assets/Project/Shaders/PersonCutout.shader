Shader "Forgy/PersonCutout"
{
    Properties
    {
        _MainTex ("Webcam", 2D) = "white" {}
        _Mask ("Person Mask", 2D) = "black" {}
        _Threshold ("Threshold", Range(0,1)) = 0.5
        _Softness ("Edge Softness", Range(0.01,0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off ZWrite Off ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Mask;

            float _Threshold;
            float _Softness;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 cam = tex2D(_MainTex, i.uv);
                float m = tex2D(_Mask, i.uv).r;

                float a = smoothstep(
                    _Threshold - _Softness,
                    _Threshold + _Softness,
                    m
                );

                return fixed4(cam.rgb, a);
            }

            ENDCG
        }
    }
}