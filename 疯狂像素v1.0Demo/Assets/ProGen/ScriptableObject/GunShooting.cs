using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class GunShooting : MonoBehaviour
{
    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;//子弹预制体引用
    [SerializeField] private Transform gunMuzzleTransform;//枪口位置引用
    [SerializeField] private ShootingPattern shootingPattern;//射击模式引用（单发/三连发等）
    [SerializeField] private float bulletSpeed = 10f;//子弹速度
    [SerializeField] private float fireRate = 0.3f;//射击间隔时间
    [SerializeField] private SpriteRenderer gunSpriteRenderer;//枪械精灵渲染器引用
    [SerializeField] private FipController flipController;//翻转控制器引用
    [SerializeField] private CinemachineImpulseSource camImpulse;//摄像机冲击源引用
    [Header("音效设置")]
    [SerializeField] private bool playShootSound = true; // 是否播放开枪音效
    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 30;//弹药上限
    [SerializeField] private int currentAmmo = 0;//当前弹药

    private PlayerInput gunInput;
    private InputAction shootAction;
    private InputAction gunAction;
    private float shootTimer = 0f;//射击计时器
    private bool isVisible = false;//枪械可见性状态
    private bool isShooting = false;//是否正在射击

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public event Action<int, int> OnAmmoChanged;


    void Awake()
    {
        gunInput = GetComponent<PlayerInput>();
        
        if (gunInput != null && gunInput.actions != null)
        {
            shootAction = gunInput.actions["Shoot"];
            gunAction = gunInput.actions["GunKey"];
            
            // 启用 InputAction
            if (shootAction != null)
            {
                shootAction.Enable();
            }
            if (gunAction != null)
            {
                gunAction.Enable();
            }
        }
        
        // 如果未在 Inspector 中设置，自动获取 CinemachineImpulseSource
        if (camImpulse == null)
        {
            camImpulse = GetComponent<CinemachineImpulseSource>();
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 如果未指定枪口位置，使用当前 Transform
        if (gunMuzzleTransform == null)
        {
            gunMuzzleTransform = transform;
        }

        // 初始化弹药
        if (maxAmmo <= 0)
        {
            maxAmmo = 30;
        }
        if (currentAmmo <= 0 || currentAmmo > maxAmmo)
        {
            currentAmmo = maxAmmo;
        }
        NotifyAmmoChanged();
    }

    // Update is called once per frame
    void Update()
    {
        // 更新射击计时器
        if (shootTimer > 0)
        {
            shootTimer -= Time.deltaTime;
        }
        Shooting();
        GunVisiable();
        
    }
    
    void GunVisiable()
    {
        if (gunAction != null && gunAction.WasPerformedThisFrame())
        {
            //切换可见性状态
            isVisible = !isVisible;
            //根据状态更新精灵渲染器的启用状态
            if (gunSpriteRenderer != null)
            {
                gunSpriteRenderer.enabled = isVisible;
            }
            if (flipController != null)
            {
                flipController.SetFacingDirection(isVisible);
            }
        }
    }

    /// <summary>
    /// 外部调用设置武器可见性（用于武器切换系统）
    /// </summary>
    public void SetWeaponVisible(bool visible)
    {
        isVisible = visible;
        if (gunSpriteRenderer != null)
        {
            gunSpriteRenderer.enabled = visible;
        }
        if (flipController != null)
        {
            flipController.SetFacingDirection(visible);
        }
    }
    void Shooting()
    {
        if (!isVisible)
        {
            isShooting = false;
            return;
        }
        
        // 检测鼠标左键点击或 InputAction 射击
        bool shouldShoot = false;
        
        // 优先检测鼠标左键点击
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            shouldShoot = true;
        }
        // 如果没有鼠标输入，则使用 InputAction
        else if (shootAction != null && shootAction.WasPerformedThisFrame())
        {
            shouldShoot = true;
        }
        
        // 计算本次射击需要的弹药数
        int requiredAmmo = (shootingPattern is TripleShotPattern) ? 3 : 1;
        
        // 检查是否可以射击（冷却时间已过且弹药足够）
        if (shouldShoot && shootTimer <= 0 && currentAmmo >= requiredAmmo)
        {
            isShooting = true;
            shootTimer = fireRate;
            Shoot(); // 执行射击
        }
        else if (shootAction != null && shootAction.WasReleasedThisFrame())
        {
            isShooting = false;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isShooting = false;
        }
    }
    
    void Shoot()
    {
        // 检查必要的引用
        if (bulletPrefab == null || gunMuzzleTransform == null)
        {
            Debug.LogWarning($"[GunShooting] {gameObject.name}: bulletPrefab 或 gunMuzzleTransform 未设置！");
            return;
        }
        
        // 调试日志：显示当前使用的子弹预制体
        Debug.Log($"[GunShooting] {gameObject.name} 射击，使用子弹预制体: {bulletPrefab.name}");
        
        // 播放开枪音效
        if (playShootSound)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.PlayShootSound();
            }
        }
        
        // 计算本次射击消耗的弹药数
        int ammoCost = 1; // 默认消耗1发
        
        // 如果设置了射击模式，使用射击模式；否则直接射击
        if (shootingPattern != null)
        {
            // 检查是否是三连发模式，如果是则消耗3发弹药
            if (shootingPattern is TripleShotPattern)
            {
                ammoCost = 3;
            }
            
            // 将射击委托给模块化射击模式
            shootingPattern.Fire(bulletPrefab, gunMuzzleTransform, bulletSpeed);
        }
        else
        {
            // 直接实现射击逻辑（不使用射击模式）
            DirectShoot();
        }
        
        // 扣除弹药
        currentAmmo = Mathf.Max(0, currentAmmo - ammoCost);
        NotifyAmmoChanged();

        // 注意：屏幕震动现在由子弹碰撞墙壁时触发，而不是射击时
    }
    
    /// <summary>
    /// 直接射击（不使用射击模式）
    /// </summary>
    private void DirectShoot()
    {
        // 实例化子弹
        GameObject bullet = Instantiate(bulletPrefab, gunMuzzleTransform.position, gunMuzzleTransform.rotation);
        
        // 为子弹添加速度
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = gunMuzzleTransform.right * bulletSpeed;
        }
        else
        {
            Debug.LogWarning("GunShooting: 子弹预制体缺少 Rigidbody2D 组件！");
        }
    }
    
    /// <summary>
    /// 获取当前是否正在射击
    /// </summary>
    public bool IsShooting()
    {
        return isShooting;
    }
    /// <summary>
    /// 触发屏幕震动效果（由子弹碰撞墙壁时调用）
    /// </summary>
    public void ScreenShake()
    {
        if (camImpulse != null)
        {
            camImpulse.GenerateImpulse();
        }
    }

    /// <summary>
    /// 外部调用以补充弹药（如拾取弹药包）
    /// </summary>
    public void AddAmmo(int amount)
    {
        if (amount <= 0) return;
        currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
        NotifyAmmoChanged();
    }

    /// <summary>
    /// 设置子弹预制体（外部调用，用于武器管理器动态切换子弹）
    /// </summary>
    public void SetBulletPrefab(GameObject newBulletPrefab)
    {
        if (newBulletPrefab != null)
        {
            bulletPrefab = newBulletPrefab;
            Debug.Log($"[GunShooting] {gameObject.name} 的子弹预制体已设置为: {newBulletPrefab.name}");
        }
        else
        {
            Debug.LogWarning($"[GunShooting] {gameObject.name} 尝试设置空的子弹预制体！");
        }
    }

    /// <summary>
    /// 获取当前子弹预制体
    /// </summary>
    public GameObject GetBulletPrefab()
    {
        return bulletPrefab;
    }

    /// <summary>
    /// 通知 UI 弹药变化
    /// </summary>
    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }
}
