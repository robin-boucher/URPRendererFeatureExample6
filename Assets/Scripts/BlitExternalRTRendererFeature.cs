using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace UTJSample
{
    // Fullscreen blit to external RenderTexture
    public class BlitExternalRTRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public RenderTexture renderTexture;
            public Material material = null;
        }

        public class BlitExternalRTRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME = "BlitExternalRT";

            private RenderTexture targetRenderTexture;
            private RTHandle targetRTHandle;

            private Material material;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public TextureHandle sourceTextureHandle;
                public Material material;
            }

            public BlitExternalRTRenderPass(RenderPassEvent renderPassEvent, RenderTexture renderTexture, Material material)
            {
                this.profilingSampler = new ProfilingSampler(nameof(BlitExternalRTRenderPass));

                this.targetRenderTexture = renderTexture;
                this.material = material;

                this.renderPassEvent = renderPassEvent;
            }

            public void SetRenderTexture(RenderTexture renderTexture)
            {
                this.targetRenderTexture = renderTexture;
            }

            public void SetRTHandle(RTHandle rtHandle)
            {
                this.targetRTHandle = rtHandle;
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (this.targetRenderTexture == null) {
                    this.targetRTHandle?.Release();
                    return;
                }

                // Create RTHandle from render texture
                if (this.targetRTHandle == null || this.targetRTHandle.rt != this.targetRenderTexture) {
                    this.targetRTHandle?.Release();
                    this.targetRTHandle = RTHandles.Alloc(this.targetRenderTexture);
                }

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

                    // TextureHandle from external render texture
                    TextureHandle targetTextureHandle = renderGraph.ImportTexture(this.targetRTHandle);

                    // Set camera color RT for read
                    builder.UseTexture(cameraColorTextureHandle, AccessFlags.Read);

                    // Set external RT for write
                    builder.SetRenderAttachment(targetTextureHandle, 0, AccessFlags.Write);

                    // Resources/References for pass execution
                    // Blit source texture
                    passData.sourceTextureHandle = cameraColorTextureHandle;
                    // Blit material
                    passData.material = this.material;

                    // NOTE: When using ImportTexture, the pass will never be culled

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
                // Release RTHandle
                this.targetRTHandle?.Release();
            }
        }

        [SerializeField] private Settings settings = new Settings();

        private BlitExternalRTRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new BlitExternalRTRenderPass(
                this.settings.renderPassEvent,
                this.settings.renderTexture,
                this.settings.material
            );
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // EnqueuePass is still required so that the ScriptableRenderer
            // will know which passes to call RecordRenderGraph on
            renderer.EnqueuePass(this.renderPass);
        }

        public void SetRenderTexture(RenderTexture renderTexture)
        {
            this.renderPass.SetRenderTexture(renderTexture);
        }

        public void SetRTHandle(RTHandle rtHandle)
        {
            this.renderPass.SetRTHandle(rtHandle);
        }

        protected override void Dispose(bool disposing)
        {
            // Use Dispose for cleanup

            this.renderPass.Dispose();
        }
    }
}