using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace UTJSample
{
    // Draw renderers to custom render target
    public class DrawRenderersRendererCustomRTFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public string renderTargetName = "_CustomDrawRenderersTex";
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public RenderQueueType renderQueueType = RenderQueueType.Opaque;
            public LayerMask layerMask = -1;
        }

        public class DrawRenderersCustomRTRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME = "DrawRenderersCustomRT";

            private RenderQueueType renderQueueType;

            private FilteringSettings filteringSettings;

            private List<ShaderTagId> shaderTagIds;

            private string renderTargetName;
            private int renderTargetId;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public RendererListHandle rendererListHandle;
            }

            public DrawRenderersCustomRTRenderPass(string renderTargetName, RenderPassEvent renderPassEvent, RenderQueueType renderQueueType, LayerMask layerMask)
            {
                this.profilingSampler = new ProfilingSampler(nameof(DrawRenderersCustomRTRenderPass));

                this.renderPassEvent = renderPassEvent;

                this.renderTargetName = renderTargetName;
                this.renderTargetId = Shader.PropertyToID(this.renderTargetName);

                this.renderQueueType = renderQueueType;

                // Filtering settings
                RenderQueueRange renderQueueRange = renderQueueType == RenderQueueType.Opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent;
                this.filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

                // Target material shader pass names
                this.shaderTagIds = new List<ShaderTagId>();
                this.shaderTagIds.Add(new ShaderTagId("UniversalForward"));
                this.shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
                this.shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
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
                    // TextureHandle for camera color RT
                    TextureHandle cameraColorTextureHandle = resourceData.activeColorTexture;

                    // Camera RT descriptor
                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    desc.colorFormat = RenderTextureFormat.ARGB32; // Enable alpha
                    desc.msaaSamples = 1;
                    desc.depthBufferBits = 0;

                    // TextureHandle for render target
                    // UniversalRenderer.CreateRenderGraphTexture is a helper method to create RenderGraph TextureHandle
                    TextureHandle targetTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, this.renderTargetName, true);

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

                    // Set render target (custom render target)
                    builder.SetRenderAttachment(targetTextureHandle, 0, AccessFlags.Write);

                    // Set target as global texture so shaders can access
                    builder.SetGlobalTextureAfterPass(targetTextureHandle, this.renderTargetId);

                    // Disable pass culling
                    // Passes are culled if no other passes access the write target
                    // For example, if only a shader accesses the render target texture (and not a separate pass),
                    // we need to disable pass culling to ensure this pass will always run
                    builder.AllowPassCulling(false);

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

        private DrawRenderersCustomRTRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new DrawRenderersCustomRTRenderPass(
                this.settings.renderTargetName,
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