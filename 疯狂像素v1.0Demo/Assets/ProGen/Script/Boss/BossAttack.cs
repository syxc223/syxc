using UnityEngine;

/// <summary>
/// Boss攻击组件
/// 处理Boss的实际攻击逻辑（发射子弹）
/// </summary>
[RequireComponent(typeof(BossController))]
public class BossAttack : MonoBehaviour
{
    [Header("攻击设置")]
    [SerializeField] private GameObject bulletPrefab; // Boss子弹预制体
    [SerializeField] private Transform attackPoint; // 攻击点（如果为空则使用Boss位置）
    [SerializeField] private float bulletSpeed = 5f; // 子弹速度
    
    private BossController bossController; // Boss控制器引用
    private Transform player; // 玩家引用

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        
        // 如果没有指定攻击点，使用Boss位置
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
        }
    }

    /// <summary>
    /// 查找玩家
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    /// <summary>
    /// 执行攻击（由BossController在攻击动画时调用）
    /// </summary>
    public void PerformAttack()
    {
        if (bulletPrefab == null || player == null)
        {
            Debug.LogWarning($"[BossAttack] {gameObject.name} 子弹预制体或玩家为空，无法攻击");
            return;
        }

        // 计算朝向玩家的方向
        Vector2 direction = ((Vector2)player.position - (Vector2)attackPoint.position).normalized;
        
        // 发射子弹
        SpawnBullet(direction);
        
        Debug.Log($"[BossAttack] {gameObject.name} 发射子弹攻击玩家");
    }

    /// <summary>
    /// 生成子弹
    /// </summary>
    private void SpawnBullet(Vector2 direction)
    {
        GameObject bullet = Instantiate(bulletPrefab, attackPoint.position, Quaternion.identity);
        
        // 设置子弹速度
        if (bullet.TryGetComponent<Rigidbody2D>(out Rigidbody2D bulletRb))
        {
            bulletRb.linearVelocity = direction * bulletSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        // 设置子弹伤害（如果有EnemyBullet组件）
        // EnemyBullet已经有伤害逻辑，不需要额外设置
        // 这里只是验证组件存在，不需要使用变量
        bullet.TryGetComponent<EnemyBullet>(out _);
    }

    /// <summary>
    /// 在攻击动画的特定帧调用（通过Animation Event）
    /// </summary>
    public void OnAttackAnimationEvent()
    {
        PerformAttack();
    }
}

