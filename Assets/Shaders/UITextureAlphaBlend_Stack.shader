Shader "UTJSample/UITextureAlphaBlend_Stack"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert  // Vertex function from Blit.hlsl
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            // UI texture rendered into from DrawStackUIRendererFeature
            TEXTURE2D(_UIStackTexture);

            half4 frag (Varyings input) : SV_Target
            {
                // Sample built-in _BlitTexture for Blitter API (camera texture)
                half4 cameraTex = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, input.texcoord);

                // Convert camera texture to sRGB before blend
                cameraTex.rgb = FastLinearToSRGB(cameraTex.rgb);

                // Sample UI texture
                half4 uiTex = SAMPLE_TEXTURE2D(_UIStackTexture, sampler_PointClamp, input.texcoord);

                half4 color;

                // Alpha blend UI on top of camera
                color.rgb = cameraTex.rgb * (1 - uiTex.a) + uiTex.rgb;
                
                // Convert result to Linear
                color.rgb = FastSRGBToLinear(color.rgb);
                color.a = 1;

                return color;
            }
            ENDHLSL
        }
    }
}