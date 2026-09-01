using UnityEngine;

/// <summary>
/// Boss控制器
/// 管理Boss的动画和基本AI行为
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private Animator bossAnimator; // Boss动画器
    
    [Header("攻击设置")]
    [SerializeField] private float attackRange = 5f; // 攻击范围
    [SerializeField] private float attackCooldown = 2f; // 攻击冷却时间
    [SerializeField] private float attackDuration = 1f; // 攻击动画持续时间
    
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 3f; // Boss移动速度
    [SerializeField] private float stopDistance = 4f; // 停止移动距离（攻击范围边缘）
    
    [Header("玩家检测")]
    [SerializeField] private float detectionRange = 10f; // 检测玩家范围
    
    [Header("音频设置")]
    [SerializeField] private bool playBossMusicOnFirstDetection = true; // 是否在第一次检测到玩家时播放Boss音乐
    
    [Header("弹窗设置")]
    [SerializeField] private string bossName = "变种幽浮"; // Boss名称（用于弹窗显示）
    [SerializeField] private bool showPopupOnFirstDetection = true; // 是否在第一次检测到玩家时显示弹窗
    
    [Header("翻转设置")]
    [SerializeField] private bool enableFlip = true; // 是否启用翻转功能
    [SerializeField] private SpriteRenderer bossSpriteRenderer; // Boss精灵渲染器（如果为空会自动获取）
    
    private Transform player; // 玩家引用
    private Rigidbody2D rb2D; // Boss刚体
    private float lastAttackTime = 0f; // 上次攻击时间
    private bool isAttacking = false; // 是否正在攻击
    private bool isPlayerInRange = false; // 玩家是否在范围内
    private bool hasShownPopup = false; // 是否已显示过弹窗
    private bool hasPlayedBossMusic = false; // 是否已播放过Boss音乐
    private Vector2 moveDirection = Vector2.zero; // 移动方向
    private bool isFacingRight = true; // 当前面向右侧

    private void Awake()
    {
        // 自动获取组件
        if (bossAnimator == null)
        {
            bossAnimator = GetComponent<Animator>();
        }
        
        rb2D = GetComponent<Rigidbody2D>();
        
        if (rb2D == null)
        {
            Debug.LogWarning($"[BossController] {gameObject.name} 缺少Rigidbody2D组件！");
        }
        
        if (bossAnimator == null)
        {
            Debug.LogWarning($"[BossController] {gameObject.name} 缺少Animator组件！");
        }
        
        // 自动获取SpriteRenderer
        if (bossSpriteRenderer == null)
        {
            bossSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (bossSpriteRenderer == null)
        {
            bossSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (bossSpriteRenderer == null && enableFlip)
        {
            Debug.LogWarning($"[BossController] {gameObject.name} 缺少SpriteRenderer组件！翻转功能将无法使用。");
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
            return;
        }

        // 检测玩家距离
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool wasPlayerInRange = isPlayerInRange; // 记录之前的状态
        isPlayerInRange = distanceToPlayer <= detectionRange;
        
        // 第一次检测到玩家时播放Boss音乐
        if (playBossMusicOnFirstDetection && !hasPlayedBossMusic && isPlayerInRange && !wasPlayerInRange)
        {
            PlayBossMusic();
            hasPlayedBossMusic = true;
        }
        
        // 第一次检测到玩家时显示弹窗
        if (showPopupOnFirstDetection && !hasShownPopup && isPlayerInRange && !wasPlayerInRange)
        {
            ShowBossPopup();
            hasShownPopup = true;
        }

        // 如果玩家在范围内且不在攻击中
        if (isPlayerInRange && !isAttacking)
        {
            // 计算移动方向
            moveDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
            
            // 如果距离大于停止距离，继续移动
            if (distanceToPlayer > stopDistance)
            {
                // 移动逻辑在FixedUpdate中处理
            }
            else
            {
                // 到达攻击范围，停止移动
                moveDirection = Vector2.zero;
                
                // 检查是否可以攻击
                if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
                {
                    Attack();
                }
            }
        }
        else
        {
            // 玩家不在范围内或正在攻击，停止移动
            moveDirection = Vector2.zero;
        }

        // 更新动画状态
        UpdateAnimation();
        
        // 更新翻转
        if (enableFlip && isPlayerInRange)
        {
            UpdateFlip();
        }
    }

    /// <summary>
    /// 物理更新，处理Boss移动
    /// </summary>
    private void FixedUpdate()
    {
        // 如果不在攻击状态且有移动方向，则移动
        if (!isAttacking && moveDirection != Vector2.zero && rb2D != null)
        {
            Vector2 targetPosition = rb2D.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb2D.MovePosition(targetPosition);
        }
        else if (rb2D != null)
        {
            // 停止移动（保持当前速度或设置为零）
            rb2D.linearVelocity = Vector2.zero;
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
    /// 攻击
    /// </summary>
    private void Attack()
    {
        if (isAttacking) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        // 播放攻击动画
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Attack");
            Debug.Log($"[BossController] {gameObject.name} 开始攻击");
        }

        // 触发实际攻击（延迟执行，等待动画播放）
        BossAttack bossAttack = GetComponent<BossAttack>();
        if (bossAttack != null)
        {
            Invoke(nameof(TriggerBossAttack), 0.5f); // 延迟0.5秒执行攻击
        }

        // 攻击结束后重置状态
        Invoke(nameof(ResetAttackState), attackDuration);
    }

    /// <summary>
    /// 触发Boss攻击
    /// </summary>
    private void TriggerBossAttack()
    {
        BossAttack bossAttack = GetComponent<BossAttack>();
        if (bossAttack != null)
        {
            bossAttack.PerformAttack();
        }
    }

    /// <summary>
    /// 重置攻击状态
    /// </summary>
    private void ResetAttackState()
    {
        isAttacking = false;
    }

    /// <summary>
    /// 更新动画状态
    /// </summary>
    private void UpdateAnimation()
    {
        if (bossAnimator == null) return;

        // 如果正在攻击
        if (isAttacking)
        {
            bossAnimator.SetBool("IsIdle", false);
            bossAnimator.SetBool("IsAttacking", true);
            bossAnimator.SetBool("IsMoving", false);
        }
        // 如果在移动
        else if (moveDirection != Vector2.zero && isPlayerInRange)
        {
            bossAnimator.SetBool("IsIdle", false);
            bossAnimator.SetBool("IsAttacking", false);
            bossAnimator.SetBool("IsMoving", true);
        }
        // 空闲状态
        else
        {
            bossAnimator.SetBool("IsIdle", true);
            bossAnimator.SetBool("IsAttacking", false);
            bossAnimator.SetBool("IsMoving", false);
        }
    }

    /// <summary>
    /// 检查是否正在攻击
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// 检查玩家是否在范围内
    /// </summary>
    public bool IsPlayerInRange()
    {
        return isPlayerInRange;
    }

    /// <summary>
    /// 更新Boss翻转（根据玩家位置）
    /// </summary>
    private void UpdateFlip()
    {
        if (player == null || bossSpriteRenderer == null)
        {
            return;
        }
        
        // 判断玩家在Boss的左侧还是右侧
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        // 如果方向改变，执行翻转
        if (shouldFaceRight != isFacingRight)
        {
            FlipSprite(shouldFaceRight);
        }
    }
    
    /// <summary>
    /// 翻转Boss精灵
    /// </summary>
    /// <param name="faceRight">是否面向右侧</param>
    private void FlipSprite(bool faceRight)
    {
        isFacingRight = faceRight;
        
        if (bossSpriteRenderer != null)
        {
            // 翻转X轴
            // 当面向右侧（isFacingRight = true）时，flipX = false（不翻转，正常显示）
            // 当面向左侧（isFacingRight = false）时，flipX = true（翻转）
            bossSpriteRenderer.flipX = !isFacingRight;
        }
    }
    
    /// <summary>
    /// 播放Boss音乐
    /// </summary>
    private void PlayBossMusic()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayBossMusic();
            Debug.Log($"[BossController] {gameObject.name} 第一次检测到玩家，播放Boss音乐");
        }
        else
        {
            Debug.LogWarning("[BossController] AudioManager实例未找到！无法播放Boss音乐。");
        }
    }
    
    /// <summary>
    /// 显示Boss弹窗
    /// </summary>
    private void ShowBossPopup()
    {
        Debug.Log($"[BossController] ShowBossPopup被调用，bossName: {bossName}");
        
        BossPopupUI popupUI = BossPopupUI.Instance;
        if (popupUI != null)
        {
            Debug.Log($"[BossController] 找到BossPopupUI实例: {popupUI.gameObject.name}");
            popupUI.ShowBossPopup(bossName);
            Debug.Log($"[BossController] {gameObject.name} 第一次检测到玩家，显示弹窗: {bossName}");
        }
        else
        {
            Debug.LogError("[BossController] BossPopupUI实例未找到！请确保场景中存在BossPopupUI组件。");
            Debug.LogError("[BossController] 提示：在Canvas下创建GameObject，添加BossPopupUI脚本");
        }
    }
    
    private void OnDrawGizmos()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 绘制停止距离
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        
        // 绘制移动方向
        if (moveDirection != Vector2.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }
    }
}

