using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace UTJSample
{
    // Fullscreen blit to custom render target
    public class BlitCustomRTRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public string renderTargetName = "_CustomBlitTex";
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Material material = null;
        }

        public class BlitCustomRTRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME = "BlitCustomRT";

            private Material material;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public TextureHandle sourceTextureHandle;
                public Material material;
            }

            private string renderTargetName;
            private int renderTargetId;

            // RTHandle for custom target
            private RTHandle customRTHandle;

            public BlitCustomRTRenderPass(string renderTargetName, RenderPassEvent renderPassEvent, Material material)
            {
                this.profilingSampler = new ProfilingSampler(nameof(BlitCustomRTRenderPass));

                this.material = material;

                this.renderTargetName = renderTargetName;
                this.renderTargetId = Shader.PropertyToID(this.renderTargetName);

                this.renderPassEvent = renderPassEvent;
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // Recording phase; add passes to RenderGraph

                // FrameData objects
                // ResourceData
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                // CameraData
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                // Pass to blit camera color RT -> tempRT
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME, out PassData passData, this.profilingSampler)) {
                    // TextureHandle for camera color RT
                    TextureHandle cameraColorTextureHandle = resourceData.activeColorTexture;

                    // Camera RT descriptor
                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    desc.msaaSamples = 1;
                    desc.depthBufferBits = 0;

                    // TextureHandle for render target
                    // UniversalRenderer.CreateRenderGraphTexture is a helper method to create RenderGraph TextureHandle
                    TextureHandle targetTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, this.renderTargetName, true);

                    // Set camera color RT for read
                    builder.UseTexture(cameraColorTextureHandle, AccessFlags.Read);

                    // Set target for write
                    // SetRenderAttachment: equivalent of SetRenderTarget
                    builder.SetRenderAttachment(targetTextureHandle, 0, AccessFlags.Write);

                    // Set target as global texture so shaders can access
                    builder.SetGlobalTextureAfterPass(targetTextureHandle, this.renderTargetId);

                    // Disable pass culling
                    // Passes are culled if no other passes access the write target
                    // For example, if only a shader accesses the render target texture (and not a separate pass),
                    // we need to disable pass culling to ensure this pass will always run
                    builder.AllowPassCulling(false);

                    // Resources/References for pass execution
                    // Blit source texture
                    passData.sourceTextureHandle = cameraColorTextureHandle;
                    // Blit material
                    passData.material = this.material;

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecutePass(passData, graphContext));
                }
            }

            // Pass execution function
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

        private BlitCustomRTRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new BlitCustomRTRenderPass(
                this.settings.renderTargetName,
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