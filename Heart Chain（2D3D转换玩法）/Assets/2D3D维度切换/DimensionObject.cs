using UnityEngine;

public class DimensionObject : MonoBehaviour
{
    [Header("Colliders")]
    public Collider collider_2D;
    public Collider collider_3D;

    private void SwitchTo2D()
    {
        collider_2D.enabled = true;
        collider_3D.enabled = false;
    }
    private void SwitchTo3D()
    {
        collider_2D.enabled = false;
        collider_3D.enabled = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        // 2D 模式下，将玩家位置重置为3D模式下的位置
        var player = collision.gameObject.GetComponent<DimensionPlayerController>();
        // 确实是Player对象，并且是2D模式
        if (player && player.is2D)
        {
            var contact = collision.contacts[0];
            // 碰撞法线，指向远离被碰撞表面的方向
            // 由于AB来源反向，所以需要取反
            var hitNormal = -contact.normal;
            // 如果 hitNormal 的 Y 分量接近 1，或者与 Vector3.up 的点积接近 1
            // 则表示碰撞发生在物体的顶部表面
            if (Vector3.Dot(hitNormal, Vector3.up) > 0.9f)
            {
                var rb = player.GetComponent<Rigidbody>();//
                rb.position = new Vector3(player.transform.position.x,
                                        player.transform.position.y,
                                        transform.position.z);
            }
        }
    }
}
