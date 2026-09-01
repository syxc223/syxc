using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PacmanPlayerController : MonoBehaviour
{
    [Header("Move Settings")]
    public float moveForce = 10f;      // 力的大小，控制加速度
    public float maxSpeed = 6f;        // 最大线速度

    private Rigidbody rb;
    private Vector3 inputDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ; // 如果你希望只是“滚”而不乱翻，可以按需要调整/去掉
    }

    void Update()
    {
        // 只在 XZ 平面读取输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(h, 0f, v).normalized;
    }

    void FixedUpdate()
    {
        // 物理加力推动小球滚动
        if (inputDir.sqrMagnitude > 0.01f)
        {
            Vector3 force = inputDir * moveForce;
            rb.AddForce(force, ForceMode.Acceleration);
        }

        // 限制最大速度，防止越来越快
        Vector3 vel = rb.velocity;
        Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
        if (horizVel.magnitude > maxSpeed)
        {
            horizVel = horizVel.normalized * maxSpeed;
            rb.velocity = new Vector3(horizVel.x, vel.y, horizVel.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Pellet pellet = other.GetComponent<Pellet>();
        if (pellet != null)
        {
            pellet.Collect();
        }
    }
}