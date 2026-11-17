Shader "Forgy/MaskSmooth"
{
    Properties
    {
        _MainTex ("Raw Mask", 2D) = "black" {}
        _Prev ("Previous", 2D) = "black" {}
        _Blend ("Blend", Range(0,1)) = 0.4
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _Prev;
            float _Blend;

            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;
                float m = tex2D(_MainTex, i.uv).r * 4.0;
                m += tex2D(_MainTex, i.uv + float2( t.x, 0)).r * 2.0;
                m += tex2D(_MainTex, i.uv + float2(-t.x, 0)).r * 2.0;
                m += tex2D(_MainTex, i.uv + float2(0,  t.y)).r * 2.0;
                m += tex2D(_MainTex, i.uv + float2(0, -t.y)).r * 2.0;
                m += tex2D(_MainTex, i.uv + float2( t.x,  t.y)).r;
                m += tex2D(_MainTex, i.uv + float2(-t.x,  t.y)).r;
                m += tex2D(_MainTex, i.uv + float2( t.x, -t.y)).r;
                m += tex2D(_MainTex, i.uv + float2(-t.x, -t.y)).r;
                m /= 16.0;
                float p = tex2D(_Prev, i.uv).r;
                float o = lerp(p, m, _Blend);
                return fixed4(o, o, o, 1);
            }
            ENDCG
        }
    }
}