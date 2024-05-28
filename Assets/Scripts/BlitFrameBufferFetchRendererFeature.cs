using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
namespace UTJSample
{
    // Fullscreen blit to camera render target
    // Uses frame buffer fetch to read output from previous pass to allow pass merging,
    // which reduces the amount of data sent to GPU each frame
    public class BlitFrameBufferFetchRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Material blitMaterial = null;
            public Shader frameBufferFetchShader = null;
        }

        public class BlitFrameBufferFetchRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME_TEMP_RT = "BlitFBFetch_TempRT";
            private const string PASS_NAME_CAMERA_COLOR = "BlitFBFetch_CameraColor";

            private Material blitMaterial;
            private Material frameBufferFetchMaterial;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public TextureHandle sourceTextureHandle;
                public Material material;
            }

            public BlitFrameBufferFetchRenderPass(RenderPassEvent renderPassEvent, Material blitMaterial)
            {
                this.profilingSampler = new ProfilingSampler(nameof(BlitFrameBufferFetchRenderPass));

                this.blitMaterial = blitMaterial;

                this.renderPassEvent = renderPassEvent;
            }

            public void SetFrameBufferFetchMaterial(Material frameBufferFetchMaterial)
            {
                this.frameBufferFetchMaterial = frameBufferFetchMaterial;
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (this.blitMaterial == null) {
                    return;
                }
                if (this.frameBufferFetchMaterial == null) {
                    return;
                }

                // Recording phase; add passes to RenderGraph
                // 1. Blit camera color RT -> temporary RT
                // 2. Blit temporary RT -> camera color RT

                // FrameData objects
                // ResourceData
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                // CameraData
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                // TextureHandle for camera color RT
                TextureHandle cameraColorTextureHandle = resourceData.activeColorTexture;

                // Camera RT descriptor
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                // TextureHandle for temporary RT
                // UniversalRenderer.CreateRenderGraphTexture is a helper method to create RenderGraph TextureHandle
                TextureHandle tempTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_TempRT", false);

                // Pass to blit camera color RT -> tempRT
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME_TEMP_RT, out PassData passData, this.profilingSampler)) {

                    // Set camera color RT for read
                    builder.UseTexture(cameraColorTextureHandle, AccessFlags.Read);

                    // Set tempRT for write
                    // SetRenderAttachment: equivalent of SetRenderTarget
                    builder.SetRenderAttachment(tempTextureHandle, 0, AccessFlags.Write);

                    // Resources/References for pass execution
                    // Blit source texture
                    passData.sourceTextureHandle = cameraColorTextureHandle;
                    // Blit material
                    passData.material = this.blitMaterial;

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecuteBlitToTempRTPass(passData, graphContext));
                }

                // Pass to blit tempRT -> camera color RT
                // Uses frame buffer fetch
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME_CAMERA_COLOR, out PassData passData, this.profilingSampler)) {

                    // Use SetInputAttachment to set tempRT as frame buffer input
                    builder.SetInputAttachment(tempTextureHandle, 0, AccessFlags.Read);

                    // Set camera color RT for write
                    builder.SetRenderAttachment(cameraColorTextureHandle, 0, AccessFlags.Write);

                    // Resources/References for pass execution
                    // No blit source texture, since we are using previous pass output
                    passData.sourceTextureHandle = TextureHandle.nullHandle;
                    passData.material = this.frameBufferFetchMaterial;

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecuteFrameBuferFetchPass(passData, graphContext));
                }
            }

            // Pass execution function (blit to TempRT)
            private static void ExecuteBlitToTempRTPass(PassData passData, RasterGraphContext graphContext)
            {
                RasterCommandBuffer cmd = graphContext.cmd;

                // Blit
                Blitter.BlitTexture(cmd, passData.sourceTextureHandle, new Vector4(1, 1, 0, 0), passData.material, 0);
            }

            // Pass execution function (frame buffer fetch)
            private static void ExecuteFrameBuferFetchPass(PassData passData, RasterGraphContext graphContext)
            {
                RasterCommandBuffer cmd = graphContext.cmd;

                // Blit
                Blitter.BlitTexture(cmd, new Vector4(1, 1, 0, 0), passData.material, 0);
            }

            public void Dispose()
            {
                // Nothing to do here since RenderGraph handles resource management for us
            }
        }

        [SerializeField] private Settings settings = new Settings();

        private Material frameBufferFetchMaterial;

        private BlitFrameBufferFetchRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new BlitFrameBufferFetchRenderPass(
                this.settings.renderPassEvent,
                this.settings.blitMaterial
            );
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (this.frameBufferFetchMaterial == null) {
                if (this.settings.frameBufferFetchShader != null) {
                    this.frameBufferFetchMaterial = CoreUtils.CreateEngineMaterial(this.settings.frameBufferFetchShader);
                    this.renderPass.SetFrameBufferFetchMaterial(this.frameBufferFetchMaterial);
                }
            }

            // EnqueuePass is still required so that the ScriptableRenderer
            // will know which passes to call RecordRenderGraph on
            renderer.EnqueuePass(this.renderPass);

        }

        protected override void Dispose(bool disposing)
        {
            // Use Dispose for cleanup

            this.renderPass.Dispose();

            CoreUtils.Destroy(this.frameBufferFetchMaterial);
            this.frameBufferFetchMaterial = null;
        }
    }
}