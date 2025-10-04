using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelizePass : ScriptableRenderPass
{
    private readonly PixelizeFeature.CustomPassSettings settings;
    private Material material;

    // RTHandles вместо RenderTargetIdentifier / GetTemporaryRT
    private RTHandle cameraColor;     // текущий цветовой таргет камеры
    private RTHandle pixelRT;         // низкорезовая RT

    private int pixelW, pixelH;

    public PixelizePass(PixelizeFeature.CustomPassSettings settings)
    {
        this.settings = settings;
        renderPassEvent = settings.renderPassEvent;

        if (material == null)
            material = CoreUtils.CreateEngineMaterial("Hidden/Pixelize");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // целевой буфер камеры теперь RTHandle
        cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;

        // вычисляем целевое «пиксельное» разрешение
        pixelH = settings.screenHeight;
        pixelW = Mathf.Max(1, Mathf.RoundToInt(pixelH * renderingData.cameraData.camera.aspect));

        material.SetVector("_BlockCount",    new Vector2(pixelW, pixelH));
        material.SetVector("_BlockSize",     new Vector2(1f / pixelW, 1f / pixelH));
        material.SetVector("_HalfBlockSize", new Vector2(0.5f / pixelW, 0.5f / pixelH));

        // аллоцируем временную RT через RTHandles
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.msaaSamples = 1;
        desc.depthBufferBits = 0;
        desc.width  = pixelW;
        desc.height = pixelH;
        desc.useMipMap = false;
        desc.autoGenerateMips = false;

        // важно: Point-фильтрация
        pixelRT = RTHandles.Alloc(
            desc.width, desc.height, depthBufferBits: 0,
            colorFormat: desc.graphicsFormat,
            filterMode: FilterMode.Point,
            wrapMode: TextureWrapMode.Clamp,
            dimension: TextureDimension.Tex2D,
            name: "_PixelRT"
        );
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get("Pixelize Pass");
        using (new ProfilingScope(cmd, new ProfilingSampler("Pixelize Pass")))
        {
            // ↓ вместо устаревших Blit(...) используем Blitter и RTHandle-цели

            // 1) downsample в низкорезовую RT с материалом (пост-эффект)
            Blitter.BlitCameraTexture(cmd, cameraColor, pixelRT, material, 0);

            // 2) upsample назад в цветовой таргет камеры (point)
            Blitter.BlitCameraTexture(cmd, pixelRT, cameraColor);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // освобождаем временную RT
        if (pixelRT != null)
        {
            RTHandles.Release(pixelRT);
            pixelRT = null;
        }
    }
}
