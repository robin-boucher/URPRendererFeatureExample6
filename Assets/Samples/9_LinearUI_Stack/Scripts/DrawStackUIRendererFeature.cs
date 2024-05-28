using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace UTJSample
{
    // Draw UI to external render texture
    // Used for rendering UI across multiple cameras into single render target
    public class DrawStackUIRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public bool firstPass = true;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public LayerMask layerMask = -1;
        }

        public class DrawStackUIRenderPass : ScriptableRenderPass
        {
            private const string PASS_NAME = "DrawStackUI";

            private FilteringSettings filteringSettings;

            private List<ShaderTagId> shaderTagIds;

            private bool firstPass;

            // External render target
            // Managed externally in StackUIRenderTarget.cs
            private RTHandle targetRTHandle;

            // PassData classes to hold resource handles and references
            private class PassData
            {
                public bool firstPass;
                public RendererListHandle rendererListHandle;
            }

            public DrawStackUIRenderPass(bool firstPass, RenderPassEvent renderPassEvent, LayerMask layerMask)
            {
                this.profilingSampler = new ProfilingSampler(nameof(DrawStackUIRenderPass));

                this.renderPassEvent = renderPassEvent;

                this.firstPass = firstPass;

                // Filtering settings
                this.filteringSettings = new FilteringSettings(RenderQueueRange.transparent, layerMask);

                // Target material shader pass names
                this.shaderTagIds = new List<ShaderTagId>();
                this.shaderTagIds.Add(new ShaderTagId("UniversalForward"));
                this.shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
                this.shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
                // this.shaderTagIds.Add(new ShaderTagId("Universal2D"));
            }

            public void SetRTHandle(RTHandle rtHandle)
            {
                this.targetRTHandle = rtHandle;
            }

            // Record render graph
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (this.targetRTHandle == null || this.targetRTHandle.rt == null) {
                    return;
                }

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

                    // TextureHandle from external render target
                    TextureHandle targetTextureHandle = renderGraph.ImportTexture(this.targetRTHandle);
          
                    // Sorting criteria (default transparent)
                    SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;

                    // Drawing settings
                    DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(this.shaderTagIds, renderingData, cameraData, lightData, sortingCriteria);

                    // RendererListHandle
                    RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, this.filteringSettings);
                    passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);
                    passData.firstPass = this.firstPass;  // firstPass flag

                    // Set pass to use rendererListHandle
                    builder.UseRendererList(passData.rendererListHandle);

                    // Set render target (custom render target)
                    builder.SetRenderAttachment(targetTextureHandle, 0, AccessFlags.Write);

                    // Set render function
                    builder.SetRenderFunc((PassData passData, RasterGraphContext graphContext) => ExecutePass(passData, graphContext));
                }
            }

            // Pass execution function
            private static void ExecutePass(PassData passData, RasterGraphContext graphContext)
            {
                RasterCommandBuffer cmd = graphContext.cmd;

                // Clear color only the first pass,
                // so that UI can be stacked on top of previous overlay cameras
                cmd.ClearRenderTarget(true, passData.firstPass, Color.clear);

                // Draw renderer list
                cmd.DrawRendererList(passData.rendererListHandle);
            }

            public void Dispose()
            {
                // Do nothing; RTHandle is handled externally
            }
        }

        [SerializeField] private Settings settings = new Settings();

        private DrawStackUIRenderPass renderPass;

        public override void Create()
        {
            this.renderPass = new DrawStackUIRenderPass(
                this.settings.firstPass,
                this.settings.renderPassEvent,
                this.settings.layerMask
            );
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(this.renderPass);
        }

        public void SetRTHandle(RTHandle rtHandle)
        {
            this.renderPass.SetRTHandle(rtHandle);
        }
    }
}