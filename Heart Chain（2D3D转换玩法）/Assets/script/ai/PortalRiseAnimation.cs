using UnityEngine;

/// <summary>
/// 简单的“缓缓上升”动画：传送门生成时，从地下缓慢升到目标高度。
/// 把这个脚本挂到传送门预制体上即可。
/// </summary>
public class PortalRiseAnimation : MonoBehaviour
{
    [Header("上升设置")]
    public float riseDistance = 2f;      // 上升高度
    public float duration = 1.5f;        // 上升时间（秒）
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 startPos;
    private Vector3 endPos;
    private float timer;

    void OnEnable()
    {
        endPos = transform.position;
        startPos = endPos - Vector3.up * riseDistance;
        transform.position = startPos;
        timer = 0f;
    }

    void Update()
    {
        if (timer >= duration) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float eased = curve.Evaluate(t);

        transform.position = Vector3.Lerp(startPos, endPos, eased);
    }
}

