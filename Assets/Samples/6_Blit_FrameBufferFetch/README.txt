==== 6. Blit_FrameBufferFetch

This is an example of a blit using frame buffer fetch.

Frame buffer fetch is an optimization mostly utilized on mobile GPUs, where a pass can directly access the frame buffer output from the previous pass.

The blit still uses 2 passes:
1. Blit the camera color to an interim render target
2. Blit the interim render target back to the camera color

However, the second blit's shader utilizes frame buffer fetch.
By doing this, the 2 passes are merged, improving performance.
You can see the passes merged in the Render Graph Viewer, and compare it with the standard blit in Sample 1, where the passes are not merged.

In order to use frame buffer fetch in a shader, use LOAD_FRAMEBUFFER_X_INPUT.
See Assets/Shaders/FrameBufferFetch.shader for reference.

NOTE: Frame buffer fetch is only supported when native render pass is enabled.
      Native render pass is not supported on DX12 and OpenGL/GLES.

      For these graphics APIs, you will need to write a fallback which does not use frame buffer fetch.
      The _RENDER_PASS_ENABLED keyword can be used to check if native render pass is enabled.

      However, as of Unity 6000.0.4f1, _RENDER_PASS_ENABLED in only set in the deferred rendering path.
      If you are using the forward rendering path, you will need to provide a separate shader as a fallback.

Renderer
- Assets/URP/6_Blit_FrameBufferFetchRenderer

Shader
- Assets/Shaders/FrameBufferFetch.shader

Scripts
- Assets/Scripts/BlitFrameBufferFetchRendererFeature.cs: Renderer Feature which blits using frame buffer fetch