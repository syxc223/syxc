using UnityEngine;

public class RandomMovement : MonoBehaviour
{
    [SerializeField] private float amplitudeX = 2f; // 水平波幅
    [SerializeField] private float amplitudeY = 1f; // 垂直波幅
    [SerializeField] private float frequencyX = 1f; // 水平波速
    [SerializeField] private float frequencyY = 0.5f; // 垂直波速
    [SerializeField] private float speed = 2f; // 总体移动速度
    private Vector2 startPosition;// 初始位置
    private float timeOffset;// 时间偏移量，确保不同对象的运动不同步
    private Rigidbody2D rb;// 刚体组件引用

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position; // 记录初始位置
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void FixedUpdate()
    {
        // 计算新的位置（使用 speed 控制整体运动速度）
        float xOffset = Mathf.Sin(Time.fixedTime * frequencyX * speed + timeOffset) * amplitudeX;
        float yOffset = Mathf.Cos(Time.fixedTime * frequencyY * speed + timeOffset) * amplitudeY;
        Vector2 offset = new Vector2(xOffset, yOffset);

        // 考虑父对象的位置
        Vector2 parentPosition = transform.parent != null ? (Vector2)transform.parent.position : Vector2.zero;
        Vector2 targetPosition = startPosition + offset + parentPosition;

        rb.MovePosition(targetPosition);
    }

    public void SetStartPosition(Vector2 position)
    {
        startPosition = position;
        rb.position = position;
    }
}