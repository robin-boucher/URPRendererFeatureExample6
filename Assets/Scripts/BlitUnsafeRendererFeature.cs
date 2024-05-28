using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace UTJSample
{
    // Fullscreen blit to camera render target
    // Uses unsafe pass
    public class BlitUnsafeRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Material material = null;
        }

        public class BlitUnsafeRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME = "BlitUnsafe";

            private Material material;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public TextureHandle tempTextureHandle;
                public TextureHandle cameraColorTextureHandle;
                public Material material;
            }

            public BlitUnsafeRenderPass(RenderPassEvent renderPassEvent, Material material)
            {
                this.profilingSampler = new ProfilingSampler(nameof(BlitUnsafeRenderPass));

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

                // FrameData objects
                // ResourceData
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                // CameraData
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                // Pass
                // With an unsafe pass, we can sequentially blit to multiple targets in a single pass,
                // which we cannot do in a raster pass
                // (However, unsafe passes can never be merged)
                using (IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass(PASS_NAME, out PassData passData, this.profilingSampler)) {

                    // TextureHandle for camera color RT
                    TextureHandle cameraColorTextureHandle = resourceData.activeColorTexture;

                    // Camera RT descriptor
                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    desc.msaaSamples = 1;
                    desc.depthBufferBits = 0;

                    // TextureHandle for temporary RT
                    // UniversalRenderer.CreateRenderGraphTexture is a helper method to create RenderGraph TextureHandle
                    TextureHandle tempTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_TempRT", true);

                    // Set tempRT/cameraColor for read/write (will be doing both in single unsafe pass)
                    builder.UseTexture(cameraColorTextureHandle, AccessFlags.ReadWrite);
                    builder.UseTexture(tempTextureHandle, AccessFlags.ReadWrite);

                    // Resources/References for pass execution
                    // Blit source texture
                    passData.tempTextureHandle = tempTextureHandle;
                    // Blist destination texture
                    passData.cameraColorTextureHandle = cameraColorTextureHandle;
                    // Blit material
                    passData.material = this.material;

                    // Set render function
                    builder.SetRenderFunc((PassData passData, UnsafeGraphContext graphContext) => ExecutePass(passData, graphContext));
                }
            }

            // Pass execution function (blit)
            private static void ExecutePass(PassData passData, UnsafeGraphContext graphContext)
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphContext.cmd);

                // Blit camera color -> tempRT
                Blitter.BlitCameraTexture(cmd, passData.cameraColorTextureHandle, passData.tempTextureHandle);
                // Blit tempRT -> camera color
                Blitter.BlitCameraTexture(cmd, passData.tempTextureHandle, passData.cameraColorTextureHandle, passData.material, 0);
            }

            public void Dispose()
            {
                // Nothing to do here since RenderGraph handles resource management for us
            }
        }

        [SerializeField] private Settings settings = new Settings();

        private BlitUnsafeRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new BlitUnsafeRenderPass(
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