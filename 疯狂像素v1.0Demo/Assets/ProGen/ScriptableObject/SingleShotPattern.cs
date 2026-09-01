using UnityEngine;

/// <summary>
/// 单发射击模式组件
/// 每次射击只发射一颗子弹
/// </summary>
[AddComponentMenu("Scripts/Single Shot Pattern")]
public class SingleShotPattern : ShootingPattern
{
    [Header("射击参数")]
    [SerializeField] private float fireRate = 0.5f; // 射击间隔（秒）
    [SerializeField] private float bulletSpeed = 10f; // 子弹速度
    
    [Header("引用")]
    [SerializeField] private GameObject bulletPrefab; // 子弹预制体
    [SerializeField] private Transform muzzle; // 枪口位置（如果为空，使用当前 Transform）
    
    private float lastFireTime = 0f; // 上次射击时间
    
    void Awake()
    {
        // 如果未指定枪口位置，使用当前 Transform
        if (muzzle == null)
        {
            muzzle = transform;
        }
    }
    
    /// <summary>
    /// 执行射击（单发）
    /// </summary>
    public override void Fire(GameObject bulletPrefab, Transform muzzle, float bulletSpeed)
    {
        // 检查射击冷却时间
        if (Time.time - lastFireTime < fireRate)
        {
            return; // 还在冷却中，不能射击
        }
        
        // 使用传入的参数，如果没有则使用序列化字段
        GameObject prefabToUse = bulletPrefab != null ? bulletPrefab : this.bulletPrefab;
        Transform muzzleToUse = muzzle != null ? muzzle : this.muzzle;
        float speedToUse = bulletSpeed > 0 ? bulletSpeed : this.bulletSpeed;
        
        if (prefabToUse == null || muzzleToUse == null)
        {
            Debug.LogWarning("SingleShotPattern: bulletPrefab 或 muzzle 为空！");
            return;
        }
        
        // 实例化子弹
        GameObject bullet = Instantiate(prefabToUse, muzzleToUse.position, muzzleToUse.rotation);
        
        // 为子弹添加速度
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = muzzleToUse.right * speedToUse;
        }
        
        // 更新射击时间
        lastFireTime = Time.time;
    }
    
    /// <summary>
    /// 使用默认参数射击（方便外部调用）
    /// </summary>
    public void FireDefault()
    {
        if (bulletPrefab == null || muzzle == null)
        {
            Debug.LogWarning("SingleShotPattern: 请先在 Inspector 中设置 bulletPrefab 和 muzzle！");
            return;
        }
        
        // 检查射击冷却时间
        if (Time.time - lastFireTime < fireRate)
        {
            return; // 还在冷却中，不能射击
        }
        
        // 实例化子弹
        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        
        // 为子弹添加速度
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = muzzle.right * bulletSpeed;
        }
        
        // 更新射击时间
        lastFireTime = Time.time;
    }
    
    /// <summary>
    /// 检查是否可以射击（冷却时间是否已过）
    /// </summary>
    public bool CanFire()
    {
        return Time.time - lastFireTime >= fireRate;
    }
}
