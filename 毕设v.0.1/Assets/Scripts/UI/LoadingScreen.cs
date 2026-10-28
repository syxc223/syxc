using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen loading overlay shown during Netcode scene transitions.
/// Progress bar is cosmetic (Netcode exposes no load progress); it animates
/// to 90% while waiting and snaps to 100% when the scene load completes.
/// Singleton — DontDestroyOnLoad.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("References")]
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] Image _background;
    [SerializeField] Image _darken;
    [SerializeField] Slider _progressBar;
    [SerializeField] TMP_Text _progressText;
    [SerializeField] TMP_Text _tipText;
    [SerializeField] TMP_Text _titleText;

    [Header("Settings")]
    [SerializeField] float _fadeIn = 0.35f;
    [SerializeField] float _fadeOut = 0.45f;
    [SerializeField, TextArea] string[] _tips =
    {
        "提示：一人踩住压力板，另一人趁机通过大门。",
        "提示：通过大门后，踩住另一侧压力板让同伴过来。",
        "提示：按 T 打开聊天与队友交流。",
        "提示：按 ESC 打开暂停菜单，可以退出房间。",
        "提示：两人同时到达各自时空的出口即可通关。",
    };

    Coroutine _activeRoutine;
    float _displayProgress;
    float _targetProgress;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Show the overlay and start the cosmetic progress animation.
    /// The actual scene load is driven by Netcode; caller hides via Complete().
    /// </summary>
    public void Begin(string title)
    {
        // Coroutines cannot start on an inactive GameObject — activate first
        // (the object self-deactivates in Awake and after Complete()).
        gameObject.SetActive(true);
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(BeginRoutine(title));
    }

    /// <summary>
    /// Called when Netcode OnLoadComplete fires for the local client.
    /// Fills the bar to 100% and fades out.
    /// </summary>
    public void Complete()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(CompleteRoutine());
    }

    IEnumerator BeginRoutine(string title)
    {
        if (_titleText != null) _titleText.text = title;
        if (_tipText != null && _tips.Length > 0)
            _tipText.text = _tips[Random.Range(0, _tips.Length)];

        _displayProgress = 0f;
        _targetProgress = 0.9f;
        if (_progressBar != null) _progressBar.value = 0f;
        if (_progressText != null) _progressText.text = "0%";

        // Reset alpha so the fade-in starts from transparent
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;

        gameObject.SetActive(true);
        if (_canvasGroup != null) _canvasGroup.blocksRaycasts = true;
        yield return Fade(1f, _fadeIn);

        // Cosmetic progress: crawl toward 90%, slow near the end
        while (_displayProgress < 0.895f)
        {
            float speed = _displayProgress < 0.6f ? 0.35f : 0.12f;
            _displayProgress = Mathf.MoveTowards(_displayProgress, _targetProgress, speed * Time.unscaledDeltaTime);
            if (_progressBar != null) _progressBar.value = _displayProgress;
            if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(_displayProgress * 100)}%";
            yield return null;
        }
    }

    IEnumerator CompleteRoutine()
    {
        // Snap to full
        _displayProgress = 1f;
        if (_progressBar != null) _progressBar.value = 1f;
        if (_progressText != null) _progressText.text = "100%";
        yield return new WaitForSecondsRealtime(0.35f);

        yield return Fade(0f, _fadeOut);

        gameObject.SetActive(false);
        if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;
    }

    IEnumerator Fade(float target, float duration)
    {
        if (_canvasGroup == null) yield break;

        float start = _canvasGroup.alpha;
        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        _canvasGroup.alpha = target;
    }

    public void SetBackground(Sprite sprite)
    {
        if (_background != null)
            _background.sprite = sprite;
    }
}
