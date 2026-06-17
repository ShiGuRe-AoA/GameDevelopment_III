using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material maskMaterial;
        public Material compositeMaterial;

        [ColorUsage(true, true)]
        public Color outlineColor = Color.cyan;

        [Range(1f, 10f)]
        public float thickness = 2f;

        public bool useDiagonal = true;

        [Range(0f, 1f)]
        public float alphaCutoff = 0.01f;

        public bool renderWhenNoTargets = false;
    }

    public Settings settings = new Settings();

    private MaskPass maskPass;
    private CompositePass compositePass;

    private RTHandle maskTexture;
    private RTHandle tempTexture;

    public override void Create()
    {
        maskPass = new MaskPass(this)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };

        compositePass = new CompositePass(this)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void SetupRenderPasses(
        ScriptableRenderer renderer,
        in RenderingData renderingData)
    {
        compositePass.SetCameraColorTarget(renderer.cameraColorTargetHandle);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (settings.maskMaterial == null || settings.compositeMaterial == null)
            return;

        if (!settings.renderWhenNoTargets && OutlineRegistry.Count == 0)
            return;

        renderer.EnqueuePass(maskPass);
        renderer.EnqueuePass(compositePass);
    }

    protected override void Dispose(bool disposing)
    {
        maskTexture?.Release();
        tempTexture?.Release();
    }

    private class MaskPass : ScriptableRenderPass
    {
        private readonly OutlineRenderFeature feature;

        public MaskPass(OutlineRenderFeature feature)
        {
            this.feature = feature;
        }

        public override void OnCameraSetup(
            CommandBuffer cmd,
            ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc =
                renderingData.cameraData.cameraTargetDescriptor;

            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(
                ref feature.maskTexture,
                desc,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: "_ScreenSpaceOutlineMask"
            );

            ConfigureTarget(feature.maskTexture);
            ConfigureClear(ClearFlag.Color, Color.clear);
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            Material mat = feature.settings.maskMaterial;

            if (mat == null)
                return;

            CommandBuffer cmd =
                CommandBufferPool.Get("Screen Space Outline Mask");

            mat.SetFloat("_AlphaCutoff", feature.settings.alphaCutoff);

            OutlineRegistry.CleanupNulls();

            var renderers = OutlineRegistry.Renderers;

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer r = renderers[i];

                if (r == null)
                    continue;

                if (!r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                cmd.DrawRenderer(r, mat, 0, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private class CompositePass : ScriptableRenderPass
    {
        private readonly OutlineRenderFeature feature;

        private RTHandle cameraColorTarget;

        public CompositePass(OutlineRenderFeature feature)
        {
            this.feature = feature;

            // 需要读取当前相机颜色
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public void SetCameraColorTarget(RTHandle target)
        {
            cameraColorTarget = target;
        }

        public override void OnCameraSetup(
            CommandBuffer cmd,
            ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc =
                renderingData.cameraData.cameraTargetDescriptor;

            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(
                ref feature.tempTexture,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_ScreenSpaceOutlineTemp"
            );
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (cameraColorTarget == null)
                return;

            if (feature.maskTexture == null || feature.tempTexture == null)
                return;

            Material mat = feature.settings.compositeMaterial;

            if (mat == null)
                return;

            RenderTextureDescriptor desc =
                renderingData.cameraData.cameraTargetDescriptor;

            mat.SetTexture("_OutlineMask", feature.maskTexture);
            mat.SetColor("_OutlineColor", feature.settings.outlineColor);
            mat.SetFloat("_Thickness", feature.settings.thickness);
            mat.SetFloat("_UseDiagonal", feature.settings.useDiagonal ? 1f : 0f);

            mat.SetVector(
                "_OutlineTexelSize",
                new Vector4(
                    1f / desc.width,
                    1f / desc.height,
                    desc.width,
                    desc.height
                )
            );

            CommandBuffer cmd =
                CommandBufferPool.Get("Screen Space Outline Composite");

            Blitter.BlitCameraTexture(
                cmd,
                cameraColorTarget,
                feature.tempTexture,
                mat,
                0
            );

            Blitter.BlitCameraTexture(
                cmd,
                feature.tempTexture,
                cameraColorTarget
            );

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}