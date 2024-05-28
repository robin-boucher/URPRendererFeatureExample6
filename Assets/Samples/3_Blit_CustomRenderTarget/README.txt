==== 3. Blit_CustomRenderTarget

This is an example of a blit to a custom render target, and not back to the camera color.

The example blits the camera color to a custom render target called "_Sample3CustomRT".
_Sample3CustomRT is then set as a global texture so that it can be referenced in shaders.

AllowPassCulling is set to false so that the pass will not be culled.
Ordinarily, this pass would be culled because no future passes reference _Sample3CustomRT,
so AllowPassCulling is set to false to force the pass to be executed.

Renderer
- Assets/URP/3_Blit_CustomRenderTargetRenderer

Scripts
- Assets/Scripts/BlitCustomRTRendererFeature.cs: Renderer Feature which blits to a custom render target