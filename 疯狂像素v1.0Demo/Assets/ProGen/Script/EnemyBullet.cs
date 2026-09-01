using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;//子弹存活时间

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

  void OnTriggerEnter2D(Collider2D collision)
    {
        //检测子弹与玩家的碰撞
        if (collision.CompareTag("Player"))
        {
            PlayerHealth.Health-= 0.25f;
        }
        if(collision.CompareTag("Solid"))
        {
            Destroy(gameObject);
        }
    }
}
