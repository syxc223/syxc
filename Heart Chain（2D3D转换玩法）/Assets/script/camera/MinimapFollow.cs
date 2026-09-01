using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target;
    public float height = 30f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = target.position;
        pos.y = height;
        transform.position = pos;

        // 始终朝下看
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}