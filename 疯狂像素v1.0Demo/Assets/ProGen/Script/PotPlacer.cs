using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 罐子放置器
/// 在地图上随机放置罐子（类似ObjectPlacer）
/// </summary>
public class PotPlacer : MonoBehaviour
{
    [SerializeField] private GameObject potPrefab; // 罐子预制体
    [SerializeField] private int potNumber = 10; // 要放置的罐子数量（基础数量，会根据层数调整）
    [SerializeField] private Walker walker; // 行走组件引用
    [SerializeField] private Tilemap groundTilemap; // 地面瓦片地图引用
    [SerializeField] private bool useLevelScaling = false; // 是否根据层数调整数量（罐子通常不需要）

    private List<GameObject> placedPots = new List<GameObject>(); // 已放置罐子列表

    /// <summary>
    /// 检查关卡生成是否完成
    /// </summary>
    private bool IsGenerationComplete()
    {
        if (walker == null || groundTilemap == null)
            return false;

        for (int x = walker.GetGridMin().x; x <= walker.GetGridMax().x; x++)
        {
            for (int y = walker.GetGridMin().y; y <= walker.GetGridMax().y; y++)
            {
                if (groundTilemap.HasTile(new Vector3Int(x, y, 0)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 清除已放置的罐子
    /// </summary>
    public void ClearPlacedPots()
    {
        foreach (GameObject pot in placedPots)
        {
            if (pot != null)
            {
                Destroy(pot);
            }
        }
        placedPots.Clear();
    }

    /// <summary>
    /// 获取地面瓦片地图中所有地板瓦片位置
    /// </summary>
    private List<Vector3> GetFloorPositions()
    {
        List<Vector3> floorPositions = new List<Vector3>();
        
        if (walker == null)
        {
            Debug.LogWarning("PotPlacer: walker 未设置！");
            return floorPositions;
        }
        
        // 使用 Walker 的公共方法获取所有可行走的位置
        // 这个方法基于 visitedTiles 数组，确保只返回实际被 Walker 访问过的位置
        floorPositions = walker.GetWalkableFloorPositions();
        
        if (floorPositions.Count == 0)
        {
            Debug.LogWarning("PotPlacer: Walker 没有返回任何可行走位置！可能地图还未生成完成。");
        }
        
        return floorPositions;
    }

    /// <summary>
    /// Walker 完成生成后调用，放置罐子
    /// </summary>
    public void OnLevelGenerationComplete()
    {
        if (!IsGenerationComplete()) return;

        ClearPlacedPots();
        PlacePots();
    }

    /// <summary>
    /// 根据地板位置随机放置罐子
    /// </summary>
    private void PlacePots()
    {
        if (potPrefab == null || walker == null || groundTilemap == null)
        {
            Debug.LogWarning("PotPlacer: potPrefab、walker 或 groundTilemap 未设置！");
            return;
        }

        var floorPositions = GetFloorPositions();
        if (floorPositions.Count == 0)
        {
            Debug.LogWarning("PotPlacer: 没有可用的地板位置！");
            return;
        }

        // 根据层数计算实际要放置的数量（可选）
        int actualCount = potNumber;
        if (useLevelScaling)
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager != null)
            {
                // 罐子数量可以随着层数稍微增加
                actualCount = Mathf.RoundToInt(potNumber * (1 + (levelManager.GetCurrentLevel() - 1) * 0.1f));
            }
        }

        // 确保不超过可用位置数量
        int count = Mathf.Clamp(actualCount, 0, floorPositions.Count);
        
        Debug.Log($"PotPlacer: 准备放置 {count} 个罐子");

        // 随机选择位置并放置罐子
        List<Vector3> availablePositions = new List<Vector3>(floorPositions);
        for (int i = 0; i < count && availablePositions.Count > 0; i++)
        {
            // 随机选择一个位置
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector3 pos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex); // 移除已选择的位置，避免重复

            // 实例化罐子
            GameObject pot = Instantiate(potPrefab, pos, Quaternion.identity, transform);
            placedPots.Add(pot);
        }

        Debug.Log($"PotPlacer: 成功放置 {placedPots.Count} 个罐子");
    }
}

