using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace UTJSample
{
    // Class to hold RenderTexture and RTHandle to draw UI
    // into a single target across multiple overlay cameras

    // Note that this example intentionally does not use SetGlobalTexture
    // for demonstration purposes (showing how to use ImportTexture in a RenderPass
    // to use an externally maintained RTHandle)

    [ExecuteInEditMode] // ExecuteInEditMode to work in editor
    public class StackUIRTHandle : MonoBehaviour
    {
        [SerializeField] private Camera baseCamera;

        private RenderTexture renderTexture;
        private RTHandle rtHandle;

        private List<DrawStackUIRendererFeature> drawStackUIRendererFeatures;
        private List<AlphaBlendStackUIRendererFeature> alphaBlendStackUIRendererFeatures;

        private void Cleanup()
        {
            // Release RTHandle and clear RenderPipelineManager callbacks

            ReleaseRenderTexture();

            RenderPipelineManager.beginContextRendering -= BeginContextRendering;
            RenderPipelineManager.endContextRendering -= EndContextRendering;
        }

        private void OnEnable()
        {
            // Add RenderPipelineManager callbacks
            RenderPipelineManager.beginContextRendering -= BeginContextRendering;
            RenderPipelineManager.endContextRendering -= EndContextRendering;
            RenderPipelineManager.beginContextRendering += BeginContextRendering;
            RenderPipelineManager.endContextRendering += EndContextRendering;

            CreateRenderTexture();
            this.drawStackUIRendererFeatures = GetRendererFeatures<DrawStackUIRendererFeature>();
            this.alphaBlendStackUIRendererFeatures = GetRendererFeatures<AlphaBlendStackUIRendererFeature>();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void CreateRenderTexture()
        {
            // Create render texture if needed

            if (this.renderTexture == null || this.rtHandle == null || this.rtHandle.rt == null) {
                ReleaseRenderTexture();

                // Create RenderTexture and allocate RTHandle
                RenderTextureDescriptor desc = new RenderTextureDescriptor(
                    this.baseCamera.pixelWidth, this.baseCamera.pixelHeight,
                    SystemInfo.GetGraphicsFormat(DefaultFormat.LDR),
                    0, -1
                );
                this.renderTexture = new RenderTexture(desc);
                this.renderTexture.name = "_UIStackTexture";
                this.rtHandle = RTHandles.Alloc(this.renderTexture);
            }
        }

        private void ReleaseRenderTexture()
        {
            this.renderTexture?.Release();
            this.rtHandle?.Release();
        }

        private List<T> GetRendererFeatures<T>() where T : ScriptableRendererFeature
        {
            // Collect renderer feature from camera stack (need to use reflection to access ScriptableRenderer.m_RendererFeatures)

            List<T> rendererFeatures = new List<T>();

            UniversalAdditionalCameraData baseCameraData = this.baseCamera.GetUniversalAdditionalCameraData();

            List<Camera> cameras = new List<Camera>();
            cameras.Add(this.baseCamera);
            cameras.AddRange(baseCameraData.cameraStack);

            FieldInfo propertyInfo = typeof(ScriptableRenderer).GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
            if (propertyInfo == null) {
                return null;
            }

            for (int ic = 0, nc = cameras.Count; ic < nc; ic++) {
                UniversalAdditionalCameraData cameraData = cameras[ic].GetUniversalAdditionalCameraData();

                List<ScriptableRendererFeature> cameraRendererFeatures = propertyInfo.GetValue(cameraData.scriptableRenderer) as List<ScriptableRendererFeature>;
                if (cameraRendererFeatures == null) {
                    continue;
                }

                for (int ir = 0, nr = cameraRendererFeatures.Count; ir < nr; ir++) {
                    T rendererFeature = cameraRendererFeatures[ir] as T;
                    if (rendererFeature != null) {
                        rendererFeatures.Add(rendererFeature);
                    }
                }
            }

            return rendererFeatures;
        }

        private void BeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            // Create render texture (will only allocate once until released)
            CreateRenderTexture();

            // Collect relevant renderer features in camera stack
            if (this.drawStackUIRendererFeatures == null) {
                this.drawStackUIRendererFeatures = GetRendererFeatures<DrawStackUIRendererFeature>();
            }
            if (this.alphaBlendStackUIRendererFeatures == null) {
                this.alphaBlendStackUIRendererFeatures = GetRendererFeatures<AlphaBlendStackUIRendererFeature>();
            }

            // Set RTHandle/RenderTexture to renderer features
            for (int i = 0, n = this.drawStackUIRendererFeatures.Count; i < n; i++) {
                this.drawStackUIRendererFeatures[i].SetRTHandle(this.rtHandle);
            }
            for (int i = 0, n = this.alphaBlendStackUIRendererFeatures.Count; i < n; i++) {
                this.alphaBlendStackUIRendererFeatures[i].SetRenderTexture(this.renderTexture);
            }
        }

        private void EndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {

        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}