using UnityEngine;

/// <summary>
/// 苹果拾取组件
/// 附加到苹果预制体上，玩家接触时拾取苹果并恢复血量
/// </summary>
public class ApplePickup : MonoBehaviour
{
    [Header("恢复设置")]
    [SerializeField] private float healAmount = 1.0f; // 恢复的血量值

    // 支持2D碰撞器
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectApple();
        }
    }

    // 支持3D碰撞器
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            CollectApple();
        }
    }

    /// <summary>
    /// 拾取苹果
    /// </summary>
    private void CollectApple()
    {
        // 恢复玩家血量
        float currentHealth = PlayerHealth.Health;
        float maxHealth = PlayerStats.MaxHealth > 0 ? PlayerStats.MaxHealth : PlayerHealth.clampHealth;
        
        // 如果血量已满，不拾取
        if (currentHealth >= maxHealth)
        {
            Debug.Log($"[ApplePickup] 玩家血量已满 ({currentHealth:F2}/{maxHealth:F2})，无法拾取苹果");
            return;
        }
        
        // 恢复血量
        PlayerHealth.Health += healAmount;
        float newHealth = PlayerHealth.Health;
        
        Debug.Log($"[ApplePickup] 玩家拾取苹果，恢复 {healAmount} 点血量 ({currentHealth:F2} -> {newHealth:F2}/{maxHealth:F2})");
        
        // 播放拾取苹果音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayApplePickupSound();
        }
        
        // 可以在这里添加拾取特效等
        // 例如：粒子特效等
        
        // 销毁苹果对象
        Destroy(gameObject);
    }

    /// <summary>
    /// 设置恢复血量值（供外部调用）
    /// </summary>
    public void SetHealAmount(float amount)
    {
        healAmount = Mathf.Max(0.1f, amount);
    }
}

