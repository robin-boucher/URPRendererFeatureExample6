==== 9. LinearUI_Stack

This is a practical example of a real-world use case which occurs when rendering sRGB UI in Linear color space.

Since the alpha blending result is different between Gamma and Linear color space, the final UI coloring will appear off (i.e not what the UI artist intended) when viewed in Linear color space.

We address this by implementing Renderer Features to perform the alpha blending manually.  The steps are as follows:
1. Render the UI into a custom render target named "_UIStackTexture".  If a UI sprite's texture is sRGB, we also perform a color conversion.
2. Manually blend _UIStackTexture on top of the camera RenderTarget, via a blit.

In this scenario, there is a camera stack with multiple overlay cameras.  Each overlay camera renders a separate canvas.

We want to render all of the overlay cameras' UI into a single render target (_UIStackTexture), so we need to maintain a RTHandle externally, and pass it to each camera.

Additionally, we only want to clear the color on _UIStackTexture on the first overlay camera, but not on the following cameras.
This is because we need to draw each camera's UI layer on top of the previous cameras' results.

Renderer
- Assets/URP/9_LinearUIStackRenderer_First: Renderer for first overlay camera in the stack.  Clears the color on _UIStackTexture
- Assets/URP/9_LinearUIStackRenderer_Mid: Renderer for middle overlay cameras.  Does not clear color on _UIStackTexture, so that the UI can be drawn on top of prior overlay cameras' results
- Assets/URP/9_LinearUIStackRenderer_Last: Renderer for last overlay camera.  Performs the final alpha blend with the camera color

Shaders
- Assets/Shaders/UI.shader: Custom UI shader which performs color conversion when in Linear color space (only used on Image components using a sRGB texture)
- Assets/Shaders/UITextureAlphaBlend: Alpha blend _UIStackTexture on top of the camera color

Scripts
- Assets/Samples/9_LinearUI_Stack/Scripts/DrawStackUIRendererFeature.cs: Renderer Feature which draws UI into an external render target
- Assets/Samples/9_LinearUI_Stack/Scripts/AlphaBlendStackUIRendererFeature.cs: Renderer Feature which performs the final alpha blend with the camera color
- Assets/Samples/9_LinearUI_Stack/Scripts/StackUIRTHandle.cs: MonoBehaviour which holds the RenderTexture and RTHandle for _UIStackTexture, and passes it to each Renderer Feature