using UnityEngine;

public class DamageManager : MonoBehaviour
{
    [SerializeField] private float damage = 1f;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 对玩家造成伤害
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth.Health -= damage;
        }

        // 被子弹击中时销毁
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Debug.Log($"[DamageManager] {gameObject.name} 被子弹击中，准备销毁并掉落物品");
            
            // 在销毁前掉落金币（如果有CoinDropper组件）
            CoinDropper coinDropper = GetComponent<CoinDropper>();
            if (coinDropper != null)
            {
                Debug.Log($"[DamageManager] {gameObject.name} 找到CoinDropper组件，调用DropCoins()");
                coinDropper.DropCoins();
            }
            
            // 掉落苹果（如果有AppleDropper组件）
            AppleDropper appleDropper = GetComponent<AppleDropper>();
            if (appleDropper != null)
            {
                Debug.Log($"[DamageManager] {gameObject.name} 找到AppleDropper组件，调用DropApple()");
                appleDropper.DropApple();
            }
            
            Destroy(gameObject);
        }
    }
}
