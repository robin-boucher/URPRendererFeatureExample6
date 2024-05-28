==== 8. LinearUI

This is a practical example of a real-world use case which occurs when rendering sRGB UI in Linear color space.

Since the alpha blending result is different between Gamma and Linear color space, the final UI coloring will appear off (i.e not what the UI artist intended) when viewed in Linear color space.

We address this by implementing Renderer Features to perform the alpha blending manually.  The steps are as follows:
1. Render the UI into a custom render target named _UITexture.  If the UI texture is sRGB, we perform a color conversion.
2. Manually blend _UITexture on top of the camera RenderTarget, via a blit.

Renderer
- Assets/URP/8_LinearUIRenderer

Shaders
- Assets/Shaders/UI.shader: Custom UI shader which performs color conversion when in Linear color space (only used on Image components using a sRGB texture)
- Assets/Shaders/UITextureAlphaBlend: Alpha blend _UITexture on top of the camera color

Scripts
- Assets/Scripts/DrawRenderersCustomRTRendererFeature.cs: Renderer Feature which draws renderers into a custom render target
- Assets/Scripts/BlitRendererFeature.cs: Renderer Feature which performs a standard blit in 2 RasterPasses