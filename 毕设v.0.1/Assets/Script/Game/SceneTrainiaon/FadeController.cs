using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全屏淡入淡出控制器。
/// 自动创建 Canvas + 全屏黑底 Image，适用于 URP / Built-in 所有渲染管线。
/// 无需预制体、无需 Shader Graph。
/// </summary>
public class FadeController : Singleton<FadeController>
{
    [Header("Fade Panel (手动赋值可覆盖自动创建)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image panelImage;

    [Header("Timing")]
    [SerializeField, Range(0.05f, 3f)] private float fadeDuration = 0.35f;

    private Coroutine _fadeRoutine;
    private float _nextFadeOutDurationOverride = -1f;
    private float _nextFadeInDurationOverride = -1f;
    private FadeStyle _currentStyle = FadeStyle.PanelFade;

    protected override void Awake()
    {
        base.Awake();

        // 如果未手动赋值，则运行时自动创建 Canvas + Image
        if (panelRoot == null || panelImage == null)
        {
            CreateFadeCanvas();
        }
        else
        {
            panelImage.enabled = false;
            SetAlpha(0);
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 运行时自动创建一个独立的 Screen Space Overlay Canvas 及全屏黑底 Image。
    /// </summary>
    private void CreateFadeCanvas()
    {
        // 创建 Canvas GameObject
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // 确保在所有 UI 之上

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        panelRoot = canvasGo;

        // 创建全屏黑底 Image
        var imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        panelImage = imageGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);
        panelImage.raycastTarget = false; // 不阻挡点击
        panelImage.enabled = false;

        // 锚点拉伸铺满全屏
        var rect = panelImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 设置当前使用的淡入淡出样式。
    /// 目前仅 PanelFade 完整实现；WipeFade 预留扩展点。
    /// </summary>
    public void SetStyle(FadeStyle style)
    {
        _currentStyle = style;
        // WipeFade 的扩展点：未来可在此切换到 Wipe 专用的 Canvas/Shader。
    }

    /// <summary>
    /// 为下一次淡入/淡出设置覆盖持续时间（单次消费）。
    /// </summary>
    public void SetNextFadeDurations(float fadeInDuration, float fadeOutDuration)
    {
        _nextFadeInDurationOverride = fadeInDuration > 0f ? fadeInDuration : -1f;
        _nextFadeOutDurationOverride = fadeOutDuration > 0f ? fadeOutDuration : -1f;
    }

    /// <summary>
    /// 淡出到全黑（alpha: 0 → 1）。
    /// </summary>
    public void FadeOut(Action onDone = null)
    {
        StartFade(targetAlpha: 1f, duration: ResolveDuration(isFadeOut: true), onDone);
    }

    /// <summary>
    /// 淡入到透明（alpha: 1 → 0）。
    /// </summary>
    public void FadeIn(Action onDone = null)
    {
        StartFade(targetAlpha: 0f, duration: ResolveDuration(isFadeOut: false), onDone);
    }

    private void StartFade(float targetAlpha, float duration, Action onDone)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }
        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, onDone));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onDone)
    {
        panelImage.enabled = true;

        float startAlpha = panelImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetAlpha(alpha);
            elapsed += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime，防止 timescale=0 时卡住
            yield return null;
        }

        SetAlpha(targetAlpha);

        // 淡入完成后隐藏 Image，避免阻挡 raycast（虽然已关闭 raycastTarget，但仍影响渲染）
        if (targetAlpha <= 0.001f)
        {
            panelImage.enabled = false;
        }

        onDone?.Invoke();
        _fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = panelImage.color;
        color.a = alpha;
        panelImage.color = color;
    }

    private float ResolveDuration(bool isFadeOut)
    {
        if (isFadeOut && _nextFadeOutDurationOverride >= 0f)
        {
            float value = _nextFadeOutDurationOverride;
            _nextFadeOutDurationOverride = -1f;
            return value;
        }

        if (!isFadeOut && _nextFadeInDurationOverride >= 0f)
        {
            float value = _nextFadeInDurationOverride;
            _nextFadeInDurationOverride = -1f;
            return value;
        }

        return fadeDuration;
    }
}
