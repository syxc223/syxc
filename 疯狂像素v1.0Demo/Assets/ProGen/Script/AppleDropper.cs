using UnityEngine;

/// <summary>
/// 苹果掉落组件
/// 附加到敌人对象上，敌人死亡时掉落苹果
/// </summary>
public class AppleDropper : MonoBehaviour
{
    [Header("掉落设置")]
    [SerializeField] private GameObject applePrefab; // 苹果预制体
    [SerializeField] private float dropChance = 0.3f; // 掉落概率（0-1，30%概率）
    [SerializeField] private float dropForce = 2f; // 掉落力度（随机散布范围）
    [SerializeField] private bool dropOnDeath = true; // 是否在死亡时掉落

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        // 初始化时检查组件配置
        Debug.Log($"[AppleDropper] {gameObject.name} Awake - ApplePrefab: {(applePrefab != null ? applePrefab.name : "NULL")}");
    }

    private void Start()
    {
        // 获取EnemyHealth组件（用于参考，但实际调用由EnemyHealth触发）
        enemyHealth = GetComponent<EnemyHealth>();
        
        Debug.Log($"[AppleDropper] {gameObject.name} Start - ApplePrefab: {(applePrefab != null ? applePrefab.name : "NULL")}, " +
                  $"DropChance: {dropChance * 100:F1}%, DropOnDeath: {dropOnDeath}");
    }

    /// <summary>
    /// 掉落苹果（由EnemyHealth在死亡时调用）
    /// </summary>
    public void DropApple()
    {
        Debug.Log($"[AppleDropper] DropApple() 被调用 - GameObject: {gameObject.name}");
        
        if (!dropOnDeath)
        {
            Debug.Log($"[AppleDropper] {gameObject.name} dropOnDeath = false，跳过掉落");
            return;
        }
        
        if (applePrefab == null)
        {
            Debug.LogError($"[AppleDropper] {gameObject.name} 的苹果预制体(applePrefab)未设置！请在Inspector中设置Apple Prefab字段。");
            return;
        }

        // 根据概率决定是否掉落
        float randomValue = Random.Range(0f, 1f);
        if (randomValue > dropChance)
        {
            Debug.Log($"[AppleDropper] {gameObject.name} 未触发掉落（随机值: {randomValue:F2} > 掉落概率: {dropChance:F2}）");
            return;
        }

        Debug.Log($"[AppleDropper] {gameObject.name} 触发苹果掉落，位置: {transform.position}");

        try
        {
            // 计算随机偏移位置（小范围散布）
            Vector2 randomOffset = Random.insideUnitCircle * dropForce;
            Vector3 dropPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

            // 实例化苹果
            GameObject apple = Instantiate(applePrefab, dropPosition, Quaternion.identity);
            if (apple != null)
            {
                Debug.Log($"[AppleDropper] 成功实例化苹果，位置: {dropPosition}");

                // 配置Rigidbody2D并添加掉落效果
                Rigidbody2D appleRb = apple.GetComponent<Rigidbody2D>();
                if (appleRb != null)
                {
                    // 确保重力设置正确（如果重力为0则设置为1）
                    if (appleRb.gravityScale == 0)
                    {
                        appleRb.gravityScale = 1f;
                        Debug.Log($"[AppleDropper] 苹果重力设置为1");
                    }
                    
                    // 给苹果一个小的随机水平力和小的向上力，模拟掉落弹跳效果
                    // 注意：向上的力应该小于重力，否则会飞起来
                    float horizontalForce = Random.Range(-dropForce * 0.3f, dropForce * 0.3f);
                    float upwardForce = dropForce * 0.5f; // 减小向上的力，避免飞得太高
                    Vector2 bounceForce = new Vector2(horizontalForce, upwardForce);
                    appleRb.AddForce(bounceForce, ForceMode2D.Impulse);
                }
                else
                {
                    Debug.LogWarning($"[AppleDropper] 苹果预制体缺少Rigidbody2D组件，无法添加弹跳效果");
                }
            }
            else
            {
                Debug.LogError($"[AppleDropper] 实例化苹果失败！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AppleDropper] 掉落苹果时发生错误: {e.Message}");
        }
    }
}

