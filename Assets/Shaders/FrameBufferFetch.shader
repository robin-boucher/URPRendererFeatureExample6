Shader "UTJSample/FrameBufferFetch"
{
    // NOTE: Frame buffer fetch is only supported if native render pass is enabled
    //       DX12 and OpenGL/GLES do not support native render pass, so this shader
    //       will not work on these graphics APIs

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

            // Declare frame buffer input (half precision)
            FRAMEBUFFER_INPUT_X_HALF(0);

            half4 Frag(Varyings input) : SV_Target
            {
                // Load frame buffer input from previous pass
                half4 color = LOAD_FRAMEBUFFER_X_INPUT(0, input.positionCS.xy);

                return color;
            }

            ENDHLSL
        }
    }
}