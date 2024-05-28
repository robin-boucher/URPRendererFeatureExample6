==== 7. Blit_UnsafePass

This is an example of a blit using an UnsafePass, instead of a RasterPass.

A RasterPass can only write to a single render target.
This can make it cumbersome to write a Renderer Feature which requires many writes, as you will need to provide a separate RasterPass for each.

There are no restrictions in an UnsafePass, allowing you to perform all of your operations in a single pass.

As a downside, UnsafePasses are never merged, which may have an impact on performance.

UnsafePasses are useful for initially porting your existing Renderer Features to Render Graph, and for Renderer Features where pass merging isn't possible.

Renderer
- Assets/URP/7_Blit_UnsafePassRenderer

Scripts
- Assets/Scripts/BlitUnsafeRendererFeature.cs: Renderer Feature which blits to a render target, and sets the camera color buffer to that render target