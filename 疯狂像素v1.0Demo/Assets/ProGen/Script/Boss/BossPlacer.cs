using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

/// <summary>
/// Boss放置器
/// 只在第二个场景（场景索引1）生成Boss
/// </summary>
public class BossPlacer : MonoBehaviour
{
    [SerializeField] private GameObject bossPrefab; // Boss预制体
    [SerializeField] private Walker walker; // 行走组件引用
    [SerializeField] private Tilemap groundTilemap; // 地面瓦片地图引用
    [SerializeField] private int targetSceneIndex = 1; // 目标场景索引（第二个场景）
    
    private GameObject bossInstance; // Boss实例引用

    private void Start()
    {
        // 检查当前场景是否是目标场景
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentSceneIndex == targetSceneIndex)
        {
            StartCoroutine(PlaceBossAfterGeneration());
        }
        else
        {
            Debug.Log($"BossPlacer: 当前场景索引 {currentSceneIndex} 不是目标场景 {targetSceneIndex}，不生成Boss");
        }
    }

    /// <summary>
    /// 重新生成Boss（关卡刷新时调用）
    /// </summary>
    public void RegenerateBoss()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentSceneIndex == targetSceneIndex)
        {
            StartCoroutine(PlaceBossAfterGeneration());
        }
    }

    private IEnumerator PlaceBossAfterGeneration()
    {
        while (!IsGenerationComplete())
        {
            yield return new WaitForSeconds(0.1f);
        }
        PlaceBoss();
    }

    /// <summary>
    /// 检查关卡生成是否完成
    /// </summary>
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
    /// 获取地图中心位置（用于放置Boss）
    /// </summary>
    private Vector3? GetCenterFloorPosition()
    {
        if (walker == null || groundTilemap == null) return null;

        Vector2Int center = new Vector2Int(
            (walker.GetGridMin().x + walker.GetGridMax().x) / 2,
            (walker.GetGridMin().y + walker.GetGridMax().y) / 2
        );

        // 检查中心位置是否有地板
        if (groundTilemap.HasTile(new Vector3Int(center.x, center.y, 0)))
        {
            Vector3 worldPos = groundTilemap.CellToWorld(new Vector3Int(center.x, center.y, 0)) + groundTilemap.tileAnchor;
            return worldPos;
        }

        // 如果中心没有地板，找最近的地板位置
        float minDistance = float.MaxValue;
        Vector3? nearestPos = null;

        for (int x = walker.GetGridMin().x; x <= walker.GetGridMax().x; x++)
        {
            for (int y = walker.GetGridMin().y; y <= walker.GetGridMax().y; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                if (groundTilemap.HasTile(tilePos))
                {
                    Vector3 worldPos = groundTilemap.CellToWorld(tilePos) + groundTilemap.tileAnchor;
                    float distance = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(x, y));
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestPos = worldPos;
                    }
                }
            }
        }

        return nearestPos;
    }

    /// <summary>
    /// 放置Boss
    /// </summary>
    private void PlaceBoss()
    {
        if (bossPrefab == null || groundTilemap == null)
        {
            Debug.LogWarning("BossPlacer: bossPrefab 或 groundTilemap 未设置，无法放置Boss。");
            return;
        }

        // 销毁旧的Boss实例
        if (bossInstance != null)
        {
            Destroy(bossInstance);
            bossInstance = null;
        }

        // 获取中心位置
        Vector3? centerPos = GetCenterFloorPosition();
        if (centerPos.HasValue)
        {
            bossInstance = Instantiate(bossPrefab, centerPos.Value, Quaternion.identity);
            
            // 设置Boss血量50
            BossHealth bossHealth = bossInstance.GetComponent<BossHealth>();
            if (bossHealth != null)
            {
                bossHealth.InitializeHealth(50f);
            }

            Debug.Log($"BossPlacer: Boss已放置在场景中心，位置: {centerPos.Value}");
        }
        else
        {
            Debug.LogWarning("BossPlacer: 未找到合适的位置放置Boss！");
        }
    }
}

