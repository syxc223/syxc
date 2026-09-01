using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;//子弹预制体
    [SerializeField] private float detectionRadius = 5f;//检测半径
    [SerializeField] private float fireRate = 0.5f;//射击频率
    [SerializeField] private float bulletSpeed = 5f;//子弹速度
    [SerializeField] private int baseBulletCount = 1;//基础子弹数量
    [SerializeField] private int maxBulletCount = 5;//最大子弹数量
    [SerializeField] private float bulletSpreadAngle = 2f;//子弹散射角度
    
    [Header("动画设置")]
    [SerializeField] private Animator enemyAnimator; // 敌人动画器
    [SerializeField] private string attackTriggerName = "Attack"; // 攻击动画触发器名称
    [SerializeField] private string isAttackingBoolName = "IsAttacking"; // 攻击状态布尔值名称
    [SerializeField] private float attackAnimationDelay = 0.3f; // 攻击动画延迟（动画播放后多久发射子弹，秒）
    [SerializeField] private bool useAttackAnimation = true; // 是否使用攻击动画

    private Transform player;//玩家引用
    private Rigidbody2D playerRb2D;//玩家刚体引用
    private float nextFireTime;//下次射击时间
    private EnemyScale scaler;//敌人组件引用
    private bool isPlayerDetected;//玩家检测状态
    private bool wasPlayerDetectedLastFrame;//上帧玩家检测状态
    private Rigidbody2D rb2D;//敌人刚体引用
    private bool isAttacking = false; // 是否正在攻击（动画播放中）

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();//获取刚体组件
        scaler = GetComponent<EnemyScale>();//获取敌人缩放组件
        nextFireTime = Time.time;//初始化下次射击时间
        if(fireRate <=0.1f) fireRate = 0.5f;//确保射击频率不为零或负数
        if(bulletPrefab == null) enabled = false;//如果子弹预制体为空则禁用脚本
        if(detectionRadius <=0f) detectionRadius = 5f;//确保检测半径为正数
        
        // 自动获取Animator组件（如果未设置）
        if (enemyAnimator == null && useAttackAnimation)
        {
            enemyAnimator = GetComponent<Animator>();
            if (enemyAnimator == null)
            {
                Debug.LogWarning($"[EnemyShooting] {gameObject.name} 未找到Animator组件，攻击动画功能将被禁用");
                useAttackAnimation = false;
            }
        }
        
        FindPlayer();//查找玩家
    }

    // Update is called once per frame
    void Update()
    {
        if(player == null)
        {
            FindPlayer();//如果玩家为空则重新查找玩家
            isPlayerDetected = false;
            return;
        }
        PlayerDetection();//玩家检测
    }

    void FindPlayer()
{
    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");//通过标签查找玩家对象
    if (playerObj != null)//如果找到玩家对象
    {
        player = playerObj.transform;//获取玩家变换组件
        playerRb2D = playerObj.GetComponent<Rigidbody2D>();//获取玩家刚体组件
    }
}
    void Shoot()
    {
        if (player == null || bulletPrefab == null) return;//如果玩家或子弹预制体为空则返回
        
        // 如果正在攻击动画中，不重复触发
        if (isAttacking) return;

        // 如果使用攻击动画，先播放动画
        if (useAttackAnimation && enemyAnimator != null)
        {
            isAttacking = true;
            
            // 触发攻击动画
            if (!string.IsNullOrEmpty(attackTriggerName))
            {
                enemyAnimator.SetTrigger(attackTriggerName);
            }
            
            // 设置攻击状态布尔值
            if (!string.IsNullOrEmpty(isAttackingBoolName))
            {
                enemyAnimator.SetBool(isAttackingBoolName, true);
            }
            
            Debug.Log($"[EnemyShooting] {gameObject.name} 开始攻击动画");
            
            // 延迟执行实际射击（等待动画播放）
            Invoke(nameof(PerformShoot), attackAnimationDelay);
            
            // 重置攻击状态（在动画结束后）
            Invoke(nameof(ResetAttackState), attackAnimationDelay + 0.2f);
        }
        else
        {
            // 不使用动画，直接射击
            PerformShoot();
        }
    }
    
    /// <summary>
    /// 执行实际射击（在攻击动画播放后调用）
    /// </summary>
    private void PerformShoot()
    {
        if (player == null || bulletPrefab == null) return;

        float scaleFactor = scaler != null ? scaler.CurrentScale : 1f;//获取当前缩放因子
        int bulletCount = Mathf.RoundToInt(Mathf.Lerp(baseBulletCount, maxBulletCount,
            scaler != null ? (scaleFactor - scaler.MinScale) / (scaler.MaxScale - scaler.MinScale) : 0f));//计算子弹数量

        Vector2 playerPos = playerRb2D != null ? playerRb2D.position : (Vector2)player.position;
        Vector2 direction = (playerPos - rb2D.position).normalized;

        if (bulletCount == 1)
        {
            SpawnBullet(direction);
        }
        else
        {
            float startAngle = -bulletSpreadAngle * (bulletCount - 1) / 2f;
            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + i * bulletSpreadAngle;
                Vector2 spreadDirection = Quaternion.Euler(0, 0, angle) * direction;
                SpawnBullet(spreadDirection);
            }
        }
    }
    
    /// <summary>
    /// 重置攻击状态
    /// </summary>
    private void ResetAttackState()
    {
        isAttacking = false;
        
        // 重置攻击状态布尔值
        if (enemyAnimator != null && !string.IsNullOrEmpty(isAttackingBoolName))
        {
            enemyAnimator.SetBool(isAttackingBoolName, false);
        }
    }

// Step 3

void SpawnBullet(Vector2 direction)
{
    GameObject bullet = Instantiate(bulletPrefab, rb2D.position, Quaternion.identity);
    Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
    if (bulletRb != null)
    {
        bulletRb.linearVelocity = direction * bulletSpeed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
 //玩家检测
   void PlayerDetection()
{
    float distanceToPlayer = Vector2.Distance(rb2D.position, playerRb2D != null ? playerRb2D.position : (Vector2)player.position);
    isPlayerDetected = distanceToPlayer <= detectionRadius;

    if (isPlayerDetected && !wasPlayerDetectedLastFrame)
    {
        nextFireTime = Time.time; // Shoot immediately when player enter the range of detection
    }

    if (isPlayerDetected && Time.time >= nextFireTime)
    {
        Shoot();
        nextFireTime = Time.time + 1f / fireRate;
    }

    wasPlayerDetectedLastFrame = isPlayerDetected;
}
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;//设置Gizmos(调试绘图工具)颜色为红色
        Gizmos.DrawWireSphere(transform.position, detectionRadius);//绘制检测范围
    }

}

