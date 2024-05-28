using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace UTJSample
{
    // Fullscreen blit to camera render target
    public class BlitRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Material material = null;
        }

        public class BlitRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME_TEMP_RT = "Blit_TempRT";
            private const string PASS_NAME_CAMERA_COLOR = "Blit_CameraColor";

            private Material material;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public TextureHandle sourceTextureHandle;
                public Material material;
            }

            public BlitRenderPass(RenderPassEvent renderPassEvent, Material material)
            {
                this.profilingSampler = new ProfilingSampler(nameof(BlitRenderPass));

                this.material = material;

                this.renderPassEvent = renderPassEvent;

                this.requiresIntermediateTexture = true;
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (this.material == null) {
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
                TextureHandle tempTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_TempRT", true);

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
                    passData.material = this.material;

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecutePass(passData, graphContext));
                }

                // Pass to blit tempRT -> camera color RT
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME_CAMERA_COLOR, out PassData passData, this.profilingSampler)) {

                    // Set tempRT for read
                    builder.UseTexture(tempTextureHandle, AccessFlags.Read);

                    // Set camera color RT for write
                    builder.SetRenderAttachment(cameraColorTextureHandle, 0, AccessFlags.Write);

                    // Resources/References for pass execution
                    // Blit source texture
                    passData.sourceTextureHandle = tempTextureHandle;
                    passData.material = null;

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecutePass(passData, graphContext));
                }
            }

            // Pass execution function (blit)
            private static void ExecutePass(PassData passData, RasterGraphContext graphContext)
            {
                RasterCommandBuffer cmd = graphContext.cmd;

                // Blit
                if (passData.material == null) {
                    // If no material specified
                    Blitter.BlitTexture(cmd, passData.sourceTextureHandle, new Vector4(1, 1, 0, 0), 0, false);
                } else {
                    Blitter.BlitTexture(cmd, passData.sourceTextureHandle, new Vector4(1, 1, 0, 0), passData.material, 0);
                }
            }

            public void Dispose()
            {
                // Nothing to do here since RenderGraph handles resource management for us
            }
        }

        [SerializeField] private Settings settings = new Settings();

        private BlitRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new BlitRenderPass(
                this.settings.renderPassEvent,
                this.settings.material
            );
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // EnqueuePass is still required so that the ScriptableRenderer
            // will know which passes to call RecordRenderGraph on
            renderer.EnqueuePass(this.renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            // Use Dispose for cleanup

            this.renderPass.Dispose();
        }
    }
}