using UnityEngine;

public abstract class ShootingPattern : MonoBehaviour
{
   public abstract void Fire(GameObject bulletPrefab, Transform muzzle, float bulletSpeed);
}
