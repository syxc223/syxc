using UnityEngine;

/// <summary>
/// 掉落物清理器（静态工具类）
/// 用于清理场景中的所有掉落物
/// </summary>
public static class DropItemCleaner
{
    /// <summary>
    /// 清理场景中的所有掉落物
    /// 包括：金币、苹果、弹药等
    /// </summary>
    public static void CleanAllDropItems()
    {
        // 清理金币
        CoinPickup[] coins = Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int coinCount = 0;
        foreach (CoinPickup coin in coins)
        {
            if (coin != null && coin.gameObject != null)
            {
                Object.Destroy(coin.gameObject);
                coinCount++;
            }
        }
        
        // 清理苹果
        ApplePickup[] apples = Object.FindObjectsByType<ApplePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int appleCount = 0;
        foreach (ApplePickup apple in apples)
        {
            if (apple != null && apple.gameObject != null)
            {
                Object.Destroy(apple.gameObject);
                appleCount++;
            }
        }
        
        // 清理弹药
        AmmoCollected[] ammos = Object.FindObjectsByType<AmmoCollected>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int ammoCount = 0;
        foreach (AmmoCollected ammo in ammos)
        {
            if (ammo != null && ammo.gameObject != null)
            {
                Object.Destroy(ammo.gameObject);
                ammoCount++;
            }
        }
        
        int totalCount = coinCount + appleCount + ammoCount;
        Debug.Log($"[DropItemCleaner] 清理完成！共清理 {totalCount} 个掉落物（金币: {coinCount}, 苹果: {appleCount}, 弹药: {ammoCount}）");
    }
    
    /// <summary>
    /// 清理指定类型的掉落物
    /// </summary>
    /// <typeparam name="T">掉落物组件类型</typeparam>
    public static void CleanDropItems<T>() where T : MonoBehaviour
    {
        T[] items = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        
        foreach (T item in items)
        {
            if (item != null && item.gameObject != null)
            {
                Object.Destroy(item.gameObject);
                count++;
            }
        }
        
        Debug.Log($"[DropItemCleaner] 清理了 {count} 个 {typeof(T).Name} 类型的掉落物");
    }
}

