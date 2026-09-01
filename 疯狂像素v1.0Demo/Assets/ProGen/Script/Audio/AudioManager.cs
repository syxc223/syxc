using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 音频管理器
/// 统一管理游戏中的所有音乐和音效
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    
    [Header("音乐设置")]
    [SerializeField] private AudioClip menuMusic; // 主菜单背景音乐
    [SerializeField] private AudioClip explorationMusic; // 地图探索音乐
    [SerializeField] private AudioClip bossMusic; // Boss战音乐
    
    [Header("音效设置")]
    [SerializeField] private AudioClip shootSound; // 开枪音效
    [SerializeField] private AudioClip playerDeathSound; // 玩家死亡音效
    [SerializeField] private AudioClip bossDeathSound; // Boss死亡音效
    [SerializeField] private AudioClip coinPickupSound; // 拾取金币音效
    [SerializeField] private AudioClip applePickupSound; // 拾取苹果音效
    
    [Header("音频源设置")]
    [SerializeField] private AudioSource musicSource; // 音乐音频源
    [SerializeField] private AudioSource sfxSource; // 音效音频源
    
    [Header("音量设置")]
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.7f; // 音乐音量
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f; // 音效音量
    
    [Header("音乐切换设置")]
    [SerializeField] private float musicFadeTime = 1f; // 音乐淡入淡出时间
    
    private AudioClip currentMusic; // 当前播放的音乐
    private bool isFading = false; // 是否正在淡入淡出
    
    // 单例模式
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioManager>();// 尝试在场景中查找已有的实例
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // 确保只有一个实例
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 保持音频管理器在场景切换时不被销毁
            
            // 如果没有设置音频源，自动创建
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            
            // 设置音量
            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 根据当前场景自动播放相应的音乐
        CheckSceneAndPlayMusic();
    }
    
    private void OnEnable()
    {
        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        // 取消监听场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// 场景加载完成时的回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckSceneAndPlayMusic();
    }
    
    /// <summary>
    /// 检查场景并播放相应的音乐
    /// </summary>
    private void CheckSceneAndPlayMusic()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        
        // 检查是否是主菜单场景（通常是第一个场景或包含"Menu"的场景）
        if (sceneIndex == 0 || sceneName.ToLower().Contains("menu") || sceneName.ToLower().Contains("title"))
        {
            PlayMenuMusic();
        }
        // 检查是否是最后一个场景（可能包含Boss）
        else if (sceneIndex == sceneCount - 1 || sceneName.ToLower().Contains("boss"))
        {
            PlayBossMusic();
        }
        // 其他场景播放探索音乐
        else
        {
            PlayExplorationMusic();
        }
    }
    
    /// <summary>
    /// 播放主菜单音乐
    /// </summary>
    public void PlayMenuMusic()
    {
        if (menuMusic != null && currentMusic != menuMusic)
        {
            PlayMusic(menuMusic);
            Debug.Log("[AudioManager] 播放主菜单音乐");
        }
    }
    
    /// <summary>
    /// 播放探索音乐
    /// </summary>
    public void PlayExplorationMusic()
    {
        if (explorationMusic != null && currentMusic != explorationMusic)
        {
            PlayMusic(explorationMusic);
            Debug.Log("[AudioManager] 播放探索音乐");
        }
    }
    
    /// <summary>
    /// 播放Boss战音乐
    /// </summary>
    public void PlayBossMusic()
    {
        if (bossMusic != null && currentMusic != bossMusic)
        {
            PlayMusic(bossMusic);
            Debug.Log("[AudioManager] 播放Boss战音乐");
        }
    }
    
    /// <summary>
    /// 播放音乐（带淡入淡出效果）
    /// </summary>
    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        
        currentMusic = clip;
        
        if (musicSource.isPlaying)
        {
            // 如果正在播放音乐，先淡出再淡入新音乐
            StartCoroutine(FadeOutAndPlay(clip));
        }
        else
        {
            // 直接播放新音乐（带淡入效果）
            StartCoroutine(FadeInAndPlay(clip));
        }
    }
    
    /// <summary>
    /// 淡出当前音乐并播放新音乐
    /// </summary>
    private System.Collections.IEnumerator FadeOutAndPlay(AudioClip newClip)
    {
        isFading = true;
        
        // 淡出当前音乐
        float startVolume = musicSource.volume;
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.unscaledDeltaTime / musicFadeTime;
            yield return null;
        }
        
        musicSource.Stop();
        
        // 播放新音乐并淡入
        musicSource.clip = newClip;
        musicSource.Play();
        
        while (musicSource.volume < musicVolume)
        {
            musicSource.volume += musicVolume * Time.unscaledDeltaTime / musicFadeTime;
            yield return null;
        }
        
        musicSource.volume = musicVolume;
        isFading = false;
    }
    
    /// <summary>
    /// 淡入并播放新音乐
    /// </summary>
    private System.Collections.IEnumerator FadeInAndPlay(AudioClip clip)
    {
        isFading = true;
        
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();
        
        while (musicSource.volume < musicVolume)
        {
            musicSource.volume += musicVolume * Time.unscaledDeltaTime / musicFadeTime;
            yield return null;
        }
        
        musicSource.volume = musicVolume;
        isFading = false;
    }
    
    /// <summary>
    /// 播放开枪音效
    /// </summary>
    public void PlayShootSound()
    {
        if (shootSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(shootSound);
        }
    }
    
    /// <summary>
    /// 播放玩家死亡音效
    /// </summary>
    public void PlayPlayerDeathSound()
    {
        if (playerDeathSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(playerDeathSound);
            Debug.Log("[AudioManager] 播放玩家死亡音效");
        }
    }
    
    /// <summary>
    /// 播放Boss死亡音效
    /// </summary>
    public void PlayBossDeathSound()
    {
        if (bossDeathSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(bossDeathSound);
            Debug.Log("[AudioManager] 播放Boss死亡音效");
        }
    }
    
    /// <summary>
    /// 播放拾取金币音效
    /// </summary>
    public void PlayCoinPickupSound()
    {
        if (coinPickupSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(coinPickupSound);
        }
    }
    
    /// <summary>
    /// 播放拾取苹果音效
    /// </summary>
    public void PlayApplePickupSound()
    {
        if (applePickupSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(applePickupSound);
        }
    }
    
    /// <summary>
    /// 停止当前音乐
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic());
        }
    }
    
    /// <summary>
    /// 淡出当前音乐
    /// </summary>
    private System.Collections.IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.unscaledDeltaTime / musicFadeTime;
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.volume = musicVolume;
        currentMusic = null;
    }
    
    /// <summary>
    /// 设置音乐音量
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null && !isFading)
        {
            musicSource.volume = musicVolume;
        }
    }
    
    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }
    
    /// <summary>
    /// 获取音乐音量
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }
    
    /// <summary>
    /// 获取音效音量
    /// </summary>
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
}

