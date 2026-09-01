using UnityEngine;

/// <summary>
/// 敌人生命值组件
/// 处理敌人的生命值和死亡逻辑
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 5.0f; // 最大生命值
    [SerializeField] private bool useLevelScaling = true; // 是否根据层数调整血量
    private float currentHealth; // 当前生命值

    private void Start()
    {
        // 如果启用层数缩放，根据当前层数设置血量
        if (useLevelScaling)
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager != null)
            {
                maxHealth = levelManager.GetEnemyHealthForCurrentLevel();
            }
        }
        
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 初始化血量（供外部调用）
    /// </summary>
    public void InitializeHealth(float health)
    {
        maxHealth = health;
        currentHealth = health;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[EnemyHealth] {gameObject.name} 受到 {damage} 点伤害，剩余生命值: {currentHealth:F2}/{maxHealth:F2}");

        if (currentHealth <= 0)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} 生命值归零，触发死亡");
            Die();
        }
    }

    /// <summary>
    /// 敌人死亡
    /// </summary>
    private void Die()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} 死亡，开始处理掉落...");
        
        // 掉落金币（如果有CoinDropper组件）
        CoinDropper coinDropper = GetComponent<CoinDropper>();
        if (coinDropper != null)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} 找到CoinDropper组件，调用DropCoins()");
            coinDropper.DropCoins();
        }
        
        // 掉落苹果（如果有AppleDropper组件）
        AppleDropper appleDropper = GetComponent<AppleDropper>();
        if (appleDropper != null)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} 找到AppleDropper组件，调用DropApple()");
            appleDropper.DropApple();
        }
        
        // 可以在这里添加死亡特效、音效等
        Destroy(gameObject);
    }
}

