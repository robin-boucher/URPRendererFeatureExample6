Shader "UTJSample/FrameBufferFetch"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Declare frame buffer input (half precision)
            FRAMEBUFFER_INPUT_X_HALF(0);

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Load frame buffer input from previous pass
                half4 frameBufferInput = LOAD_FRAMEBUFFER_X_INPUT(0, input.positionCS.xy);

                return frameBufferInput;
            }

            ENDHLSL
        }
    }
}