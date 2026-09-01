using UnityEngine;

public class PlayerHealth
{
    private static float health = 5.0f;
    public static int clampHealth = 20;

    public static float Health
    {
        get => health;
        set
        {
            // 使用 PlayerStats 的最大生命值作为上限（如果存在）
            float maxHealth = PlayerStats.MaxHealth > 0 ? PlayerStats.MaxHealth : clampHealth;
            health = Mathf.Clamp(value, 0f, maxHealth);
        }
    }

    /// <summary>
    /// 初始化生命值（使用PlayerStats的最大值）
    /// </summary>
    public static void InitializeHealth()
    {
        float maxHealth = PlayerStats.MaxHealth > 0 ? PlayerStats.MaxHealth : 5.0f;
        health = maxHealth;
    }
}
