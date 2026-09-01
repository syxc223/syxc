using System.Collections;
using UnityEngine;

/// <summary>
/// 无文字版过场导演器：只用相机运动 + 纯色几何图形讲故事。
/// 你可以在不同场景里调用 PlayIntro / PlayHub / PlayPacmanBridge / PlayEnding。
/// </summary>
public class ShapeCutsceneDirector : MonoBehaviour
{
    [Header("通用")]
    public Camera mainCamera;
    public Transform player;
    public CanvasGroup fadeCanvas;          // 黑幕

    [Header("开场：碎片聚成心")]
    public Transform introFragmentsRoot;    // 一堆分散的小方块 / 三角形
    public Transform introHeart;            // 由 0/3/4 形状拼出的心形
    public float introAssembleDuration = 2f;
    public float introHoldDuration = 1.5f;

    [Header("中间：破碎心与门的提示")]
    public Transform hubBrokenHeart;        // 缺一角的心
    public Transform hubDoorSymbol;         // 圆形 + 矩形组成的“门”
    public float hubShowDuration = 2f;

    [Header("吃豆人桥段：迷宫符号")]
    public Transform pacmanMiniMaze;        // 顶视小迷宫（只用方块和圆点）
    public Transform pacmanDoorSymbol;      // 迷宫尽头的小门图形
    public float pacmanShowDuration = 2f;

    [Header("结尾：完整心脏脉动")]
    public Transform endingHeartFull;       // 完整的心形
    public float endingPulseDuration = 2f;
    public int endingPulseCount = 3;

    void Awake()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 1f;
        }
        SetAllInactive();
    }

    void SetAllInactive()
    {
        if (introFragmentsRoot != null) introFragmentsRoot.gameObject.SetActive(false);
        if (introHeart != null) introHeart.gameObject.SetActive(false);
        if (hubBrokenHeart != null) hubBrokenHeart.gameObject.SetActive(false);
        if (hubDoorSymbol != null) hubDoorSymbol.gameObject.SetActive(false);
        if (pacmanMiniMaze != null) pacmanMiniMaze.gameObject.SetActive(false);
        if (pacmanDoorSymbol != null) pacmanDoorSymbol.gameObject.SetActive(false);
        if (endingHeartFull != null) endingHeartFull.gameObject.SetActive(false);
    }

    // 开场：碎片散开 -> 聚成心 -> 淡出
    public void PlayIntroCutscene()
    {
        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        SetAllInactive();

        if (introFragmentsRoot != null) introFragmentsRoot.gameObject.SetActive(true);
        if (introHeart != null)
        {
            introHeart.gameObject.SetActive(true);
            introHeart.localScale = Vector3.zero;
        }

        yield return Fade(1f, 0f, 1f);

        // 碎片整体缓慢向心靠拢（你可以在编辑器里配合动画器做更细节的移动）
        float t = 0f;
        Vector3 fragStart = introFragmentsRoot != null ? introFragmentsRoot.position : Vector3.zero;
        Vector3 fragEnd = introHeart != null ? introHeart.position : fragStart;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(introAssembleDuration, 0.01f);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (introFragmentsRoot != null)
            {
                introFragmentsRoot.position = Vector3.Lerp(fragStart, fragEnd, eased);
            }

            if (introHeart != null)
            {
                introHeart.localScale = Vector3.one * eased;
            }

            yield return null;
        }

        yield return new WaitForSeconds(introHoldDuration);
        yield return Fade(0f, 1f, 1f);
        SetAllInactive();
    }

    // 2D/3D 解谜和吃豆人关卡之间的过场：破碎的心 + 门
    public void PlayHubCutscene()
    {
        StartCoroutine(HubRoutine());
    }

    IEnumerator HubRoutine()
    {
        SetAllInactive();
        if (hubBrokenHeart != null) hubBrokenHeart.gameObject.SetActive(true);
        if (hubDoorSymbol != null) hubDoorSymbol.gameObject.SetActive(true);

        yield return Fade(1f, 0f, 0.5f);
        yield return new WaitForSeconds(hubShowDuration);
        yield return Fade(0f, 1f, 0.5f);
        SetAllInactive();
    }

    // 吃豆人关卡前后使用的小过场：小迷宫 + 门符号
    public void PlayPacmanBridge()
    {
        StartCoroutine(PacmanBridgeRoutine());
    }

    IEnumerator PacmanBridgeRoutine()
    {
        SetAllInactive();
        if (pacmanMiniMaze != null) pacmanMiniMaze.gameObject.SetActive(true);
        if (pacmanDoorSymbol != null) pacmanDoorSymbol.gameObject.SetActive(true);

        yield return Fade(1f, 0f, 0.5f);
        yield return new WaitForSeconds(pacmanShowDuration);
        yield return Fade(0f, 1f, 0.5f);
        SetAllInactive();
    }

    // 最终结尾：完整的心脏缓慢脉动几次，然后淡出
    public void PlayEndingCutscene()
    {
        StartCoroutine(EndingRoutine());
    }

    IEnumerator EndingRoutine()
    {
        SetAllInactive();
        if (endingHeartFull != null)
        {
            endingHeartFull.gameObject.SetActive(true);
            endingHeartFull.localScale = Vector3.one;
        }

        yield return Fade(1f, 0f, 1f);

        if (endingHeartFull != null)
        {
            for (int i = 0; i < endingPulseCount; i++)
            {
                yield return Pulse(endingHeartFull, endingPulseDuration);
            }
        }

        yield return Fade(0f, 1f, 1f);
        SetAllInactive();
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvas == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            fadeCanvas.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        fadeCanvas.alpha = to;
    }

    IEnumerator Pulse(Transform target, float duration)
    {
        if (target == null) yield break;

        float half = duration * 0.5f;
        float t = 0f;
        Vector3 baseScale = Vector3.one;
        Vector3 bigScale = baseScale * 1.15f;

        // 放大
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            target.localScale = Vector3.Lerp(baseScale, bigScale, k);
            yield return null;
        }

        // 缩回
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            target.localScale = Vector3.Lerp(bigScale, baseScale, k);
            yield return null;
        }

        target.localScale = baseScale;
    }
}

