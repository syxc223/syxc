using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置管理器
/// 管理游戏设置（音量、难度等）
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("音量设置UI")]
    [SerializeField] private Slider masterVolumeSlider; // 主音量滑块
    [SerializeField] private Slider musicVolumeSlider; // 音乐音量滑块
    [SerializeField] private Slider sfxVolumeSlider; // 音效音量滑块
    [SerializeField] private Text masterVolumeText; // 主音量文本（可选，显示百分比）
    [SerializeField] private Text musicVolumeText; // 音乐音量文本（可选）
    [SerializeField] private Text sfxVolumeText; // 音效音量文本（可选）
    
    [Header("难度设置UI")]
    [SerializeField] private Dropdown difficultyDropdown; // 难度下拉菜单（Legacy UI）
    [SerializeField] private Text difficultyText; // 难度文本（可选，显示当前难度）
    
    [Header("设置面板")]
    [SerializeField] private GameObject settingsPanel; // 设置面板（用于显示/隐藏）
    
    [Header("默认值")]
    [SerializeField] private float defaultMasterVolume = 1.0f; // 默认主音量
    [SerializeField] private float defaultMusicVolume = 0.7f; // 默认音乐音量
    [SerializeField] private float defaultSFXVolume = 1.0f; // 默认音效音量
    
    private const string MASTER_VOLUME_PREF = "MasterVolume";
    private const string MUSIC_VOLUME_PREF = "MusicVolume";
    private const string SFX_VOLUME_PREF = "SFXVolume";
    
    private void Awake()
    {
        // 加载难度设置
        DifficultyManager.LoadDifficulty();
    }
    
    private void Start()
    {
        // 初始化UI
        InitializeVolumeSliders();
        InitializeDifficultyDropdown();
        
        // 加载保存的设置
        LoadSettings();
    }
    
    /// <summary>
    /// 初始化音量滑块
    /// </summary>
    private void InitializeVolumeSliders()
    {
        // 主音量滑块
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        // 音乐音量滑块
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        // 音效音量滑块
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }
    
    /// <summary>
    /// 初始化难度下拉菜单
    /// </summary>
    private void InitializeDifficultyDropdown()
    {
        // 使用Legacy Dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> 
            { 
                "简单", 
                "普通", 
                "困难" 
            });
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }
    }
    
    /// <summary>
    /// 应用音量设置到AudioManager
    /// </summary>
    private void ApplyVolumeSettings()
    {
        if (AudioManager.Instance == null) return;
        
        float masterVol = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;
        float musicVol = musicVolumeSlider != null ? musicVolumeSlider.value : 1f;
        float sfxVol = sfxVolumeSlider != null ? sfxVolumeSlider.value : 1f;
        
        // 计算实际音量（主音量 * 各自音量）
        float actualMusicVolume = masterVol * musicVol;
        float actualSFXVolume = masterVol * sfxVol;
        
        AudioManager.Instance.SetMusicVolume(actualMusicVolume);
        AudioManager.Instance.SetSFXVolume(actualSFXVolume);
    }
    
    /// <summary>
    /// 主音量改变回调
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        // 应用音量设置
        ApplyVolumeSettings();
        
        // 更新文本显示
        if (masterVolumeText != null)
        {
            masterVolumeText.text = $"主音量: {Mathf.RoundToInt(value * 100)}%";
        }
        
        // 保存设置
        PlayerPrefs.SetFloat(MASTER_VOLUME_PREF, value);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 音乐音量改变回调
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        // 应用音量设置
        ApplyVolumeSettings();
        
        // 更新文本显示
        if (musicVolumeText != null)
        {
            musicVolumeText.text = $"音乐: {Mathf.RoundToInt(value * 100)}%";
        }
        
        // 保存设置
        PlayerPrefs.SetFloat(MUSIC_VOLUME_PREF, value);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 音效音量改变回调
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        // 应用音量设置
        ApplyVolumeSettings();
        
        // 更新文本显示
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"音效: {Mathf.RoundToInt(value * 100)}%";
        }
        
        // 保存设置
        PlayerPrefs.SetFloat(SFX_VOLUME_PREF, value);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 难度改变回调（Legacy Dropdown）
    /// </summary>
    private void OnDifficultyChanged(int index)
    {
        DifficultyManager.DifficultyLevel difficulty = (DifficultyManager.DifficultyLevel)index;
        DifficultyManager.SetDifficulty(difficulty);
        
        // 更新文本显示
        if (difficultyText != null)
        {
            difficultyText.text = $"当前难度: {DifficultyManager.GetCurrentDifficultyName()}";
        }
    }
    
    /// <summary>
    /// 加载保存的设置
    /// </summary>
    private void LoadSettings()
    {
        // 加载音量设置
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_PREF, defaultMasterVolume);
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOLUME_PREF, defaultMusicVolume);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_PREF, defaultSFXVolume);
        
        // 设置滑块值
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVol;
            OnMasterVolumeChanged(masterVol); // 触发回调以应用设置
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = musicVol;
            OnMusicVolumeChanged(musicVol);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVol;
            OnSFXVolumeChanged(sfxVol);
        }
        
        // 加载难度设置
        DifficultyManager.LoadDifficulty();
        int difficultyIndex = (int)DifficultyManager.CurrentDifficulty;
        
        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = difficultyIndex;
        }
        
        // 更新难度文本
        if (difficultyText != null)
        {
            difficultyText.text = $"当前难度: {DifficultyManager.GetCurrentDifficultyName()}";
        }
    }
    
    /// <summary>
    /// 显示设置面板
    /// </summary>
    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏设置面板
    /// </summary>
    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 切换设置面板显示/隐藏
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
}

