using UnityEngine;

/// <summary>
/// 层数/关卡管理器
/// 管理当前层数、难度递增等
/// </summary>
public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;
    
    [Header("层数设置")]
    [SerializeField] private int currentLevel = 1; // 当前层数（从1开始）
    
    [Header("难度递增设置")]
    [SerializeField] private int baseEnemyCount = 5; // 基础敌人数量（第1层）
    [SerializeField] private float enemyCountMultiplier = 1.2f; // 每层敌人数量倍数
    [SerializeField] private float baseEnemyHealth = 5.0f; // 基础敌人血量（第1层）
    [SerializeField] private float enemyHealthMultiplier = 1.15f; // 每层敌人血量倍数

    public static LevelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LevelManager>();
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
            DontDestroyOnLoad(gameObject); // 保持层数数据在场景切换时不被销毁
            
            // 加载难度设置
            DifficultyManager.LoadDifficulty();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 获取当前层数
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    /// <summary>
    /// 进入下一层
    /// </summary>
    public void AdvanceToNextLevel()
    {
        currentLevel++;
        Debug.Log($"LevelManager: 进入第 {currentLevel} 层！");
    }

    /// <summary>
    /// 根据当前层数计算敌人数量（应用难度倍数）
    /// </summary>
    public int GetEnemyCountForCurrentLevel()
    {
        // 公式：基础数量 * (倍数 ^ (层数-1)) * 难度倍数
        float difficultyMultiplier = DifficultyManager.GetEnemyCountMultiplier();
        int count = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(enemyCountMultiplier, currentLevel - 1) * difficultyMultiplier);
        return Mathf.Max(1, count); // 至少1个敌人
    }

    /// <summary>
    /// 根据当前层数计算敌人血量（应用难度倍数）
    /// </summary>
    public float GetEnemyHealthForCurrentLevel()
    {
        // 公式：基础血量 * (倍数 ^ (层数-1)) * 难度倍数
        float difficultyMultiplier = DifficultyManager.GetEnemyHealthMultiplier();
        float health = baseEnemyHealth * Mathf.Pow(enemyHealthMultiplier, currentLevel - 1) * difficultyMultiplier;
        return Mathf.Max(1.0f, health); // 至少1点血量
    }

    /// <summary>
    /// 重置层数（用于新游戏）
    /// </summary>
    public void ResetLevel()
    {
        currentLevel = 1;
        Debug.Log("LevelManager: 层数已重置为第1层");
    }

    /// <summary>
    /// 设置层数（用于测试或特殊场景）
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        Debug.Log($"LevelManager: 层数设置为第 {currentLevel} 层");
    }
}

