using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 吃豆人场景用的传送门过场：
/// 玩家进入传送门后，先播放一段“场景塌陷 / 位移”动画，再延迟切换到下一个场景。
/// </summary>
public class PacmanPortalCutscene : MonoBehaviour
{
    [Header("玩家设置")]
    [Tooltip("玩家脚本，用于在过场时禁用控制。可留空，脚本会自动在触发时查找 PacmanPlayerController。")]
    public PacmanPlayerController playerController;

    [Header("下沉物体")]
    [Tooltip("会向下沉的物体（如地板、墙等）。")]
    public Transform[] sinkObjects;
    public float sinkDistance = 3f;       // 下沉距离
    public float sinkDuration = 1.5f;     // 下沉时间

    [Header("位移物体")]
    [Tooltip("需要改变位置的物体。")]
    public Transform[] moveObjects;
    [Tooltip("位移目标位置（与 moveObjects 一一对应）。")]
    public Transform[] moveTargets;
    public float moveDuration = 1.5f;

    [Header("过场与切换")]
    [Tooltip("动画播放完后再等待的额外时间。")]
    public float extraDelayBeforeLoad = 0.5f;
    [Tooltip("要切换到的下一个场景名。")]
    public string nextSceneName;

    [Header("摄像机过场")]
    [Tooltip("可选：在过场期间将主摄像机缓慢移动到这个位置/朝向。")]
    public Transform cameraTargetPoint;
    public float cameraMoveDuration = 1.5f;

    private bool _played;

    private void OnTriggerEnter(Collider other)
    {
        TryStartCutscene(other.GetComponentInParent<PacmanPlayerController>());
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryStartCutscene(collision.collider.GetComponentInParent<PacmanPlayerController>());
    }

    void TryStartCutscene(PacmanPlayerController player)
    {
        if (_played) return;
        if (player == null) return;

        _played = true;

        if (playerController == null)
            playerController = player;

        // 禁用玩家控制 & 物理移动，让玩家在过场中不要乱动
        if (playerController != null)
        {
            playerController.enabled = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        StartCoroutine(PlayCutsceneAndLoad());
    }

    IEnumerator PlayCutsceneAndLoad()
    {
        // 记录初始位置
        Vector3[] sinkStart = new Vector3[sinkObjects != null ? sinkObjects.Length : 0];
        Vector3[] sinkEnd = new Vector3[sinkStart.Length];
        for (int i = 0; i < sinkStart.Length; i++)
        {
            if (sinkObjects[i] == null) continue;
            sinkStart[i] = sinkObjects[i].position;
            sinkEnd[i] = sinkStart[i] + Vector3.down * sinkDistance;
        }

        Vector3[] moveStart = new Vector3[moveObjects != null ? moveObjects.Length : 0];
        Vector3[] moveEnd = new Vector3[moveStart.Length];
        int moveCount = Mathf.Min(moveObjects != null ? moveObjects.Length : 0, moveTargets != null ? moveTargets.Length : 0);
        for (int i = 0; i < moveCount; i++)
        {
            if (moveObjects[i] == null || moveTargets[i] == null) continue;
            moveStart[i] = moveObjects[i].position;
            moveEnd[i] = moveTargets[i].position;
        }

        float t = 0f;
        float maxDuration = Mathf.Max(sinkDuration, moveDuration, 0.01f);

        // 摄像机相关
        Camera mainCam = Camera.main;
        Vector3 camStartPos = Vector3.zero;
        Quaternion camStartRot = Quaternion.identity;
        if (mainCam != null)
        {
            camStartPos = mainCam.transform.position;
            camStartRot = mainCam.transform.rotation;
        }

        while (t < 1f)
        {
            t += Time.deltaTime / maxDuration;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            // 下沉动画
            for (int i = 0; i < sinkStart.Length; i++)
            {
                if (sinkObjects[i] == null) continue;
                sinkObjects[i].position = Vector3.Lerp(sinkStart[i], sinkEnd[i], eased);
            }

            // 位移动画
            for (int i = 0; i < moveCount; i++)
            {
                if (moveObjects[i] == null || moveTargets[i] == null) continue;
                float lerpT = Mathf.Clamp01(t * maxDuration / Mathf.Max(moveDuration, 0.01f));
                float lerpEased = Mathf.SmoothStep(0f, 1f, lerpT);
                moveObjects[i].position = Vector3.Lerp(moveStart[i], moveEnd[i], lerpEased);
            }

            // 摄像机缓慢移动到指定机位
            if (mainCam != null && cameraTargetPoint != null)
            {
                float camT = Mathf.Clamp01(t * maxDuration / Mathf.Max(cameraMoveDuration, 0.01f));
                float camEased = Mathf.SmoothStep(0f, 1f, camT);
                mainCam.transform.position = Vector3.Lerp(camStartPos, cameraTargetPoint.position, camEased);
                mainCam.transform.rotation = Quaternion.Slerp(camStartRot, cameraTargetPoint.rotation, camEased);
            }

            yield return null;
        }

        // 保证最终落在目标位置
        for (int i = 0; i < sinkStart.Length; i++)
        {
            if (sinkObjects[i] == null) continue;
            sinkObjects[i].position = sinkEnd[i];
        }
        for (int i = 0; i < moveCount; i++)
        {
            if (moveObjects[i] == null || moveTargets[i] == null) continue;
            moveObjects[i].position = moveEnd[i];
        }

        if (extraDelayBeforeLoad > 0f)
            yield return new WaitForSeconds(extraDelayBeforeLoad);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}

