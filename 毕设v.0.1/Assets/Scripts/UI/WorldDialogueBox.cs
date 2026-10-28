using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Bottom-screen dialogue box for world interactions: object investigations, NPC lines,
/// narration. Lives under GameHUDCanvas (DontDestroyOnLoad), so it exists in every scene.
/// New lines replace the current one and the box fades out automatically.
/// Visibility is driven purely by the CanvasGroup alpha so this component's Awake
/// always runs at scene load (the GameObject stays active with alpha 0).
/// </summary>
public class WorldDialogueBox : MonoBehaviour
{
    public static WorldDialogueBox Instance { get; private set; }

    [Header("References")]
    [SerializeField] GameObject _nameplate;
    [SerializeField] TMP_Text _speakerName;
    [SerializeField] TMP_Text _dialogueText;
    [SerializeField] CanvasGroup _canvasGroup;

    [Header("Behaviour")]
    [SerializeField] float _fadeIn = 0.15f;
    [SerializeField] float _fadeOut = 0.3f;

    Coroutine _currentRoutine;

    void Awake()
    {
        Instance = this;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Show a line. speaker = name for the nameplate ("P1"/"神秘旅人"/…);
    /// empty speaker means narration (nameplate hidden).
    /// </summary>
    public static void Show(string speaker, string text, float duration = 4.5f)
    {
        if (Instance != null)
            Instance.ShowLine(speaker, text, duration);
        else
            GameHUD.Instance?.ShowToast(string.IsNullOrEmpty(speaker) ? text : $"{speaker}：{text}", duration);
    }

    public void ShowLine(string speaker, string text, float duration)
    {
        if (_speakerName != null)
            _speakerName.text = speaker;
        if (_nameplate != null)
            _nameplate.SetActive(!string.IsNullOrEmpty(speaker));
        if (_dialogueText != null)
            _dialogueText.text = text;

        if (_currentRoutine != null)
            StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(ShowAndFadeRoutine(duration));
    }

    IEnumerator ShowAndFadeRoutine(float duration)
    {
        float t = 0f;
        while (t < _fadeIn)
        {
            t += Time.unscaledDeltaTime;
            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Clamp01(t / _fadeIn);
            yield return null;
        }
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(duration);

        t = 0f;
        while (t < _fadeOut)
        {
            t += Time.unscaledDeltaTime;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f - Mathf.Clamp01(t / _fadeOut);
            yield return null;
        }
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        _currentRoutine = null;
    }
}
