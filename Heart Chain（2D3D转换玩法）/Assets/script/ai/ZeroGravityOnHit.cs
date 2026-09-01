using UnityEngine;

/// <summary>
/// 当小球（或任何带 Rigidbody 的物体）碰到这个物体时，让它“失重”。
/// 用法：
/// 1. 把这个脚本挂在你想触发失重效果的物体上（比如特殊平台 / 区域）。
/// 2. 在该物体上添加 Collider：
///    - 如果勾选 IsTrigger，则使用 OnTriggerEnter 触发；
///    - 否则使用 OnCollisionEnter 触发。
/// 3. 玩家小球身上必须有 Rigidbody。
/// </summary>
public class ZeroGravityOnHit : MonoBehaviour
{
    [Header("失重设置")]
    [Tooltip("是否在进入触发后立刻给一个向上的瞬时推力")]
    public float upwardBoost = 2f;

    [Tooltip("进入失重后，将重力关闭。")]
    public bool disableGravity = true;

    [Tooltip("失重后是否减小线性阻力，让小球更飘")]
    public float dragInZeroGravity = 0.1f;

    [Header("空中跳跃增强")]
    [Tooltip("在失重状态下是否允许玩家按跳跃键获得额外向上冲力。")]
    public bool allowJumpBoost = true;
    [Tooltip("空中按一次跳跃键额外增加的向上速度。")]
    public float jumpBoost = 3f;

    private Rigidbody targetRb;
    private bool isZeroGravity;
    private DimensionPlayerController targetPlayer;

    void Update()
    {
        if (!isZeroGravity || targetRb == null) return;

        // 重力恢复后，认为失重结束
        if (targetRb.useGravity)
        {
            isZeroGravity = false;
            return;
        }

        if (allowJumpBoost && Input.GetButtonDown("Jump"))
        {
            Vector3 v = targetRb.velocity;
            v.y += jumpBoost;
            targetRb.velocity = v;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyZeroGravity(other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>());
    }

    private void OnCollisionEnter(Collision collision)
    {
        ApplyZeroGravity(collision.rigidbody ?? collision.collider.GetComponentInParent<Rigidbody>());
    }

    void ApplyZeroGravity(Rigidbody rb)
    {
        if (rb == null) return;

        targetRb = rb;
        isZeroGravity = true;

        // 禁用玩家输入，并切换为 2D 视角
        targetPlayer = rb.GetComponent<DimensionPlayerController>() ?? rb.GetComponentInParent<DimensionPlayerController>();
        if (targetPlayer != null)
        {
            targetPlayer.enabled = false;
            targetPlayer.is2D = true;

            var switcher = FindObjectOfType<DimensionSwitcher>();
            if (switcher != null)
            {
                switcher.BroadcastMessage("SwitchTo2D", SendMessageOptions.DontRequireReceiver);
            }
        }

        if (disableGravity)
        {
            rb.useGravity = false;
        }

        // 调整 Drag，让运动更“飘”
        if (dragInZeroGravity >= 0f)
        {
            rb.drag = dragInZeroGravity;
        }

        if (Mathf.Abs(upwardBoost) > 0.001f)
        {
            rb.AddForce(Vector3.up * upwardBoost, ForceMode.VelocityChange);
        }
    }
}

