using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;//子弹存在时间
    [SerializeField] private int maxBounces = 2; // 最大反弹次数
    private CircleCollider2D bulletCollider;//子弹碰撞器引用
    private Rigidbody2D rb2D; // 子弹刚体引用
    private int bounceCount = 0; // 当前反弹次数
    private Vector2 previousPosition; // 上一帧位置（用于计算碰撞方向）
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifetime);//在指定时间后销毁子弹对象
        rb2D = GetComponent<Rigidbody2D>();
        previousPosition = transform.position;
    }

    void Update()
    {
        // 更新上一帧位置
        previousPosition = transform.position;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 调试信息：显示所有碰撞
        Debug.Log($"[Bullet] 子弹碰撞到: {collision.gameObject.name}, Tag: {collision.tag}");
        
        if(collision.gameObject.CompareTag("Solid"))
        {
            // 触发屏幕震动效果（子弹碰撞墙壁时）
            TriggerScreenShake();
            
            // 尝试反弹
            if (bounceCount < maxBounces)
            {
                Bounce(collision);
            }
            else
            {
                // 达到最大反弹次数，销毁子弹
                Debug.Log($"[Bullet] 子弹已达到最大反弹次数({maxBounces})，销毁子弹");
                Destroy(gameObject);
            }
        }
        
        // 注意：对敌人的伤害由 BulletDamage 组件处理（如果子弹上有该组件）
        // 如果没有 BulletDamage 组件，可以通过这里添加简化的伤害逻辑
    }

    /// <summary>
    /// 反弹子弹
    /// </summary>
    private void Bounce(Collider2D collision)
    {
        if (rb2D == null)
        {
            Debug.LogWarning("[Bullet] Rigidbody2D为空，无法反弹");
            Destroy(gameObject);
            return;
        }

        Vector2 bulletPos = transform.position;
        
        // 计算碰撞法线
        Vector2 normal = CalculateCollisionNormal(collision, bulletPos);
        
        // 获取当前速度方向
        Vector2 currentVelocity = rb2D.linearVelocity;
        Vector2 currentDirection = currentVelocity.normalized;
        
        // 使用Vector2.Reflect计算反射方向
        Vector2 reflectedDirection = Vector2.Reflect(currentDirection, normal);
        
        // 保持速度大小不变，只改变方向
        float speed = currentVelocity.magnitude;
        rb2D.linearVelocity = reflectedDirection * speed;
        
        // 更新子弹旋转以匹配新方向
        float angle = Mathf.Atan2(reflectedDirection.y, reflectedDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 增加反弹计数
        bounceCount++;
        Debug.Log($"[Bullet] 子弹反弹！反弹次数: {bounceCount}/{maxBounces}，新方向: {reflectedDirection}");
    }

    /// <summary>
    /// 计算碰撞法线
    /// </summary>
    private Vector2 CalculateCollisionNormal(Collider2D collision, Vector2 bulletPosition)
    {
        // 获取当前移动方向
        Vector2 currentDirection = rb2D.linearVelocity.normalized;
        
        // 获取碰撞体的边界框
        Bounds bounds = collision.bounds;
        Vector2 boundsMin = bounds.min;
        Vector2 boundsMax = bounds.max;
        
        // 计算到各边的距离
        float distanceToLeft = Mathf.Abs(bulletPosition.x - boundsMin.x);
        float distanceToRight = Mathf.Abs(bulletPosition.x - boundsMax.x);
        float distanceToBottom = Mathf.Abs(bulletPosition.y - boundsMin.y);
        float distanceToTop = Mathf.Abs(bulletPosition.y - boundsMax.y);
        
        // 结合移动方向和边界距离判断
        // 优先考虑移动方向，但如果距离差异明显，则使用距离判断
        
        // 如果水平移动分量更大
        if (Mathf.Abs(currentDirection.x) > Mathf.Abs(currentDirection.y))
        {
            // 水平移动，碰撞面应该是垂直的
            if (currentDirection.x > 0 && distanceToRight < distanceToLeft)
            {
                // 向右移动且更接近右边界
                return Vector2.left; // 法线向左
            }
            else if (currentDirection.x < 0 && distanceToLeft < distanceToRight)
            {
                // 向左移动且更接近左边界
                return Vector2.right; // 法线向右
            }
            else
            {
                // 使用距离判断
                return distanceToLeft < distanceToRight ? Vector2.right : Vector2.left;
            }
        }
        else
        {
            // 垂直移动，碰撞面应该是水平的
            if (currentDirection.y > 0 && distanceToTop < distanceToBottom)
            {
                // 向上移动且更接近上边界
                return Vector2.down; // 法线向下
            }
            else if (currentDirection.y < 0 && distanceToBottom < distanceToTop)
            {
                // 向下移动且更接近下边界
                return Vector2.up; // 法线向上
            }
            else
            {
                // 使用距离判断
                return distanceToBottom < distanceToTop ? Vector2.up : Vector2.down;
            }
        }
    }
    
    /// <summary>
    /// 触发屏幕震动效果
    /// </summary>
    private void TriggerScreenShake()
    {
        // 查找场景中的 GunShooting 组件并触发震动
        GunShooting gunShooting = FindFirstObjectByType<GunShooting>();
        if (gunShooting != null)
        {
            gunShooting.ScreenShake();
        }
    }
}
