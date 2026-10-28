using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game HUD: puzzle name, player indicator, portal cooldown, puzzle complete overlay.
/// ESC opens pause menu with Exit Room option.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("References")]
    [SerializeField] TMP_Text _puzzleNameText;
    [SerializeField] TMP_Text _playerLabel;
    [SerializeField] TMP_Text _hintText;
    [SerializeField] GameObject _completeOverlay;
    [SerializeField] TMP_Text _completeText;
    [SerializeField] Slider _masterVolumeSlider;
    [SerializeField] Slider _sfxVolumeSlider;
    [SerializeField] Slider _bgmVolumeSlider;
    [SerializeField] CanvasGroup _hudCanvasGroup;

    [Header("Pause Menu")]
    [SerializeField] GameObject _pausePanel;
    [SerializeField] Button _exitRoomButton;
    [SerializeField] Button _resumeButton;
    [SerializeField] Button _settingsButton;

    [Header("Settings Panel")]
    [SerializeField] GameObject _settingsPanel;
    [SerializeField] TMP_Dropdown _resolutionDropdown;
    [SerializeField] Toggle _fullscreenToggle;
    [SerializeField] Slider _settingsMasterSlider;
    [SerializeField] Slider _settingsSfxSlider;
    [SerializeField] Slider _settingsBgmSlider;
    [SerializeField] Button _settingsBackButton;

    [Header("FPS Counter")]
    [SerializeField] TMP_Text _fpsText;

    bool _overlayShowing;
    bool _paused;

    /// <summary>True while the pause menu is open — chat and other input should stay closed.</summary>
    public static bool IsPaused { get; private set; }

    // FPS tracking
    float _fpsUpdateInterval = 0.5f;
    float _fpsTimer;
    int _frameCount;
    Resolution[] _resolutions;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        RegisterListeners();
    }

    void OnDisable()
    {
        UnregisterListeners();
    }

    void RegisterListeners()
    {
        if (_exitRoomButton != null)
        {
            _exitRoomButton.onClick.RemoveAllListeners();
            _exitRoomButton.onClick.AddListener(OnExitRoom);
        }
        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveAllListeners();
            _resumeButton.onClick.AddListener(OnResume);
        }
        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.AddListener(OnSettings);
        }
        if (_settingsBackButton != null)
        {
            _settingsBackButton.onClick.RemoveAllListeners();
            _settingsBackButton.onClick.AddListener(OnSettingsBack);
        }
    }

    void UnregisterListeners()
    {
        if (_exitRoomButton != null) _exitRoomButton.onClick.RemoveAllListeners();
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
        if (_settingsButton != null) _settingsButton.onClick.RemoveAllListeners();
        if (_settingsBackButton != null) _settingsBackButton.onClick.RemoveAllListeners();
    }

    void Start()
    {
        // The HUD canvas CanvasGroup is serialized with BlocksRaycasts=0 in the scene
        // (so the game world stays clickable), but that also disables the pause menu
        // buttons and the chat input. Force raycasts on for UI interaction.
        if (_hudCanvasGroup != null)
            _hudCanvasGroup.blocksRaycasts = true;

        if (_completeOverlay != null)
            _completeOverlay.SetActive(false);

        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);

        // --- Audio sliders (in-game HUD) ---
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.value = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f;
            _masterVolumeSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetMasterVolume(v);
                if (_settingsMasterSlider != null) _settingsMasterSlider.value = v;
            });
        }
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.8f;
            _sfxVolumeSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetSfxVolume(v);
                if (_settingsSfxSlider != null) _settingsSfxSlider.value = v;
            });
        }
        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.value = AudioManager.Instance != null ? AudioManager.Instance.BgmVolume : 0.5f;
            _bgmVolumeSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetBgmVolume(v);
                if (_settingsBgmSlider != null) _settingsBgmSlider.value = v;
            });
        }

        // --- Settings panel audio sliders (synced with HUD sliders) ---
        if (_settingsMasterSlider != null)
        {
            _settingsMasterSlider.value = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f;
            _settingsMasterSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetMasterVolume(v);
                if (_masterVolumeSlider != null) _masterVolumeSlider.value = v;
            });
        }
        if (_settingsSfxSlider != null)
        {
            _settingsSfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.8f;
            _settingsSfxSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetSfxVolume(v);
                if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = v;
            });
        }
        if (_settingsBgmSlider != null)
        {
            _settingsBgmSlider.value = AudioManager.Instance != null ? AudioManager.Instance.BgmVolume : 0.5f;
            _settingsBgmSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetBgmVolume(v);
                if (_bgmVolumeSlider != null) _bgmVolumeSlider.value = v;
            });
        }

        InitResolutionDropdown();

        RegisterListeners();

        // Hide initially — GameManager activates this when GameScene loads
        gameObject.SetActive(false);
    }

    void InitResolutionDropdown()
    {
        if (_resolutionDropdown == null) return;

        _resolutions = Screen.resolutions;
        _resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int currentIdx = 0;
        for (int i = 0; i < _resolutions.Length; i++)
        {
            int hz = Mathf.RoundToInt((float)_resolutions[i].refreshRateRatio.value);
            string label = $"{_resolutions[i].width} × {_resolutions[i].height} @ {hz}Hz";
            options.Add(label);

            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height &&
                Mathf.Approximately((float)_resolutions[i].refreshRateRatio.value, (float)Screen.currentResolution.refreshRateRatio.value))
                currentIdx = i;
        }
        _resolutionDropdown.AddOptions(options);
        _resolutionDropdown.value = currentIdx;
        _resolutionDropdown.RefreshShownValue();
        _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.isOn = Screen.fullScreen;
            _fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }
    }

    void OnResolutionChanged(int idx)
    {
        if (idx < 0 || idx >= _resolutions.Length) return;
        var res = _resolutions[idx];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    void OnFullscreenToggled(bool isOn)
    {
        Screen.fullScreen = isOn;
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    public void SetPuzzleName(string name)
    {
        if (_puzzleNameText != null)
            _puzzleNameText.text = name;
    }

    public void SetPlayerLabel(string label)
    {
        if (_playerLabel != null)
            _playerLabel.text = label;
    }

    string _hintBase = "";
    Coroutine _toastRoutine;

    public void SetHint(string hint)
    {
        _hintBase = hint;
        if (_hintText != null)
            _hintText.text = hint;
    }

    /// <summary>
    /// Show a temporary message over the hint text; reverts to the base hint afterwards.
    /// </summary>
    public void ShowToast(string message, float duration = 2.5f)
    {
        if (_hintText == null) return;

        if (_toastRoutine != null)
        {
            StopCoroutine(_toastRoutine);
            _toastRoutine = null;
        }

        _hintText.text = message;
        _toastRoutine = StartCoroutine(RestoreHintAfter(duration));
    }

    System.Collections.IEnumerator RestoreHintAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (_hintText != null)
            _hintText.text = _hintBase;
        _toastRoutine = null;
    }

    public void ShowPuzzleComplete(string puzzleName)
    {
        if (_completeOverlay == null) return;

        _completeOverlay.SetActive(true);
        if (_completeText != null)
            _completeText.text = $"{puzzleName}\n<size=24>Complete!</size>";

        var cg = _completeOverlay.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            StartCoroutine(FadeIn(cg, 0.5f));
        }

        _overlayShowing = true;
    }

    public void HidePuzzleComplete()
    {
        if (_completeOverlay == null) return;
        _completeOverlay.SetActive(false);
        _overlayShowing = false;
    }

    System.Collections.IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = t / duration;
            yield return null;
        }
        cg.alpha = 1f;
    }

    void Update()
    {
        if (ChatSystem.IsChatOpen) return;

        // ESC — toggle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_paused)
                OnResume();
            else
                OnPause();
            return;
        }

        if (_paused) return;

        // Toggle HUD with Tab
        if (Input.GetKeyDown(KeyCode.Tab) && _hudCanvasGroup != null)
        {
            bool show = _hudCanvasGroup.alpha <= 0.5f;
            _hudCanvasGroup.alpha = show ? 1f : 0f;
            _hudCanvasGroup.blocksRaycasts = show; // hidden HUD must not block clicks
        }

        // Hide complete overlay with any key after 3 seconds
        if (_overlayShowing && Input.anyKeyDown && _completeOverlay.activeSelf)
        {
            var cg = _completeOverlay.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha >= 1f)
                HidePuzzleComplete();
        }

        UpdateFPS();
    }

    void UpdateFPS()
    {
        if (_fpsText == null) return;

        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= _fpsUpdateInterval)
        {
            int fps = Mathf.RoundToInt(_frameCount / _fpsTimer);
            _fpsText.text = $"{fps} FPS";
            _fpsText.color = fps >= 50 ? Color.green : fps >= 30 ? Color.yellow : Color.red;
            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }

    void OnPause()
    {
        _paused = true;
        IsPaused = true;
        if (_pausePanel != null) _pausePanel.SetActive(true);
        // The pause menu must be visible and clickable even if the HUD was hidden with Tab
        if (_hudCanvasGroup != null)
        {
            _hudCanvasGroup.alpha = 1f;
            _hudCanvasGroup.blocksRaycasts = true;
        }
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    void OnSettings()
    {
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    void OnSettingsBack()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(true);
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    void OnResume()
    {
        _paused = false;
        IsPaused = false;
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        AudioManager.Instance?.PlaySfx("UIClick");
    }

    void OnExitRoom()
    {
        _paused = false;
        IsPaused = false;
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        AudioManager.Instance?.PlaySfx("UIClick");
        GameManager.Instance?.ExitRoom();
    }
}
