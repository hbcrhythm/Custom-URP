using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LinearToSrgbFeature : ScriptableRendererFeature
{
    class LineartoargbPass : ScriptableRenderPass
    {
        RenderTargetIdentifier cameraColorTexture;

        string profileTag;

        Material srgbtolinerMaterial;

        public LineartoargbPass(string profileTag)
        {
            this.profileTag = profileTag;
            var shader = Shader.Find("Hidden/Universal Render Pipeline/Blit");

            if (shader == null)
            {
                Debug.LogError("shader Blit not found");
                return;
            }
            srgbtolinerMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (srgbtolinerMaterial == null)
            {
                Debug.Log("Material not created");
                return;
            }

            if (renderingData.cameraData.resolveFinalTarget)
            {
                return;
            }

            cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;

            CommandBuffer cmd = CommandBufferPool.Get(profileTag);

            cmd.EnableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
            cmd.DisableShaderKeyword("_SRGB_TO_LINEAR_CONVERSION");

#if ENABLE_VR && ENABLE_XR_MODULE
            Material matetial = renderingData.cameraData.xr.enabled ? null : srgbtolinerMaterial;
#else
            Material matetial = srgbtolinerMaterial;
#endif

            cmd.Blit(cameraColorTexture, cameraColorTexture, matetial);
            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();

            CommandBufferPool.Release(cmd);

        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    LineartoargbPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new LineartoargbPass("LinearToSrgb");

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}