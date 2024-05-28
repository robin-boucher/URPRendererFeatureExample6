using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
namespace UTJSample
{
    // Fullscreen blit to camera render target
    // Also sets color buffer to new render target,
    // in order to save 1 blit operation
    public class BlitSetColorBufferRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Material material = null;
        }

        public class BlitSetColorBufferRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME = "BlitSetColorBuffer";

            private Material material;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public TextureHandle sourceTextureHandle;
                public Material material;
            }

            public BlitSetColorBufferRenderPass(RenderPassEvent renderPassEvent, Material material)
            {
                this.profilingSampler = new ProfilingSampler(nameof(BlitSetColorBufferRenderPass));

                this.material = material;

                this.renderPassEvent = renderPassEvent;
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (this.material == null) {
                    return;
                }

                // Recording phase; add passes to RenderGraph
                // 1. Blit camera color RT -> new render target
                // 2. Set camera color buffer to new render target

                // FrameData objects
                // ResourceData
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) {
                    // If already rendering to backbuffer, do nothing
                    // (Setting color buffer is unsupported at this point)
                    return;
                }

                // CameraData
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                // TextureHandle for camera color RT
                TextureHandle cameraColorTextureHandle = resourceData.activeColorTexture;

                // Camera RT descriptor
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;

                // TextureHandle for new render target
                // UniversalRenderer.CreateRenderGraphTexture is a helper method to create RenderGraph TextureHandle
                TextureHandle targetTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, string.Format("_{0}_RT", PASS_NAME), true);

                // Pass to blit camera color RT -> tempRT
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME, out PassData passData, this.profilingSampler)) {
                    // Set camera color RT for read
                    builder.UseTexture(cameraColorTextureHandle, AccessFlags.Read);

                    // Set target RT for write
                    builder.SetRenderAttachment(targetTextureHandle, 0, AccessFlags.Write);

                    // Resources/References for pass execution
                    // Blit source texture
                    passData.sourceTextureHandle = cameraColorTextureHandle;
                    // Blit material
                    passData.material = this.material;

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecutePass(passData, graphContext));
                }

                // Set camera color buffer to new render target
                resourceData.cameraColor = targetTextureHandle;

                // NOTE: This method of blitting will not work if the result is needed
                //       in a different camera (i.e camera stacks)
                //       In this case, you must save the result to an external RTHandle and pass it
                //       to the next camera manually
            }

            // Pass execution function
            private static void ExecutePass(PassData passData, RasterGraphContext graphContext)
            {
                RasterCommandBuffer cmd = graphContext.cmd;

                // Blit
                Blitter.BlitTexture(cmd, passData.sourceTextureHandle, new Vector4(1, 1, 0, 0), passData.material, 0);
            }

            public void Dispose()
            {
                // Nothing to do here since RenderGraph handles resource management for us
            }
        }

        [SerializeField] private Settings settings = new Settings();

        private BlitSetColorBufferRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new BlitSetColorBufferRenderPass(
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