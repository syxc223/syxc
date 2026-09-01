using UnityEngine;

/// <summary>
/// Boss生命值组件
/// 处理Boss的生命值和死亡逻辑
/// </summary>
public class BossHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f; // Boss最大生命值（固定50）
    private float currentHealth; // 当前生命值
    private Animator bossAnimator; // Boss动画器
    private bool isDead = false; // 是否已死亡

    private void Start()
    {
        currentHealth = maxHealth;
        bossAnimator = GetComponent<Animator>();
        
        // 通知Boss血条显示
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.SetBoss(this);
        }
        
        Debug.Log($"[BossHealth] {gameObject.name} 初始化，最大生命值: {maxHealth}");
    }

    /// <summary>
    /// 初始化血量（供外部调用）
    /// </summary>
    public void InitializeHealth(float health)
    {
        maxHealth = health;
        currentHealth = health;
        isDead = false;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        if (isDead) return; // 如果已死亡，不再受到伤害

        currentHealth -= damage;
        Debug.Log($"[BossHealth] {gameObject.name} 受到 {damage} 点伤害，剩余生命值: {currentHealth:F2}/{maxHealth:F2}");

        // 可以在这里触发受伤动画
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("TakeDamage");
        }

        if (currentHealth <= 0)
        {
            Debug.Log($"[BossHealth] {gameObject.name} 生命值归零，触发死亡");
            Die();
        }
    }

    /// <summary>
    /// Boss死亡
    /// </summary>
    private void Die()
    {
        if (isDead) return; // 防止重复死亡
        isDead = true;

        Debug.Log($"[BossHealth] {gameObject.name} 死亡，开始处理掉落...");

        // 播放死亡动画
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Die");
        }

        // 掉落金币（如果有CoinDropper组件）
        CoinDropper coinDropper = GetComponent<CoinDropper>();
        if (coinDropper != null)
        {
            Debug.Log($"[BossHealth] {gameObject.name} 找到CoinDropper组件，调用DropCoins()");
            coinDropper.DropCoins();
        }

        // 播放Boss死亡音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossDeathSound();
        }
        
        // 可以在这里添加死亡特效等
        // 延迟销毁，等待死亡动画播放完成
        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// 获取当前生命值
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// 获取最大生命值
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// 检查是否已死亡
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }
}

