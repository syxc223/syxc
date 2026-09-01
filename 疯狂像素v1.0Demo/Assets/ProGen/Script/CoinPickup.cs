using UnityEngine;

/// <summary>
/// 金币拾取组件
/// 附加到金币预制体上，玩家接触时拾取金币
/// </summary>
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int coinValue = 1; // 金币价值（每个金币多少数量）

    // 支持2D碰撞器
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectCoin();
        }
    }

    // 支持3D碰撞器（与AmmoCollected保持一致）
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            CollectCoin();
        }
    }

    /// <summary>
    /// 拾取金币
    /// </summary>
    private void CollectCoin()
    {
        // 增加金币
        CoinSystem.AddCoins(coinValue);
        
        // 播放拾取金币音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoinPickupSound();
        }
        
        // 可以在这里添加拾取特效等
        // 例如：粒子特效等
        
        // 销毁金币对象
        Destroy(gameObject);
    }

    /// <summary>
    /// 设置金币价值（供外部调用）
    /// </summary>
    public void SetCoinValue(int value)
    {
        coinValue = Mathf.Max(1, value);
    }
}

