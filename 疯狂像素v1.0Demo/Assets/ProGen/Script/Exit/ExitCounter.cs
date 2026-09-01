using UnityEngine;

/// <summary>
/// 传送门计数器（静态类）
/// 记录玩家进入传送门的次数
/// </summary>
public static class ExitCounter
{
    private static int exitCount = 0; // 进入传送门的次数
    private const int REQUIRED_EXITS = 3; // 需要进入传送门的次数才能解锁付费传送门

    /// <summary>
    /// 获取当前进入传送门的次数
    /// </summary>
    public static int ExitCount => exitCount;

    /// <summary>
    /// 检查是否已解锁付费传送门
    /// </summary>
    public static bool IsPaidExitUnlocked => exitCount >= REQUIRED_EXITS;

    /// <summary>
    /// 增加进入传送门次数
    /// </summary>
    public static void IncrementExitCount()
    {
        exitCount++;
        Debug.Log($"ExitCounter: 玩家进入传送门 {exitCount} 次（需要 {REQUIRED_EXITS} 次解锁付费传送门）");
    }

    /// <summary>
    /// 重置计数器（用于新游戏）
    /// </summary>
    public static void ResetCounter()
    {
        exitCount = 0;
        Debug.Log("ExitCounter: 传送门计数器已重置");
    }
}
