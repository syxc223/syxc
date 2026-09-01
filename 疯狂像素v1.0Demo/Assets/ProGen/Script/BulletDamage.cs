using UnityEngine;
using System.Reflection;

/// <summary>
/// 子弹伤害组件
/// 附加到子弹预制体上，用于对敌人造成基于PlayerStats的攻击力伤害
/// </summary>
public class BulletDamage : MonoBehaviour
{
    [SerializeField] private float baseDamage = 1.0f; // 基础伤害（如果没有使用PlayerStats则使用这个）
    [SerializeField] private bool usePlayerStats = true; // 是否使用PlayerStats的攻击力

    /// <summary>
    /// 获取当前子弹的伤害值
    /// </summary>
    public float GetDamage()
    {
        if (usePlayerStats)
        {
            return PlayerStats.AttackDamage;
        }
        return baseDamage;
    }

    /// <summary>
    /// 对敌人造成伤害
    /// 注意：此方法会在 Bullet.cs 的 OnTriggerEnter2D 之后调用
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 调试信息：显示所有碰撞
        Debug.Log($"[BulletDamage] 检测到碰撞: {collision.gameObject.name}, Tag: {collision.tag}");
        
        // 检查是否击中敌人（直接使用tag属性比较，避免标签不存在时抛出异常）
        // 注意：如果标签未定义，collision.tag会返回"Untagged"，不会抛出异常
        bool isEnemy = collision.tag == "Enemy";
        bool isBoss = collision.tag == "Boss";
        
        Debug.Log($"[BulletDamage] 标签检查 - Enemy: {isEnemy}, Boss: {isBoss}");
        
        if (isEnemy || isBoss)
        {
            float damage = GetDamage();
            Debug.Log($"[BulletDamage] 子弹击中 {collision.gameObject.name}，Tag: {collision.tag}，伤害: {damage}");
            
            // 优先检查BossHealth（Boss）- 使用反射安全获取组件
            Component bossHealthComponent = collision.GetComponent("BossHealth");
            if (bossHealthComponent != null)
            {
                // 使用反射调用TakeDamage方法
                var takeDamageMethod = bossHealthComponent.GetType().GetMethod("TakeDamage");
                if (takeDamageMethod != null)
                {
                    takeDamageMethod.Invoke(bossHealthComponent, new object[] { damage });
                    Debug.Log($"[BulletDamage] 找到BossHealth组件，调用TakeDamage({damage})");
                }
                else
                {
                    Debug.LogError($"[BulletDamage] BossHealth组件没有TakeDamage方法！");
                }
            }
            else
            {
                // 尝试获取敌人的伤害处理组件
                EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    Debug.Log($"[BulletDamage] 找到EnemyHealth组件，调用TakeDamage({damage})");
                    enemyHealth.TakeDamage(damage);
                }
                else
                {
                    Debug.LogWarning($"[BulletDamage] {collision.gameObject.name} 没有EnemyHealth或BossHealth组件");
                    
                    // 如果没有EnemyHealth组件，尝试使用DamageManager（敌人被子弹击中时销毁）
                    DamageManager damageManager = collision.GetComponent<DamageManager>();
                    if (damageManager != null)
                    {
                        Debug.Log($"[BulletDamage] 找到DamageManager组件（敌人将直接销毁）");
                    }
                    else
                    {
                        Debug.LogWarning($"[BulletDamage] {collision.gameObject.name} 既没有EnemyHealth/BossHealth也没有DamageManager组件！");
                    }
                }
            }
            
            // 子弹击中敌人后销毁
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log($"[BulletDamage] 碰撞对象不是敌人或Boss，忽略");
        }
    }
}

