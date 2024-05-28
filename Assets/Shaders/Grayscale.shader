Shader "UTJSample/Grayscale"
{
    Properties
    {
        _Grayscale("Grayscale", Range(0, 1)) = 1
        _TintColor("Tint Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Grayscale;
                half3 _TintColor;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color;

                float2 uv = input.texcoord;

                half4 blitTex = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, uv);

                color.rgb = lerp(blitTex.rgb, Luminance(blitTex.rgb) * _TintColor, _Grayscale);
                color.a = blitTex.a;

                return color;
            }

            ENDHLSL
        }
    }
}