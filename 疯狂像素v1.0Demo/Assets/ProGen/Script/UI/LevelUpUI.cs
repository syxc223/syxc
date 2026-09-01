using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级选择界面控制器
/// 当玩家到达终点时显示，提供升级选项
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject levelUpPanel; // 升级面板（父对象）
    [SerializeField] private Button increaseAttackButton; // 增加攻击力按钮
    [SerializeField] private Button increaseHealthButton; // 增加生命上限按钮
    [SerializeField] private Text attackButtonText; // 攻击力按钮文本（可选）
    [SerializeField] private Text healthButtonText; // 生命值按钮文本（可选）

    [Header("升级数值")]
    [SerializeField] private float attackIncreaseAmount = 0.5f; // 每次增加攻击力的数值
    [SerializeField] private float healthIncreaseAmount = 1.0f; // 每次增加生命值的数值

    private static LevelUpUI instance; // 单例实例
    private System.Action onLevelUpComplete; // 升级完成回调

    public static LevelUpUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LevelUpUI>();
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

        // 初始化时隐藏面板
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        // 绑定按钮事件
        if (increaseAttackButton != null)
        {
            increaseAttackButton.onClick.AddListener(OnIncreaseAttackClick);
        }

        if (increaseHealthButton != null)
        {
            increaseHealthButton.onClick.AddListener(OnIncreaseHealthClick);
        }

        // 更新按钮文本
        UpdateButtonTexts();
    }

    private void OnDestroy()
    {
        // 取消事件绑定，防止内存泄漏
        if (increaseAttackButton != null)
        {
            increaseAttackButton.onClick.RemoveListener(OnIncreaseAttackClick);
        }

        if (increaseHealthButton != null)
        {
            increaseHealthButton.onClick.RemoveListener(OnIncreaseHealthClick);
        }
    }

    /// <summary>
    /// 显示升级界面
    /// </summary>
    /// <param name="onComplete">升级完成后的回调（用于继续游戏流程）</param>
    public void ShowLevelUpPanel(System.Action onComplete = null)
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
            onLevelUpComplete = onComplete;
            
            // 暂停游戏
            Time.timeScale = 0f;

            // 更新按钮文本显示当前数值
            UpdateButtonTexts();
        }
        else
        {
            Debug.LogWarning("LevelUpUI: levelUpPanel 未设置！");
            // 如果没有面板，直接执行回调
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 隐藏升级界面
    /// </summary>
    public void HideLevelUpPanel()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
            Time.timeScale = 1f; // 恢复游戏时间
        }
    }

    /// <summary>
    /// 更新按钮文本显示当前属性值
    /// </summary>
    private void UpdateButtonTexts()
    {
        if (attackButtonText != null)
        {
            attackButtonText.text = $"增加攻击力\n(当前: {PlayerStats.AttackDamage:F1})";
        }

        if (healthButtonText != null)
        {
            healthButtonText.text = $"增加生命上限\n(当前: {PlayerStats.MaxHealth:F1})";
        }
    }

    /// <summary>
    /// 增加攻击力按钮点击事件
    /// </summary>
    private void OnIncreaseAttackClick()
    {
        // 增加攻击力
        PlayerStats.AddDamageBonus(attackIncreaseAmount);
        
        Debug.Log($"攻击力已增加！当前攻击力: {PlayerStats.AttackDamage}");

        // 隐藏面板并继续游戏
        HideLevelUpPanel();
        onLevelUpComplete?.Invoke();
        onLevelUpComplete = null;
    }

    /// <summary>
    /// 增加生命上限按钮点击事件
    /// </summary>
    private void OnIncreaseHealthClick()
    {
        // 增加生命上限
        PlayerStats.AddHealthBonus(healthIncreaseAmount);
        
        Debug.Log($"生命上限已增加！当前最大生命值: {PlayerStats.MaxHealth}");
        
        // 通知HeartSystem更新（通过查找并调用其方法，或者等待Update自动检测）
        RefreshHeartSystem();

        // 隐藏面板并继续游戏
        HideLevelUpPanel();
        onLevelUpComplete?.Invoke();
        onLevelUpComplete = null;
    }

    /// <summary>
    /// 刷新心形系统（确保UI立即更新）
    /// </summary>
    private void RefreshHeartSystem()
    {
        HeartSystem heartSystem = FindFirstObjectByType<HeartSystem>();
        if (heartSystem != null)
        {
            heartSystem.RefreshHearts();
        }
    }
}

