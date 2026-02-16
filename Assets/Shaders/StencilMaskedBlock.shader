Shader "UnblockJam/StencilMaskedBlock"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("MainTex", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }

        // Stencil 1 olan yerde çizme => gate içine giren kýsým görünmez
        Stencil
        {
            Ref 1
            Comp NotEqual
            Pass Keep
        }

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        sampler2D _MainTex;
        fixed4 _Color;
        half _Smoothness;

        struct Input { float2 uv_MainTex; };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = 0;
            o.Smoothness = _Smoothness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
