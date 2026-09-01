using UnityEngine;

/// <summary>
/// 金币掉落组件
/// 附加到敌人对象上，敌人死亡时掉落金币
/// </summary>
public class CoinDropper : MonoBehaviour
{
    [Header("掉落设置")]
    [SerializeField] private GameObject coinPrefab; // 金币预制体
    [SerializeField] private int minCoins = 1; // 最小掉落数量
    [SerializeField] private int maxCoins = 3; // 最大掉落数量
    [SerializeField] private float dropForce = 2f; // 掉落力度（随机散布范围）
    [SerializeField] private bool dropOnDeath = true; // 是否在死亡时掉落

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        // 初始化时检查组件配置
        Debug.Log($"[CoinDropper] {gameObject.name} Awake - CoinPrefab: {(coinPrefab != null ? coinPrefab.name : "NULL")}");
    }

    private void Start()
    {
        // 获取EnemyHealth组件（用于参考，但实际调用由EnemyHealth或DamageManager触发）
        enemyHealth = GetComponent<EnemyHealth>();
        
        Debug.Log($"[CoinDropper] {gameObject.name} Start - CoinPrefab: {(coinPrefab != null ? coinPrefab.name : "NULL")}, " +
                  $"MinCoins: {minCoins}, MaxCoins: {maxCoins}, DropOnDeath: {dropOnDeath}");
    }

    /// <summary>
    /// 掉落金币（由EnemyHealth或DamageManager在死亡时调用）
    /// </summary>
    public void DropCoins()
    {
        Debug.Log($"[CoinDropper] DropCoins() 被调用 - GameObject: {gameObject.name}");
        
        if (!dropOnDeath)
        {
            Debug.Log($"[CoinDropper] {gameObject.name} dropOnDeath = false，跳过掉落");
            return;
        }
        
        if (coinPrefab == null)
        {
            Debug.LogError($"[CoinDropper] {gameObject.name} 的金币预制体(coinPrefab)未设置！请在Inspector中设置Coin Prefab字段。");
            return;
        }

        // 随机生成掉落数量
        int coinCount = Random.Range(minCoins, maxCoins + 1);
        Debug.Log($"[CoinDropper] {gameObject.name} 准备掉落 {coinCount} 个金币，位置: {transform.position}");

        // 在敌人位置掉落金币
        int successfullyDropped = 0;
        for (int i = 0; i < coinCount; i++)
        {
            try
            {
                // 计算随机偏移位置（小范围散布）
                Vector2 randomOffset = Random.insideUnitCircle * dropForce;
                Vector3 dropPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

                // 实例化金币
                GameObject coin = Instantiate(coinPrefab, dropPosition, Quaternion.identity);
                if (coin != null)
                {
                    successfullyDropped++;
                    Debug.Log($"[CoinDropper] 成功实例化金币 {i + 1}/{coinCount}，位置: {dropPosition}");

                    // 可选：添加小幅度弹跳效果
                    Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
                    if (coinRb != null)
                    {
                        // 给金币一个小的向上力，模拟掉落效果
                        Vector2 bounceForce = new Vector2(Random.Range(-dropForce * 0.5f, dropForce * 0.5f), dropForce);
                        coinRb.AddForce(bounceForce, ForceMode2D.Impulse);
                    }
                    else
                    {
                        Debug.LogWarning($"[CoinDropper] 金币预制体缺少Rigidbody2D组件，无法添加弹跳效果");
                    }
                }
                else
                {
                    Debug.LogError($"[CoinDropper] 实例化金币失败！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CoinDropper] 掉落金币时发生错误: {e.Message}");
            }
        }

        Debug.Log($"[CoinDropper] {gameObject.name} 成功掉落了 {successfullyDropped}/{coinCount} 个金币");
    }
}

