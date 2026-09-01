using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss血条UI组件
/// 使用Slider显示Boss的当前生命值，方便玩家观察
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    private static BossHealthBar instance;
    
    [Header("血条UI元素")]
    [SerializeField] private GameObject healthBarPanel; // 血条面板（整个容器）
    [SerializeField] private Slider healthBarSlider; // 血条Slider（显示当前血量）
    [SerializeField] private Text healthText; // 血量文本（可选，显示"当前/最大"）
    
    [Header("血条设置")]
    [SerializeField] private bool showHealthText = true; // 是否显示血量文本
    [SerializeField] private bool hideWhenBossDead = true; // Boss死亡后是否隐藏血条
    [SerializeField] private float updateSmoothness = 5f; // 血条更新平滑度（值越大越平滑）
    
    private BossHealth currentBoss; // 当前Boss引用
    private float targetValue = 1f; // 目标Slider值（0-1）
    private bool isInitialized = false; // 是否已初始化
    
    public static BossHealthBar Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BossHealthBar>();
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
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        InitializeHealthBar();
        FindBossInScene();
    }
    
    private void Update()
    {
        // 平滑更新Slider值
        if (healthBarSlider != null && Mathf.Abs(healthBarSlider.value - targetValue) > 0.001f)
        {
            healthBarSlider.value = Mathf.Lerp(healthBarSlider.value, targetValue, 
                Time.unscaledDeltaTime * updateSmoothness);
        }
        
        // 如果Boss不存在或已死亡，尝试重新查找
        if (currentBoss == null || (currentBoss.IsDead() && hideWhenBossDead))
        {
            FindBossInScene();
        }
        
        // 更新血条显示
        if (currentBoss != null && !currentBoss.IsDead())
        {
            UpdateHealthBar();
        }
        else if (currentBoss != null && currentBoss.IsDead() && hideWhenBossDead)
        {
            HideHealthBar();
        }
    }
    
    /// <summary>
    /// 初始化血条
    /// </summary>
    private void InitializeHealthBar()
    {
        // 如果没有手动设置，尝试自动查找UI元素
        if (healthBarPanel == null)
        {
            healthBarPanel = gameObject;
            Debug.Log("[BossHealthBar] 自动设置HealthBarPanel为当前GameObject");
        }
        
        if (healthBarSlider == null)
        {
            // 尝试查找名为"Slider"或包含"Slider"的子对象
            healthBarSlider = FindChildComponent<Slider>(transform, "Slider");
            if (healthBarSlider != null)
            {
                Debug.Log($"[BossHealthBar] 自动找到Slider: {healthBarSlider.name}");
            }
            
            if (healthBarSlider == null)
            {
                // 尝试查找所有Slider组件，使用第一个
                Slider[] sliders = GetComponentsInChildren<Slider>();
                if (sliders.Length > 0)
                {
                    healthBarSlider = sliders[0];
                    Debug.Log($"[BossHealthBar] 自动找到Slider: {sliders[0].name}");
                }
            }
        }
        
        // 配置Slider（如果找到了）
        if (healthBarSlider != null)
        {
            // 确保Slider的值范围是0-1
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = 1f;
            healthBarSlider.value = 1f; // 初始满血
            healthBarSlider.interactable = false; // 禁用交互（血条不需要玩家操作）
            
            Debug.Log("[BossHealthBar] Slider配置完成：minValue=0, maxValue=1, interactable=false");
        }
        
        if (healthText == null && showHealthText)
        {
            healthText = GetComponentInChildren<Text>();
            if (healthText != null)
            {
                Debug.Log($"[BossHealthBar] 自动找到HealthText: {healthText.name}");
            }
        }
        
        // 验证关键组件
        if (healthBarSlider == null)
        {
            Debug.LogError("[BossHealthBar] 错误：未找到HealthBarSlider！请确保：\n" +
                          "1. 在BossHealthBar下创建Slider组件\n" +
                          "2. 在Inspector中手动拖入Health Bar Slider引用");
        }
        
        // 初始状态隐藏血条
        if (healthBarPanel != null)
        {
            healthBarPanel.SetActive(false);
        }
        
        isInitialized = true;
        
        if (healthBarSlider != null)
        {
            Debug.Log($"[BossHealthBar] 血条初始化完成 - Slider: {healthBarSlider.name}, Text: {(healthText != null ? healthText.name : "未设置")}");
        }
        else
        {
            Debug.LogError("[BossHealthBar] 血条初始化失败：缺少Slider组件！");
        }
    }
    
    /// <summary>
    /// 在场景中查找Boss
    /// </summary>
    private void FindBossInScene()
    {
        // 查找所有带有BossHealth组件的对象
        BossHealth[] bosses = FindObjectsByType<BossHealth>(FindObjectsSortMode.None);
        
        if (bosses.Length > 0)
        {
            // 找到第一个未死亡的Boss
            foreach (BossHealth boss in bosses)
            {
                if (boss != null && !boss.IsDead())
                {
                    SetBoss(boss);
                    return;
                }
            }
            
            // 如果所有Boss都已死亡，使用第一个（用于显示死亡状态）
            if (bosses.Length > 0)
            {
                SetBoss(bosses[0]);
            }
        }
        else
        {
            // 没有找到Boss，隐藏血条
            if (currentBoss != null)
            {
                currentBoss = null;
                HideHealthBar();
            }
        }
    }
    
    /// <summary>
    /// 设置要显示的Boss
    /// </summary>
    public void SetBoss(BossHealth boss)
    {
        if (boss == null)
        {
            Debug.LogWarning("[BossHealthBar] 尝试设置空的Boss引用");
            return;
        }
        
        currentBoss = boss;
        ShowHealthBar();
        UpdateHealthBar();
        Debug.Log($"[BossHealthBar] 已设置Boss: {boss.gameObject.name}");
    }
    
    /// <summary>
    /// 更新血条显示
    /// </summary>
    private void UpdateHealthBar()
    {
        if (currentBoss == null || !isInitialized) return;
        
        float currentHealth = currentBoss.GetCurrentHealth();
        float maxHealth = currentBoss.GetMaxHealth();
        
        if (maxHealth > 0)
        {
            // 计算目标Slider值（0-1之间）
            targetValue = currentHealth / maxHealth;
            
            // 更新血量文本
            if (healthText != null && showHealthText)
            {
                healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
            }
        }
    }
    
    /// <summary>
    /// 显示血条
    /// </summary>
    public void ShowHealthBar()
    {
        if (healthBarPanel != null)
        {
            healthBarPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏血条
    /// </summary>
    public void HideHealthBar()
    {
        if (healthBarPanel != null)
        {
            healthBarPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 在子对象中查找指定名称的组件
    /// </summary>
    private T FindChildComponent<T>(Transform parent, string nameContains) where T : Component
    {
        if (parent == null) return null;
        
        foreach (Transform child in parent)
        {
            if (child.name.Contains(nameContains))
            {
                T component = child.GetComponent<T>();
                if (component != null) return component;
            }
            
            // 递归查找
            T found = FindChildComponent<T>(child, nameContains);
            if (found != null) return found;
        }
        
        return null;
    }
    
    /// <summary>
    /// 强制刷新血条（供外部调用）
    /// </summary>
    public void RefreshHealthBar()
    {
        if (currentBoss != null)
        {
            UpdateHealthBar();
            if (healthBarSlider != null)
            {
                healthBarSlider.value = targetValue; // 立即更新，不使用平滑
            }
        }
    }
}
