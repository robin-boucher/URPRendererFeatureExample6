==== 5. Blit_SetCameraColor

This is an example of a blit to a render target, and then set the camera color buffer to that render target.

Normally, a blit requires 2 passes to blit back to the camera color:
1. Blit the camera color to an interim render target
2. Blit the interim render target back to the camera color

However, it is possible to directly set the interim render target as the new camera color buffer RTHandle.
By doing this, 1 blit operation is saved, improving performance.

Note that this will only work if there is only 1 camera (i.e. no overlay cameras), and the blit is not already writing to the backbuffer (i.e. at RenderPassEvent.AfterRendering).
If you are using a camera stack, you will need to save the result to an external render target (by using ImportTexture, as seen in Sample 4), and manually pass it to the next camera.

Renderer
- Assets/URP/5_Blit_SetCameraColorRenderer

Scripts
- Assets/Scripts/BlitSetColorBufferRendererFeature.cs: Renderer Feature which blits to a render target, and sets the camera color buffer to that render target