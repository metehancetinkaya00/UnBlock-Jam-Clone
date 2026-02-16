Shader "UnblockJam/StencilWriter"
{
    SubShader
    {
        Tags { "Queue"="Geometry-10" "RenderType"="Opaque" }

        // Ekrana renk yazma (görünmez)
        ColorMask 0

        // Derinliðe yazma (görünmez kalmasý için)
        ZWrite Off

        // Her zaman geç
        ZTest Always

        // Stencil'e 1 yaz
        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass { }
    }
}
