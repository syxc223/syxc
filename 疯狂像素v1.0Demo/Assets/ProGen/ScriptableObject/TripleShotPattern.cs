using UnityEngine;

/// <summary>
/// 三连发射击模式组件
/// 每次射击同时发射三颗子弹：正前方、左前方、右前方
/// </summary>
[AddComponentMenu("Scripts/Triple Shot Pattern")]
public class TripleShotPattern : ShootingPattern
{
    [Header("射击参数")]
    [SerializeField] private float fireRate = 0.5f; // 射击间隔（秒）
    [SerializeField] private float bulletSpeed = 10f; // 子弹速度
    [SerializeField] [Range(5f, 45f)] private float spreadAngle = 15f; // 左右子弹的散射角度（度）
    
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
    /// 执行射击（三连发）
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
            Debug.LogWarning("TripleShotPattern: bulletPrefab 或 muzzle 为空！");
            return;
        }
        
        // 获取基础方向（枪口朝向）
        Vector2 baseDirection = muzzleToUse.right;
        
        // 发射三颗子弹
        // 1. 中间子弹（正前方）
        SpawnBullet(prefabToUse, muzzleToUse.position, muzzleToUse.rotation, baseDirection, speedToUse);
        
        // 2. 左侧子弹（向左偏转）
        float leftAngle = -spreadAngle; // 负角度表示向左
        Quaternion leftRotation = Quaternion.Euler(0, 0, muzzleToUse.eulerAngles.z + leftAngle);
        Vector2 leftDirection = leftRotation * Vector2.right;
        SpawnBullet(prefabToUse, muzzleToUse.position, leftRotation, leftDirection, speedToUse);
        
        // 3. 右侧子弹（向右偏转）
        float rightAngle = spreadAngle; // 正角度表示向右
        Quaternion rightRotation = Quaternion.Euler(0, 0, muzzleToUse.eulerAngles.z + rightAngle);
        Vector2 rightDirection = rightRotation * Vector2.right;
        SpawnBullet(prefabToUse, muzzleToUse.position, rightRotation, rightDirection, speedToUse);
        
        // 更新射击时间
        lastFireTime = Time.time;
    }
    
    /// <summary>
    /// 生成单颗子弹
    /// </summary>
    private void SpawnBullet(GameObject prefab, Vector3 position, Quaternion rotation, Vector2 direction, float speed)
    {
        // 实例化子弹
        GameObject bullet = Instantiate(prefab, position, rotation);
        
        // 为子弹添加速度
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
            
            // 根据方向调整子弹旋转
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            Debug.LogWarning("TripleShotPattern: 子弹预制体缺少 Rigidbody2D 组件！");
        }
    }
    
    /// <summary>
    /// 使用默认参数射击（方便外部调用）
    /// </summary>
    public void FireDefault()
    {
        if (bulletPrefab == null || muzzle == null)
        {
            Debug.LogWarning("TripleShotPattern: 请先在 Inspector 中设置 bulletPrefab 和 muzzle！");
            return;
        }
        
        Fire(bulletPrefab, muzzle, bulletSpeed);
    }
    
    /// <summary>
    /// 检查是否可以射击（冷却时间是否已过）
    /// </summary>
    public bool CanFire()
    {
        return Time.time - lastFireTime >= fireRate;
    }
    
    /// <summary>
    /// 设置散射角度
    /// </summary>
    public void SetSpreadAngle(float angle)
    {
        spreadAngle = Mathf.Clamp(angle, 5f, 45f);
    }
    
    /// <summary>
    /// 获取当前散射角度
    /// </summary>
    public float GetSpreadAngle()
    {
        return spreadAngle;
    }
}

