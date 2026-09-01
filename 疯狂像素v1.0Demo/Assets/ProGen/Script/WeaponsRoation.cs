using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponsRoation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    private Vector3 mousePos;
    private Camera mainCamera;
    
    void Awake()
    {
        // 自动获取 SpriteRenderer 组件（如果未赋值）
        if (sprite == null)
        {
            sprite = GetComponent<SpriteRenderer>();
        }
        if (sprite == null)
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 优先使用 Camera.main，如果没有则查找带 MainCamera 标签的相机
        if (Camera.main != null)
        {
            mainCamera = Camera.main;
        }
        else
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
            {
                mainCamera = cameraObj.GetComponent<Camera>();
            }
        }
        
        // 如果仍然没有找到相机，输出警告
        if (mainCamera == null)
        {
            Debug.LogWarning("WeaponsRoation: 未找到主相机！");
        }
    }

    // Update is called once per frame
    void Update()
    {
        GunRoatation();
    }

    void GunRoatation()
    {
        // 检查相机和鼠标是否可用
        if (mainCamera == null || Mouse.current == null)
        {
            return;
        }
        
        // 获取鼠标屏幕坐标
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        
        // 将屏幕坐标转换为世界坐标（使用相机到武器的 Z 距离）
        float distanceToCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        if (distanceToCamera == 0f)
        {
            distanceToCamera = mainCamera.nearClipPlane;
        }
        mousePos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, distanceToCamera));
        
        // 计算方向向量
        Vector3 direction = mousePos - transform.position;
        direction.z = 0f; // 确保在 2D 平面上旋转
        
        // 计算旋转角度
        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 检测翻转是否超过范围（当枪指向后方时翻转精灵）
        if (sprite != null)
        {
            if (rotationZ < -90f || rotationZ > 90f)
            {
                sprite.flipY = true;
            }
            else
            {
                sprite.flipY = false;
            }
        }
        
        // 应用旋转
        transform.rotation = Quaternion.Euler(0, 0, rotationZ);
    }
}
