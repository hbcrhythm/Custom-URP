using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class PlanarShadowFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PlanarShadowSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public Material planarShadowMaterial = null;

        public RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
        public LayerMask layerMask = -1;

        //shadow data
        public Vector3 lightDir = new Vector4(-0.5f, -0.7f, 0.5f);
        public Color color = new Color(0, 0, 0, 170 / 255f);
        public float falloff = 0.2f;


    }

    public PlanarShadowSettings setting = new PlanarShadowSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        //string profilerTag;

        public Material planarShadowMaterial;
        public FilteringSettings m_FilteringSettings;

        //shadow data
        public Vector3 lightDir = new Vector4(-0.5f, -0.7f, 0.5f);
        public Color color = new Color(0, 0, 0, 170 / 255f);
        public float falloff = 0.2f;

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        public CustomRenderPass(string profilerTag)
        {
            //m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            //m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            //m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));

            //this.profilerTag = profilerTag;
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
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(CustomConfig.CustomProfileId.PlanarShadow)))
            {
                // cmd.SetGlobalVector("_WorldPos", new Vector3(0, 0, 0));
                // cmd.SetGlobalVector("_ShadowProjDir", lightDir);
                // cmd.SetGlobalVector("_ShadowPlane", new Vector4(0, 1, 0, 0));

                // cmd.SetGlobalColor("_ShadowColor", color);
                // cmd.SetGlobalFloat("_ShadowFalloff", falloff);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
                drawSettings.overrideMaterial = planarShadowMaterial;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass("PlanarShadow");
        m_ScriptablePass.planarShadowMaterial = setting.planarShadowMaterial;
        m_ScriptablePass.m_FilteringSettings = new FilteringSettings(setting.renderQueueRange, setting.layerMask);
        m_ScriptablePass.lightDir = setting.lightDir;
        m_ScriptablePass.color = setting.color;
        m_ScriptablePass.falloff = setting.falloff;

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = setting.renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


