using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class FipController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;//精灵渲染器引用
    [SerializeField] private Rigidbody2D entity;//刚体2D引用
    [Header("Flipping References")]//翻转参数
    public bool useMouseForFlipping = false;//是否使用鼠标进行翻转
    [SerializeField] private bool flipx = true;//是否翻转X轴
    private bool isFacingRight = true;//实体当前面向右侧


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // 如果 spriteRenderer 未赋值，自动获取组件
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // 如果仍然为空，尝试从子对象获取
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // 如果还是没有找到，输出警告
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"FipController: 在 {gameObject.name} 上未找到 SpriteRenderer 组件。请添加 SpriteRenderer 组件。");
        }
        
        // 如果 entity 未赋值，自动获取组件
        if (entity == null)
        {
            entity = GetComponent<Rigidbody2D>();
        }
        
        if (entity == null)
        {
            Debug.LogWarning($"FipController: 在 {gameObject.name} 上未找到 Rigidbody2D 组件。");
        }
    }
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (useMouseForFlipping)
        {
            FlipBasedOnMouse();
        }
        else
        {
            FilpBasedOnInput();
        }
    }
    
    private void FlipBasedOnMouse()
    {
        // 检查鼠标设备是否可用
        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }
        
        // 获取鼠标屏幕坐标（使用新的 Input System）
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        
        // 获取相机的像素矩形（视口范围）
        Rect cameraPixelRect = Camera.main.pixelRect;
        
        // 将鼠标坐标限制在相机视口范围内，避免超出视口警告
        mouseScreenPos.x = Mathf.Clamp(mouseScreenPos.x, cameraPixelRect.xMin, cameraPixelRect.xMax);
        mouseScreenPos.y = Mathf.Clamp(mouseScreenPos.y, cameraPixelRect.yMin, cameraPixelRect.yMax);
        
        // 将屏幕坐标转换为世界坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Camera.main.nearClipPlane));
        mouseWorldPos.z = 0;
        
        // 判断鼠标在实体的左侧还是右侧
        bool shouldFaceRight = mouseWorldPos.x > transform.position.x;
        if (shouldFaceRight != isFacingRight)
        {
            FlipSprite(shouldFaceRight);
        }
    }
    
    private void FilpBasedOnInput()
    {
        //检查是否存在显著水平输入
        if (entity != null && Mathf.Abs(entity.linearVelocity.x) > 0.01f)
        {
            // 向右移动（速度x > 0）时面向右侧，向左移动（速度x < 0）时面向左侧
            bool shouldFaceRight = entity.linearVelocity.x > 0;
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
        }
    }
    private void FlipSprite(bool faceRight)
    {
        isFacingRight = faceRight;
        if (flipx)
        {
            if (spriteRenderer != null)
            {
                // 修复：反转翻转逻辑以解决面向相反方向的问题
                // 当向右移动（isFacingRight = true）时，flipX = false（不翻转）
                // 当向左移动（isFacingRight = false）时，flipX = true（翻转）
                // 如果方向仍然相反，请检查角色的初始精灵方向
                spriteRenderer.flipX = isFacingRight;
            }
        }
        else
        {
            // 使用旋转而不是翻转X轴
            transform.rotation = Quaternion.Euler(0, isFacingRight ? 0 : 180, 0);
        }
    }
    public void SetFacingDirection(bool useMouse)
    {
        useMouseForFlipping=useMouse;
        if (useMouse)
        {
            FlipBasedOnMouse();
        }
        else
        {
            FilpBasedOnInput();
        }
    }
}
