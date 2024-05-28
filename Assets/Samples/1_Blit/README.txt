==== 1. Blit

This is an example of a basic blit using Render Graph.

Blit is done in 2 passes:
1. Blit the camera color to a temporary render target
2. Blit the temporary render target back to the camera color

Blitting between the same source/destination render target is unsupported.

Renderer
- Assets/URP/1_BlitRenderer

Scripts
- Assets/Scripts/BlitRendererFeature.cs: Renderer Feature which performs the blit in 2 passes