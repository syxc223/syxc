using UnityEngine;

public class AmmoCollected : MonoBehaviour
{
    [SerializeField] private int ammoAmount = 20;

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            // 尝试在玩家或其父物体上获取 GunShooting 组件来补充弹药
            var gun = other.GetComponentInParent<GunShooting>();
            if (gun != null)
            {
                gun.AddAmmo(ammoAmount);
            }
            Destroy(gameObject);
        }
    }

}
