using System.Collections;
using UnityEngine;

/// <summary>
/// 控制主摄像机在传送门出现时做一个“靠近门 → 再回到玩家”的小过场。
/// 挂在主摄像机所在物体上，并在 Inspector 中指定玩家 Transform。
/// </summary>
public class PortalRevealCamera : MonoBehaviour
{
    [Header("玩家")]
    public Transform player;

    [Header("正常跟随参数")]
    public Vector3 normalOffset = new Vector3(0f, 15f, -10f);
    public float followSpeed = 5f;

    [Header("过场参数")]
    public float moveDuration = 1.2f;
    public float holdDuration = 1.0f;
    public float returnDuration = 1.2f;
    public float lookHeightOffset = 1.5f;

    private bool inCutscene;
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
    }

    void LateUpdate()
    {
        if (inCutscene || player == null) return;

        Vector3 desiredPos = player.position + normalOffset;
        cachedTransform.position = Vector3.Lerp(
            cachedTransform.position,
            desiredPos,
            followSpeed * Time.deltaTime
        );

        cachedTransform.LookAt(player.position);
    }

    /// <summary>
    /// 外部调用：播放“镜头靠近传送门”的过场。
    /// </summary>
    public void PlayReveal(Transform portal)
    {
        if (inCutscene || portal == null) return;
        StartCoroutine(DoReveal(portal));
    }

    IEnumerator DoReveal(Transform portal)
    {
        inCutscene = true;

        Vector3 startPos = cachedTransform.position;
        Quaternion startRot = cachedTransform.rotation;

        // 目标机位：从门的斜上方看过去
        Vector3 portalLookTarget = portal.position + Vector3.up * lookHeightOffset;
        Vector3 portalCamPos = portal.position + new Vector3(0f, 6f, -6f);

        // 1. 移动到门附近
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(moveDuration, 0.01f);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            cachedTransform.position = Vector3.Lerp(startPos, portalCamPos, eased);
            cachedTransform.rotation = Quaternion.Slerp(
                startRot,
                Quaternion.LookRotation(portalLookTarget - cachedTransform.position, Vector3.up),
                eased
            );

            yield return null;
        }

        // 2. 停留一会儿，给门上升动画时间
        float holdTimer = 0f;
        while (holdTimer < holdDuration)
        {
            holdTimer += Time.deltaTime;
            cachedTransform.LookAt(portalLookTarget);
            yield return null;
        }

        // 3. 相机回到玩家
        if (player != null)
        {
            Vector3 endPos = player.position + normalOffset;
            Quaternion endRot = Quaternion.LookRotation(player.position - endPos, Vector3.up);

            t = 0f;
            Vector3 cutStartPos = cachedTransform.position;
            Quaternion cutStartRot = cachedTransform.rotation;

            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(returnDuration, 0.01f);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                cachedTransform.position = Vector3.Lerp(cutStartPos, endPos, eased);
                cachedTransform.rotation = Quaternion.Slerp(cutStartRot, endRot, eased);

                yield return null;
            }
        }

        inCutscene = false;
    }
}

