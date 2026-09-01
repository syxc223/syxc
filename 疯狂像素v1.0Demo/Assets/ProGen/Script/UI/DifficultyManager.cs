using UnityEngine;

/// <summary>
/// 难度管理器（静态类）
/// 管理游戏难度设置
/// </summary>
public static class DifficultyManager
{
    /// <summary>
    /// 难度等级枚举
    /// </summary>
    public enum DifficultyLevel
    {
        Easy = 0,    // 简单
        Normal = 1,  // 普通
        Hard = 2     // 困难
    }
    
    private static DifficultyLevel currentDifficulty = DifficultyLevel.Normal; // 当前难度（默认普通）
    private const string DIFFICULTY_PREF_KEY = "GameDifficulty"; // PlayerPrefs键名
    
    /// <summary>
    /// 获取当前难度
    /// </summary>
    public static DifficultyLevel CurrentDifficulty
    {
        get => currentDifficulty;
    }
    
    /// <summary>
    /// 获取难度倍数（用于调整敌人数量和血量）
    /// </summary>
    public static float GetDifficultyMultiplier()
    {
        switch (currentDifficulty)
        {
            case DifficultyLevel.Easy:
                return 0.7f; // 简单：敌人数量和血量减少30%
            case DifficultyLevel.Normal:
                return 1.0f; // 普通：标准难度
            case DifficultyLevel.Hard:
                return 1.5f; // 困难：敌人数量和血量增加50%
            default:
                return 1.0f;
        }
    }
    
    /// <summary>
    /// 获取敌人数量倍数
    /// </summary>
    public static float GetEnemyCountMultiplier()
    {
        return GetDifficultyMultiplier();
    }
    
    /// <summary>
    /// 获取敌人血量倍数
    /// </summary>
    public static float GetEnemyHealthMultiplier()
    {
        return GetDifficultyMultiplier();
    }
    
    /// <summary>
    /// 设置难度
    /// </summary>
    public static void SetDifficulty(DifficultyLevel difficulty)
    {
        currentDifficulty = difficulty;
        PlayerPrefs.SetInt(DIFFICULTY_PREF_KEY, (int)difficulty);
        PlayerPrefs.Save();
        Debug.Log($"DifficultyManager: 难度设置为 {difficulty}");
    }
    
    /// <summary>
    /// 从PlayerPrefs加载难度设置
    /// </summary>
    public static void LoadDifficulty()
    {
        if (PlayerPrefs.HasKey(DIFFICULTY_PREF_KEY))
        {
            int difficultyValue = PlayerPrefs.GetInt(DIFFICULTY_PREF_KEY, (int)DifficultyLevel.Normal);
            currentDifficulty = (DifficultyLevel)difficultyValue;
            Debug.Log($"DifficultyManager: 从PlayerPrefs加载难度: {currentDifficulty}");
        }
        else
        {
            // 如果没有保存的设置，使用默认值
            currentDifficulty = DifficultyLevel.Normal;
            Debug.Log($"DifficultyManager: 使用默认难度: {currentDifficulty}");
        }
    }
    
    /// <summary>
    /// 获取难度显示名称
    /// </summary>
    public static string GetDifficultyName(DifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                return "简单";
            case DifficultyLevel.Normal:
                return "普通";
            case DifficultyLevel.Hard:
                return "困难";
            default:
                return "普通";
        }
    }
    
    /// <summary>
    /// 获取当前难度的显示名称
    /// </summary>
    public static string GetCurrentDifficultyName()
    {
        return GetDifficultyName(currentDifficulty);
    }
}

