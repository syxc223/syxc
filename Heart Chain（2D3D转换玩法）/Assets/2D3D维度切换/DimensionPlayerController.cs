using UnityEngine;

public class DimensionPlayerController : MonoBehaviour
{
    public bool is2D;
    private void SwitchTo2D()
    {
        is2D = true;
    }
    private void SwitchTo3D()
    {
        is2D = false;
    }

    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 代码设置了刚体旋转锁定，也可以在编辑中勾选设置
        rb.constraints = RigidbodyConstraints.FreezeRotationX 
                        | RigidbodyConstraints.FreezeRotationZ
                        | RigidbodyConstraints.FreezeRotationY;
    }
    private void Update()
    {
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        var movement = Vector3.zero;

        if (is2D)
        {
            // --- 2D 模式逻辑 ---
            // 只能左右移动 (X轴)，Z轴锁定
            movement = new Vector3(h, 0, 0) * moveSpeed;
        }
        else
        {
            // --- 3D 模式逻辑 ---
            // 基于摄像机方向计算移动
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            
            // 将方向投影到水平面上
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // 根据摄像机方向计算移动向量
            movement = (forward * v + right * h) * moveSpeed;
        }

        // 应用移动
        Vector3 velocity = rb.velocity;
        velocity.x = movement.x;
        if (!is2D)
        {
            // 2D模式下不改变Z速度
            velocity.z = movement.z;
        }
        rb.velocity = velocity;

        // 跳跃 (通用)
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    private bool IsGrounded()
    {
        // 简单的射线检测地面
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}
