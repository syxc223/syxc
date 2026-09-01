using UnityEngine;

/// <summary>
/// 罐子组件
/// 处理罐子的破碎逻辑、动画播放和金币掉落
/// </summary>
public class Pot : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private Animator potAnimator; // 罐子动画器
    [SerializeField] private string breakAnimationTrigger = "Break"; // 破碎动画触发器名称
    
    [Header("金币掉落设置")]
    [SerializeField] private GameObject coinPrefab; // 金币预制体
    [SerializeField] [Range(0f, 1f)] private float coinDropChance = 0.5f; // 掉落金币的概率（0-1）
    [SerializeField] private int minCoins = 1; // 最小掉落数量
    [SerializeField] private int maxCoins = 3; // 最大掉落数量
    [SerializeField] private float dropForce = 2f; // 掉落力度（随机散布范围）
    
    [Header("破碎设置")]
    [SerializeField] private float destroyDelay = 1f; // 破碎后销毁延迟（秒）
    
    private bool isBroken = false; // 是否已破碎
    private Collider2D potCollider; // 碰撞器（破碎后禁用）

    private void Awake()
    {
        // 自动获取Animator组件
        if (potAnimator == null)
        {
            potAnimator = GetComponent<Animator>();
        }
        
        // 获取碰撞器
        potCollider = GetComponent<Collider2D>();
        
        if (potAnimator == null)
        {
            Debug.LogWarning($"Pot: {gameObject.name} 缺少Animator组件！");
        }
    }

    /// <summary>
    /// 被子弹击中时调用
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查是否被子弹击中
        if (collision.CompareTag("Bullet"))
        {
            BreakPot();
            
            // 销毁子弹
            if (collision.gameObject != null)
            {
                Destroy(collision.gameObject);
            }
        }
    }

    /// <summary>
    /// 破碎罐子
    /// </summary>
    public void BreakPot()
    {
        // 防止重复破碎
        if (isBroken)
        {
            return;
        }

        isBroken = true;
        Debug.Log($"[Pot] {gameObject.name} 被击碎");

        // 禁用碰撞器（防止再次被击中）
        if (potCollider != null)
        {
            potCollider.enabled = false;
        }

        // 播放破碎动画
        PlayBreakAnimation();

        // 概率掉落金币
        TryDropCoins();

        // 延迟销毁罐子对象
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// 播放破碎动画
    /// </summary>
    private void PlayBreakAnimation()
    {
        if (potAnimator != null)
        {
            potAnimator.SetTrigger(breakAnimationTrigger);
            Debug.Log($"[Pot] {gameObject.name} 播放破碎动画: {breakAnimationTrigger}");
        }
        else
        {
            Debug.LogWarning($"[Pot] {gameObject.name} Animator为空，无法播放动画");
        }
    }

    /// <summary>
    /// 尝试掉落金币（根据概率）
    /// </summary>
    private void TryDropCoins()
    {
        // 检查是否掉落金币
        if (Random.value <= coinDropChance)
        {
            DropCoins();
        }
        else
        {
            Debug.Log($"[Pot] {gameObject.name} 未掉落金币（概率未命中）");
        }
    }

    /// <summary>
    /// 掉落金币
    /// </summary>
    private void DropCoins()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning($"[Pot] {gameObject.name} 的金币预制体未设置！");
            return;
        }

        // 随机生成掉落数量
        int coinCount = Random.Range(minCoins, maxCoins + 1);
        Debug.Log($"[Pot] {gameObject.name} 掉落 {coinCount} 个金币");

        // 在罐子位置掉落金币
        for (int i = 0; i < coinCount; i++)
        {
            // 计算随机偏移位置（小范围散布）
            Vector2 randomOffset = Random.insideUnitCircle * dropForce;
            Vector3 dropPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

            // 实例化金币
            GameObject coin = Instantiate(coinPrefab, dropPosition, Quaternion.identity);
            
            // 添加弹跳效果
            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                Vector2 bounceForce = new Vector2(Random.Range(-dropForce * 0.5f, dropForce * 0.5f), dropForce);
                coinRb.AddForce(bounceForce, ForceMode2D.Impulse);
            }
        }
    }
}

