using UnityEngine;

/// <summary>
/// 挂在玩家身上：
/// 当玩家在“失重 / 上升”阶段被禁用输入后，一旦再次碰到任何碰撞体，
/// 就恢复重力并重新开启玩家控制。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerFloatResetOnCollision : MonoBehaviour
{
    private Rigidbody rb;
    private DimensionPlayerController controller;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<DimensionPlayerController>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // 只有在玩家控制被禁用时才尝试恢复（不改变失重状态，只恢复按键）
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }
    }
}

