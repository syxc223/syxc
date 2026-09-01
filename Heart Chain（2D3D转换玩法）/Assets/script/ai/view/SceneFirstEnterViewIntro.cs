using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 首次进入某个场景时，为相机添加一次“缓缓靠近”的视角过渡。
/// 用法：
/// 1. 把这个脚本挂在场景里的相机（或一个相机控制物体）上。
/// 2. 在 Inspector 里设置 target（通常是玩家或场景中心），以及起始偏移 / 最终偏移。
/// 3. 勾选/不勾选 usePlayerPrefs 决定是否跨游戏运行记忆“已经播过”。
/// </summary>
public class SceneFirstEnterViewIntro : MonoBehaviour
{
    [Header("目标")]
    public Transform target;                  // 相机最终要看向的目标（玩家或场景焦点）

    [Header("机位设置")]
    public Vector3 startOffset = new Vector3(0f, 15f, -20f); // 首次进入时的远景偏移
    public Vector3 endOffset = new Vector3(0f, 8f, -10f);    // 正常游玩时的机位偏移
    public float duration = 2.0f;                              // 靠近时长

    [Header("持久化设置")]
    [Tooltip("是否使用 PlayerPrefs 记住已经播放过该场景的视角过渡（跨游戏运行）。")]
    public bool usePlayerPrefs = true;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        string key = "SceneViewIntro_" + sceneName;

        bool alreadyPlayed = usePlayerPrefs && PlayerPrefs.GetInt(key, 0) == 1;
        if (alreadyPlayed)
        {
            // 非首次进入：直接设置为最终机位
            Vector3 endPos = target.position + endOffset;
            transform.position = endPos;
            transform.LookAt(target.position);
            return;
        }

        // 首次进入：从远机位缓缓靠近
        StartCoroutine(IntroRoutine(target, key));
    }

    IEnumerator IntroRoutine(Transform t, string prefsKey)
    {
        Vector3 startPos = t.position + startOffset;
        Vector3 endPos = t.position + endOffset;

        // 先把相机放到起始位置
        transform.position = startPos;
        transform.LookAt(t.position);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            // 位置缓缓靠近
            transform.position = Vector3.Lerp(startPos, endPos, k);
            transform.LookAt(t.position);

            yield return null;
        }

        transform.position = endPos;
        transform.LookAt(t.position);

        if (usePlayerPrefs)
        {
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
        }
    }
}

