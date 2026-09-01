using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class ExitHandler : MonoBehaviour
{
    [SerializeField] private Walker walker;//行走组件引用
    [SerializeField] private Tilemap groundTilemap;//地面瓦片地图引用
    [SerializeField] private GameObject exitPrefab;//出口预制体引用
    [SerializeField] private PlayerHandler playerHandler;//玩家处理组件引用
    private GameObject exitInstance;//出口实例引用

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
            StartCoroutine(PlaceExitAfterRegeneration());
    }


    public void RegenerateExit()
{
    StartCoroutine(PlaceExitAfterRegeneration());
}


private IEnumerator PlaceExitAfterRegeneration()
{
    while (!IsGenerationComplete() || playerHandler == null || playerHandler.GetPlayerInstance() == null)
    {
        yield return new WaitForSeconds(0.1f);
    }
    PlaceExit();
}
    private bool IsGenerationComplete()
    {
        // 只要发现任意地面瓦片即认为生成完成，避免卡死等待所有格子都有瓦片
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
    private Vector2Int? GetFarthestFloorPosition(Vector2 playerPos)
    {
        Vector2Int? farthest = null;
        float maxDistance = -1f;
        
        if (walker == null || groundTilemap == null)
        {
            Debug.LogWarning("ExitHandler: walker 或 groundTilemap 未设置！");
            return null;
        }
        
        // 使用 Walker 的公共方法获取所有可行走的位置（基于 visitedTiles）
        List<Vector3> walkablePositions = walker.GetWalkableFloorPositions();
        
        if (walkablePositions.Count == 0)
        {
            Debug.LogWarning("ExitHandler: 没有找到可行走位置！");
            return null;
        }
        
        // 从可行走位置中找到离玩家最远的位置
        foreach (Vector3 worldPos in walkablePositions)
        {
            // 将世界坐标转换为网格坐标
            Vector3Int tilePos = groundTilemap.WorldToCell(worldPos);
            Vector2Int pos = new Vector2Int(tilePos.x, tilePos.y);
            
            // 计算从玩家到该格子的距离
            float distance = Vector2.Distance(playerPos, new Vector2(pos.x, pos.y));
            
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = pos;
            }
        }
        
        return farthest;
    }

    private void PlaceExit()
    {
        if (exitPrefab == null || groundTilemap == null || playerHandler == null)
        {
            Debug.LogWarning("ExitHandler: exitPrefab / groundTilemap / playerHandler 未设置，无法放置出口。");
            return;
        }

        // 销毁旧的出口实例
        if (exitInstance != null)
        {
            Destroy(exitInstance);
            exitInstance = null;
        }

        // 获取玩家世界坐标 -> 网格坐标
        GameObject player = playerHandler.GetPlayerInstance();
        if (player == null)
        {
            Debug.LogWarning("ExitHandler: 未找到玩家实例。");
            return;
        }
        Vector3Int playerTilePos = groundTilemap.WorldToCell(player.transform.position);

        // 找到离玩家最远的地板瓦片位置
        Vector2Int? farthestPos = GetFarthestFloorPosition(new Vector2(playerTilePos.x, playerTilePos.y));
        if (farthestPos.HasValue)
        {
            // 将网格坐标转换为世界坐标（中心）
            Vector3 worldPos = groundTilemap.CellToWorld(new Vector3Int(farthestPos.Value.x, farthestPos.Value.y, 0)) + new Vector3(0.5f, 0.5f, 0f);
            exitInstance = Instantiate(exitPrefab, worldPos, Quaternion.identity);

            //给walker分配一个指向exitTrigger的引用
            ExitTrigger exitTrigger = exitInstance.GetComponent<ExitTrigger>();
            if (exitTrigger != null)
            {
                exitTrigger.SetWalker(walker);
            }
        }
    }
}