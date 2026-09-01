using UnityEngine;

/// <summary>
/// 玩家触碰后被传送回出生点。
/// 用法：
/// 1. 将本脚本挂在一个带 Collider 的物体上（可以勾选 IsTrigger）。
/// 2. 在 Inspector 中设置 spawnPoint（出生点）；
///    - 若留空，会使用玩家当前的初始位置作为出生点。
/// 3. 玩家物体上需要有 Rigidbody + DimensionPlayerController（或任意脚本，只要有 Transform）。
/// </summary>
public class PlayerTeleportToSpawn : MonoBehaviour
{
    [Header("出生点设置")]
    public Transform spawnPoint;                 // 指定的出生点（可选）

    private Vector3 fallbackSpawnPosition;
    private bool hasFallback;

    void Start()
    {
        // 如果没有显式设置 spawnPoint，可以在第一次检测到玩家时记录他的位置作为出生点
    }

    void OnTriggerEnter(Collider other)
    {
        TryTeleport(other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>());
    }

    void OnCollisionEnter(Collision collision)
    {
        TryTeleport(collision.rigidbody ?? collision.collider.GetComponentInParent<Rigidbody>());
    }

    void TryTeleport(Rigidbody rb)
    {
        if (rb == null) return;

        Transform player = rb.transform;

        // 若尚未记录备用出生点，则以玩家当前初始位置为 fallback
        if (!hasFallback)
        {
            fallbackSpawnPosition = player.position;
            hasFallback = true;
        }

        Vector3 targetPos = spawnPoint != null ? spawnPoint.position : fallbackSpawnPosition;

        // 直接传送位置，并清理速度，避免继续飞出去
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = targetPos;
    }
}

