using UnityEngine;

/// <summary>
/// 当小球碰到这个物体后，小球会一直向上飘。
/// 比较适合做“上升气流柱 / 光柱”之类的机关。
/// </summary>
public class FloatUpOnHit : MonoBehaviour
{
    [Header("向上飘的配置")]
    public float floatSpeed = 3f;          // 向上匀速的速度
    public float floatDrag = 0.5f;         // 飘起来时的阻力

    [Header("空中跳跃增强")]
    [Tooltip("在失重上升状态下，是否允许玩家按跳跃键获得额外向上冲力。")]
    public bool allowJumpBoost = true;
    [Tooltip("空中按一次跳跃键额外增加的向上速度。")]
    public float jumpBoost = 3f;

    private Rigidbody targetRb;
    private bool isFloating;
    private DimensionPlayerController targetPlayer;

    void Update()
    {
        if (!isFloating || targetRb == null) return;

        if (allowJumpBoost && Input.GetButtonDown("Jump"))
        {
            // 在当前上升速度基础上再给一段额外向上速度
            Vector3 v = targetRb.velocity;
            v.y += jumpBoost;
            targetRb.velocity = v;
        }
    }

    void FixedUpdate()
    {
        if (!isFloating || targetRb == null) return;

        // 如果已经恢复重力，认为浮空结束
        if (targetRb.useGravity)
        {
            isFloating = false;
            return;
        }

        Vector3 v = targetRb.velocity;
        v.y = floatSpeed;                  // 持续保持一个向上速度
        targetRb.velocity = v;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartFloat(other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>());
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryStartFloat(collision.rigidbody ?? collision.collider.GetComponentInParent<Rigidbody>());
    }

    void TryStartFloat(Rigidbody rb)
    {
        if (rb == null) return;

        targetRb = rb;
        isFloating = true;

        targetRb.useGravity = false;
        targetRb.drag = floatDrag;

        // 禁用玩家输入，并切换为 2D 视角
        targetPlayer = rb.GetComponent<DimensionPlayerController>() ?? rb.GetComponentInParent<DimensionPlayerController>();
        if (targetPlayer != null)
        {
            targetPlayer.enabled = false;           // 禁用移动/跳跃按键
            targetPlayer.is2D = true;              // 标记为 2D 模式

            var switcher = FindObjectOfType<DimensionSwitcher>();
            if (switcher != null)
            {
                switcher.BroadcastMessage("SwitchTo2D", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}

