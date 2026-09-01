using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// 付费传送门处理器
/// 在地图最下方生成付费传送门（当玩家进入普通传送门3次后）
/// </summary>
public class PaidExitHandler : MonoBehaviour
{
    [SerializeField] private Walker walker; // 行走组件引用
    [SerializeField] private Tilemap groundTilemap; // 地面瓦片地图引用
    [SerializeField] private GameObject paidExitPrefab; // 付费传送门预制体引用
    [SerializeField] private PlayerHandler playerHandler; // 玩家处理组件引用
    
    private GameObject paidExitInstance; // 付费传送门实例引用
    private bool hasSpawned = false; // 是否已生成

    private void Start()
    {
        // 重置生成标记（场景切换时重新检查）
        hasSpawned = false;
        
        // 检查是否应该生成付费传送门
        CheckAndSpawnPaidExit();
        
        Debug.Log($"[PaidExitHandler] Start - ExitCount: {ExitCounter.ExitCount}, IsUnlocked: {ExitCounter.IsPaidExitUnlocked}, HasSpawned: {hasSpawned}");
    }

    /// <summary>
    /// 重新生成付费传送门（关卡刷新时调用）
    /// </summary>
    public void RegeneratePaidExit()
    {
        hasSpawned = false; // 重置标记，允许重新生成
        StartCoroutine(PlacePaidExitAfterRegeneration());
    }

    /// <summary>
    /// 检查并生成付费传送门
    /// </summary>
    private void CheckAndSpawnPaidExit()
    {
        if (ExitCounter.IsPaidExitUnlocked && !hasSpawned)
        {
            StartCoroutine(PlacePaidExitAfterRegeneration());
        }
    }

    private IEnumerator PlacePaidExitAfterRegeneration()
    {
        while (!IsGenerationComplete() || playerHandler == null || playerHandler.GetPlayerInstance() == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (ExitCounter.IsPaidExitUnlocked && !hasSpawned)
        {
            PlacePaidExit();
            hasSpawned = true;
        }
    }

    private bool IsGenerationComplete()
    {
        if (walker == null || groundTilemap == null) return false;

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
    /// 获取地图最下方的地板位置
    /// </summary>
    private Vector2Int? GetBottommostFloorPosition()
    {
        Vector2Int? bottommost = null;
        int minY = int.MaxValue;

        if (walker == null || groundTilemap == null)
        {
            Debug.LogWarning("PaidExitHandler: walker 或 groundTilemap 未设置！");
            return null;
        }

        // 使用 Walker 的公共方法获取所有可行走的位置（基于 visitedTiles）
        List<Vector3> walkablePositions = walker.GetWalkableFloorPositions();
        
        if (walkablePositions.Count == 0)
        {
            Debug.LogWarning("PaidExitHandler: 没有找到可行走位置！");
            return null;
        }

        // 从可行走位置中找到Y坐标最小的位置（最下方）
        foreach (Vector3 worldPos in walkablePositions)
        {
            // 将世界坐标转换为网格坐标
            Vector3Int tilePos = groundTilemap.WorldToCell(worldPos);
            
            // 找到Y坐标最小的位置（最下方）
            if (tilePos.y < minY)
            {
                minY = tilePos.y;
                bottommost = new Vector2Int(tilePos.x, tilePos.y);
            }
        }

        // 如果有多个相同Y坐标的位置，选择中间的那个
        if (bottommost.HasValue)
        {
            int targetY = bottommost.Value.y;
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int count = 0;

            // 找到最下方所有地板瓦片的X范围（从可行走位置中）
            foreach (Vector3 worldPos in walkablePositions)
            {
                Vector3Int tilePos = groundTilemap.WorldToCell(worldPos);
                if (tilePos.y == targetY)
                {
                    if (tilePos.x < minX) minX = tilePos.x;
                    if (tilePos.x > maxX) maxX = tilePos.x;
                    count++;
                }
            }

            // 选择中间位置
            if (count > 0)
            {
                int centerX = (minX + maxX) / 2;
                bottommost = new Vector2Int(centerX, targetY);
            }
        }

        return bottommost;
    }

    private void PlacePaidExit()
    {
        if (paidExitPrefab == null || groundTilemap == null)
        {
            Debug.LogWarning("PaidExitHandler: paidExitPrefab 或 groundTilemap 未设置，无法放置付费传送门。");
            return;
        }

        // 销毁旧的付费传送门实例
        if (paidExitInstance != null)
        {
            Destroy(paidExitInstance);
            paidExitInstance = null;
        }

        // 找到地图最下方的地板位置
        Vector2Int? bottommostPos = GetBottommostFloorPosition();
        if (bottommostPos.HasValue)
        {
            // 将网格坐标转换为世界坐标（中心）
            Vector3 worldPos = groundTilemap.CellToWorld(new Vector3Int(bottommostPos.Value.x, bottommostPos.Value.y, 0)) + new Vector3(0.5f, 0.5f, 0f);
            paidExitInstance = Instantiate(paidExitPrefab, worldPos, Quaternion.identity);

            Debug.Log($"PaidExitHandler: 付费传送门已放置在地图最下方，位置: {worldPos}");
        }
        else
        {
            Debug.LogWarning("PaidExitHandler: 未找到地图最下方的地板位置！");
        }
    }
}

