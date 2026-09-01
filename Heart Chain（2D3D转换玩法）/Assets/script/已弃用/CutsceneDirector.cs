using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 简单过场导演器：负责开场动画、章节过场、结尾动画的播放。
/// 不依赖 Timeline，用插值和 UI 文本 + 摄像机运动实现。
/// </summary>
public class CutsceneDirector : MonoBehaviour
{
    [Header("数据")]
    public StoryBeats storyBeats;

    [Header("引用")]
    public Camera mainCamera;
    public Transform player;
    public TextMeshProUGUI subtitleText;
    public CanvasGroup fadeCanvas;           // 全屏黑幕，用于淡入淡出

    [Header("参数")]
    public float fadeDuration = 1f;
    public float textShowDuration = 4f;

    void Start()
    {
        // 可在第一个场景勾选“在 Start 时播放开场”
        if (storyBeats != null && fadeCanvas != null && subtitleText != null)
        {
            StartCoroutine(PlayIntro());
        }
    }

    #region 开场动画（第一关加载时）
    IEnumerator PlayIntro()
    {
        // 1. 黑幕全黑
        fadeCanvas.alpha = 1f;
        subtitleText.text = string.Empty;

        yield return new WaitForSeconds(0.5f);

        // 2. 显示引导文字
        subtitleText.text = storyBeats.introText;
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration)); // 从黑淡入

        yield return new WaitForSeconds(textShowDuration);

        // 3. 淡出文字 / 再次淡入游戏
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        subtitleText.text = string.Empty;
    }
    #endregion

    #region 章节过场（在 2D/3D 解谜与吃豆人章节之间）
    public void PlayHubCutscene()
    {
        if (storyBeats == null || fadeCanvas == null || subtitleText == null) return;
        StartCoroutine(HubCutsceneRoutine());
    }

    IEnumerator HubCutsceneRoutine()
    {
        fadeCanvas.alpha = 0f;
        subtitleText.text = storyBeats.hubText;
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSeconds(textShowDuration);
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        subtitleText.text = string.Empty;
    }

    public void PlayPacmanChapterCutscene()
    {
        if (storyBeats == null || fadeCanvas == null || subtitleText == null) return;
        StartCoroutine(PacmanChapterRoutine());
    }

    IEnumerator PacmanChapterRoutine()
    {
        fadeCanvas.alpha = 0f;
        subtitleText.text = storyBeats.pacmanChapterText;
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSeconds(textShowDuration);
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        subtitleText.text = string.Empty;
    }
    #endregion

    #region 结尾动画（最后一关结束）
    public void PlayEnding(string nextSceneName = "")
    {
        if (storyBeats == null || fadeCanvas == null || subtitleText == null)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            return;
        }

        StartCoroutine(EndingRoutine(nextSceneName));
    }

    IEnumerator EndingRoutine(string nextSceneName)
    {
        fadeCanvas.alpha = 0f;
        subtitleText.text = storyBeats.reconnectText;
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        yield return new WaitForSeconds(textShowDuration);

        subtitleText.text = storyBeats.endingText;
        yield return new WaitForSeconds(textShowDuration + 1f);

        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
    #endregion

    IEnumerator Fade(float from, float to, float duration)
    {
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
}

