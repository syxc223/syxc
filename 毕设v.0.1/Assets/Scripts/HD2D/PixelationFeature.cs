using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelationFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public float pixelSize = 4f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private PixelationPass _pass;

    public override void Create()
    {
        _pass = new PixelationPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass != null)
            renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }

    class PixelationPass : ScriptableRenderPass
    {
        private readonly Settings _settings;
        private Material _material;
        private RTHandle _tempTexture;

        private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");

        public PixelationPass(Settings settings)
        {
            _settings = settings;
            var shader = Shader.Find("Hidden/Pixelation");
            if (shader != null)
                _material = CoreUtils.CreateEngineMaterial(shader);
            renderPassEvent = _settings.renderPassEvent;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempTexture, desc, name: "_TempPixelationRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            var cmd = CommandBufferPool.Get("Pixelation");
            var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            _material.SetFloat(PixelSizeID, _settings.pixelSize);

            Blitter.BlitCameraTexture(cmd, cameraColorTarget, _tempTexture, _material, 0);
            Blitter.BlitCameraTexture(cmd, _tempTexture, cameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            _tempTexture?.Release();
            _tempTexture = null;
            if (_material != null)
            {
                CoreUtils.Destroy(_material);
                _material = null;
            }
        }
    }
}
