using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace UTJSample
{
    // Alpha blend stack UI with camera color
    public class AlphaBlendStackUIRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Material material = null;
        }

        public class AlphaBlendStackUIRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME_TEMP_RT = "AlphaBlendStackUI_TempRT";
            private const string PASS_NAME_CAMERA_COLOR = "AlphaBlendStackUI_CameraColor";

            private Material material;

            private RenderTexture uiRenderTexture;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public TextureHandle sourceTextureHandle;
                public Material material;
            }

            public AlphaBlendStackUIRenderPass(RenderPassEvent renderPassEvent, Material material)
            {
                this.profilingSampler = new ProfilingSampler(nameof(AlphaBlendStackUIRenderPass));

                this.material = material;

                this.renderPassEvent = renderPassEvent;

                this.requiresIntermediateTexture = true;
            }

            public void SetRenderTexture(RenderTexture uiRenderTexture)
            {
                this.uiRenderTexture = uiRenderTexture;
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (this.uiRenderTexture == null) {
                    return;
                }
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

                    // Set UI render texture to material property
                    this.material.SetTexture("_UIStackTexture", this.uiRenderTexture);

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
        }

        [SerializeField] private Settings settings = new Settings();

        private AlphaBlendStackUIRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new AlphaBlendStackUIRenderPass(
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

        public void SetRenderTexture(RenderTexture uiRenderTexture)
        {
            this.renderPass.SetRenderTexture(uiRenderTexture);
        }
    }
}