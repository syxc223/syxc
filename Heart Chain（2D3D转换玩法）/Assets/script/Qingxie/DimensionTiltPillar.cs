using System.Collections;
using UnityEngine;

public class DimensionTiltPillar : MonoBehaviour
{
    [Header("倾斜设置")]
    public float targetAngle = 90f;      // 最终倾斜角度（例如 90 表示完全倒下）
    public float duration = 1.0f;        // 倾斜用时
    public Vector3 tiltAxis = Vector3.forward; // 围绕哪条轴倾斜：forward = 绕 Z 轴

    private bool hasTilted = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasTilted) return;

        // 有可能碰撞到的是玩家子物体的 Collider，用 InParent 更稳
        var player = collision.collider.GetComponentInParent<DimensionPlayerController>();
        if (player == null) return;

        // 只允许在 2D 模式触发
        if (!player.is2D) return;

        StartCoroutine(TiltRoutine());
    }

    IEnumerator TiltRoutine()
    {
        hasTilted = true;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.AngleAxis(targetAngle, tiltAxis) * startRot;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.01f);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
            yield return null;
        }

        transform.rotation = endRot;
    }
}