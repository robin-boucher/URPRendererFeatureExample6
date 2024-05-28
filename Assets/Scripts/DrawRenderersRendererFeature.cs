using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace UTJSample
{
    // Draw renderers
    public class DrawRenderersRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public RenderQueueType renderQueueType = RenderQueueType.Opaque;
            public LayerMask layerMask = -1;
        }

        public class DrawRenderersRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME = "DrawRenderers";

            private RenderQueueType renderQueueType;

            private FilteringSettings filteringSettings;

            private List<ShaderTagId> shaderTagIds;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public RendererListHandle rendererListHandle;
            }

            public DrawRenderersRenderPass(RenderPassEvent renderPassEvent, RenderQueueType renderQueueType, LayerMask layerMask)
            {
                this.profilingSampler = new ProfilingSampler(nameof(DrawRenderersRenderPass));

                this.renderPassEvent = renderPassEvent;

                this.renderQueueType = renderQueueType;

                // Filtering settings
                RenderQueueRange renderQueueRange = renderQueueType == RenderQueueType.Opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent;
                this.filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

                // Target material shader pass names
                this.shaderTagIds = new List<ShaderTagId>();
                this.shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
                this.shaderTagIds.Add(new ShaderTagId("UniversalForward"));
                this.shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));

                // this.shaderTagIds.Add(new ShaderTagId("Universal2D"));
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // Recording phase; add passes to RenderGraph

                // FrameData objects
                // ResourceData
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                // RenderingData
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                // CameraData
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                // LightData
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                // Pass to draw renderers
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME, out PassData passData, this.profilingSampler)) {
                    // Sorting criteria (default transparent)
                    SortingCriteria sortingCriteria = this.renderQueueType == RenderQueueType.Opaque
                        ? cameraData.defaultOpaqueSortFlags
                        : SortingCriteria.CommonTransparent;

                    // Drawing settings
                    DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(this.shaderTagIds, renderingData, cameraData, lightData, sortingCriteria);

                    // RendererListHandle
                    RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, this.filteringSettings);
                    passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

                    // Set pass to use rendererListHandle
                    builder.UseRendererList(passData.rendererListHandle);

                    // Set render target (current camera color/depth targets)
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecutePass(passData, graphContext));
                }
            }

            // Pass execution function
            private static void ExecutePass(PassData passData, RasterGraphContext graphContext)
            {
                RasterCommandBuffer cmd = graphContext.cmd;

                // Draw renderer list
                cmd.DrawRendererList(passData.rendererListHandle);
            }
        }

        [SerializeField] private Settings settings = new Settings();

        private DrawRenderersRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new DrawRenderersRenderPass(
                this.settings.renderPassEvent,
                this.settings.renderQueueType,
                this.settings.layerMask
            );
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(this.renderPass);
        }
    }
}