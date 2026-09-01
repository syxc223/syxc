using UnityEngine;

/// <summary>
/// 玩家状态管理器（静态类）
/// 管理攻击力和生命上限等永久属性
/// </summary>
public static class PlayerStats
{
    private static float baseHealth = 5.0f; // 基础生命值
    private static float healthBonus = 0f; // 生命值加成（从升级获得）
    private static float baseDamage = 1.0f; // 基础攻击力（子弹伤害）
    private static float damageBonus = 0f; // 攻击力加成（从升级获得）

    /// <summary>
    /// 当前最大生命值（基础 + 加成）
    /// </summary>
    public static float MaxHealth
    {
        get => baseHealth + healthBonus;
    }

    /// <summary>
    /// 当前攻击力（基础 + 加成）
    /// </summary>
    public static float AttackDamage
    {
        get => baseDamage + damageBonus;
    }

    /// <summary>
    /// 增加生命上限
    /// </summary>
    /// <param name="amount">增加的生命值数量</param>
    public static void AddHealthBonus(float amount)
    {
        healthBonus += amount;
        // 同时恢复当前生命值
        PlayerHealth.Health = Mathf.Min(PlayerHealth.Health + amount, MaxHealth);
    }

    /// <summary>
    /// 增加攻击力
    /// </summary>
    /// <param name="amount">增加的攻击力数值</param>
    public static void AddDamageBonus(float amount)
    {
        damageBonus += amount;
    }

    /// <summary>
    /// 重置所有状态（用于新游戏）
    /// </summary>
    public static void ResetStats()
    {
        healthBonus = 0f;
        damageBonus = 0f;
        PlayerHealth.Health = baseHealth;
    }

    /// <summary>
    /// 获取当前生命值加成
    /// </summary>
    public static float GetHealthBonus() => healthBonus;

    /// <summary>
    /// 获取当前攻击力加成
    /// </summary>
    public static float GetDamageBonus() => damageBonus;
}

