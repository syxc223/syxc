using UnityEngine;
using System;

/// <summary>
/// 金币系统（静态类）
/// 管理玩家金币总数
/// </summary>
public static class CoinSystem
{
    private static int coinCount = 0; // 当前金币数量

    /// <summary>
    /// 金币变化事件（参数：当前金币数量）
    /// </summary>
    public static event Action<int> OnCoinsChanged;

    /// <summary>
    /// 获取当前金币数量
    /// </summary>
    public static int CoinCount
    {
        get => coinCount;
    }

    /// <summary>
    /// 增加金币
    /// </summary>
    /// <param name="amount">增加的金币数量</param>
    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        
        coinCount += amount;
        coinCount = Mathf.Max(0, coinCount); // 确保不为负数
        OnCoinsChanged?.Invoke(coinCount);
        
        Debug.Log($"CoinSystem: 获得 {amount} 金币，当前总数: {coinCount}");
    }

    /// <summary>
    /// 减少金币
    /// </summary>
    /// <param name="amount">减少的金币数量</param>
    /// <returns>是否成功减少（金币不足时返回false）</returns>
    public static bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (coinCount < amount) return false;

        coinCount -= amount;
        coinCount = Mathf.Max(0, coinCount);
        OnCoinsChanged?.Invoke(coinCount);
        
        Debug.Log($"CoinSystem: 花费 {amount} 金币，剩余: {coinCount}");
        return true;
    }

    /// <summary>
    /// 重置金币（用于新游戏）
    /// </summary>
    public static void ResetCoins()
    {
        coinCount = 0;
        OnCoinsChanged?.Invoke(coinCount);
        Debug.Log("CoinSystem: 金币已重置");
    }

    /// <summary>
    /// 设置金币数量（用于测试或特殊场景）
    /// </summary>
    public static void SetCoins(int amount)
    {
        coinCount = Mathf.Max(0, amount);
        OnCoinsChanged?.Invoke(coinCount);
    }
}

