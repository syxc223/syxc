using UnityEngine;

/// <summary>
/// 简单的摄像机碰撞检测：
/// 挂在场景里的实际相机（有 CinemachineBrain 的那台 Main Camera）上，
/// 会在 LateUpdate 里从玩家向相机发一条射线，若中途撞到墙，就把相机推到碰撞点附近，
/// 避免穿墙或被遮挡太多。
/// </summary>
public class CameraCollisionHandler : MonoBehaviour
{
    [Header("目标")]
    public Transform target;                 // 一般拖 Player（DimensionPlayerController）的 Transform

    [Header("碰撞设置")]
    public LayerMask collisionMask;          // 需要阻挡相机的层（墙、地形等）
    public float cameraRadius = 0.3f;        // 相机与障碍物之间保留的最小距离
    public float minDistance = 0.5f;         // 相机距离玩家的最近距离，防止相机缩进玩家身体内
    public float smoothing = 10f;            // 相机位置修正的平滑程度

    private Vector3 _desiredPosition;

    void LateUpdate()
    {
        if (target == null) return;

        // Cinemachine 已经在这一帧把相机移动到期望位置，这里先记录下来
        _desiredPosition = transform.position;

        Vector3 targetPos = target.position;
        Vector3 dir = _desiredPosition - targetPos;
        float distance = dir.magnitude;

        if (distance <= 0.001f)
            return;

        dir /= distance; // 归一化

        float maxCheckDistance = Mathf.Max(distance, minDistance);

        // 从玩家向相机方向发射一条射线，检测中途是否有遮挡
        if (Physics.SphereCast(
                targetPos,
                cameraRadius,
                dir,
                out RaycastHit hit,
                maxCheckDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            float clampedDist = Mathf.Max(hit.distance - cameraRadius, minDistance);
            Vector3 correctedPos = targetPos + dir * clampedDist;

            transform.position = Vector3.Lerp(
                transform.position,
                correctedPos,
                1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
        else
        {
            // 没有遮挡时，缓慢回到 Cinemachine 期望位置
            transform.position = Vector3.Lerp(
                transform.position,
                _desiredPosition,
                1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
    }
}

