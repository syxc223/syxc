using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器管理器
/// 管理多把武器的切换和显示
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("武器设置")]
    [SerializeField] private WeaponData[] weapons; // 武器数组
    [SerializeField] private int currentWeaponIndex = 0; // 当前武器索引

    [Header("切换设置")]
    [SerializeField] private Key switchWeaponKey = Key.Q; // 切换武器按键

    private PlayerInput playerInput;
    private InputAction switchWeaponAction;

    /// <summary>
    /// 武器数据类
    /// </summary>
    [System.Serializable]
    public class WeaponData
    {
        [Tooltip("武器名称")]
        public string weaponName;
        
        [Tooltip("武器GameObject（包含GunShooting组件）")]
        public GameObject weaponObject;
        
        [Tooltip("武器精灵渲染器（用于显示/隐藏）")]
        public SpriteRenderer weaponSpriteRenderer;
        
        [Tooltip("是否默认激活")]
        public bool isDefault = false;
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        
        // 尝试从InputSystem获取切换武器动作
        if (playerInput != null && playerInput.actions != null)
        {
            // 检查是否有SwitchWeapon动作，如果没有则使用键盘直接检测
            switchWeaponAction = playerInput.actions.FindAction("SwitchWeapon");
            if (switchWeaponAction != null)
            {
                switchWeaponAction.Enable();
            }
        }
    }

    private void Start()
    {
        // 初始化武器
        InitializeWeapons();
    }

    private void Update()
    {
        // 检测切换武器输入
        if (CheckSwitchWeaponInput())
        {
            SwitchWeapon();
        }
    }

    /// <summary>
    /// 检查切换武器输入
    /// </summary>
    private bool CheckSwitchWeaponInput()
    {
        // 优先使用InputAction
        if (switchWeaponAction != null && switchWeaponAction.WasPressedThisFrame())
        {
            return true;
        }
        
        // 如果没有InputAction，直接检测键盘
        if (Keyboard.current != null && Keyboard.current[switchWeaponKey].wasPressedThisFrame)
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 初始化所有武器
    /// </summary>
    private void InitializeWeapons()
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogWarning("WeaponManager: 未设置任何武器！请在Inspector中设置Weapons数组。");
            Debug.LogWarning("WeaponManager: 设置步骤：1. 找到Weapon Manager组件 2. 展开Weapons数组 3. 设置Size为2 4. 为每个元素设置Weapon Object");
            return;
        }

        // 找到默认武器
        int defaultIndex = 0;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].isDefault)
            {
                defaultIndex = i;
                break;
            }
        }

        // 激活默认武器，禁用其他武器
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == defaultIndex)
            {
                SetWeaponActive(i, true);
                currentWeaponIndex = i;
            }
            else
            {
                SetWeaponActive(i, false);
            }
        }

        Debug.Log($"WeaponManager: 初始化完成，当前武器: {weapons[currentWeaponIndex].weaponName}");
    }

    /// <summary>
    /// 切换武器
    /// </summary>
    public void SwitchWeapon()
    {
        if (weapons == null || weapons.Length <= 1)
        {
            Debug.LogWarning("WeaponManager: 武器数量不足，无法切换！");
            return;
        }

        // 禁用当前武器
        SetWeaponActive(currentWeaponIndex, false);

        // 切换到下一把武器
        currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Length;

        // 启用新武器
        SetWeaponActive(currentWeaponIndex, true);

        Debug.Log($"WeaponManager: 切换到武器: {weapons[currentWeaponIndex].weaponName}");
    }

    /// <summary>
    /// 设置武器激活状态
    /// </summary>
    private void SetWeaponActive(int index, bool active)
    {
        if (index < 0 || index >= weapons.Length)
        {
            return;
        }

        WeaponData weapon = weapons[index];
        
        // 激活/禁用武器对象
        if (weapon.weaponObject != null)
        {
            weapon.weaponObject.SetActive(active);
        }

        // 显示/隐藏武器精灵
        if (weapon.weaponSpriteRenderer != null)
        {
            weapon.weaponSpriteRenderer.enabled = active;
        }

        // 启用/禁用GunShooting组件
        GunShooting gunShooting = weapon.weaponObject != null 
            ? weapon.weaponObject.GetComponent<GunShooting>() 
            : null;
        
        if (gunShooting != null)
        {
            gunShooting.enabled = active;
            
            // 如果激活，设置武器可见
            if (active)
            {
                gunShooting.SetWeaponVisible(true);
            }
        }
    }

    /// <summary>
    /// 获取当前武器
    /// </summary>
    public WeaponData GetCurrentWeapon()
    {
        if (weapons != null && currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Length)
        {
            return weapons[currentWeaponIndex];
        }
        return null;
    }

    /// <summary>
    /// 切换到指定武器
    /// </summary>
    public void SwitchToWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length)
        {
            Debug.LogWarning($"WeaponManager: 武器索引 {index} 无效！");
            return;
        }

        SetWeaponActive(currentWeaponIndex, false);
        currentWeaponIndex = index;
        SetWeaponActive(currentWeaponIndex, true);
    }
}

