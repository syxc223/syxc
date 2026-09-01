using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField] private GameObject prefab;//要放置的对象引用
    [SerializeField] private int prefabNumber = 5;//要放置的对象数量（基础数量，会根据层数调整）
    [SerializeField] private Walker walker;//行走组件引用
    [SerializeField] private Tilemap groundTilemap;//地面瓦片地图引用
    [SerializeField] private bool useLevelScaling = true; // 是否根据层数调整数量

    private List<GameObject> placedObjects = new List<GameObject>();//已放置对象列表

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //验证检查关卡生成是否完成（只要任意地面瓦片存在即可）
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

    //清除已放置的对象
    public void ClearPlacedObjects()
    {
        foreach (GameObject obj in placedObjects)
        {
           if(obj != null)
           {
               Destroy(obj);
           }
        }
        placedObjects.Clear();
    }

    //获取地面瓦片地图中所有地板瓦片位置
    private List<Vector3> GetFloorPositions()
    {
        List<Vector3> floorPositions = new List<Vector3>();
        
        if (walker == null)
        {
            Debug.LogWarning("ObjectPlacer: walker 未设置！");
            return floorPositions;
        }
        
        // 使用 Walker 的公共方法获取所有可行走的位置
        // 这个方法基于 visitedTiles 数组，确保只返回实际被 Walker 访问过的位置
        floorPositions = walker.GetWalkableFloorPositions();
        
        if (floorPositions.Count == 0)
        {
            Debug.LogWarning("ObjectPlacer: Walker 没有返回任何可行走位置！可能地图还未生成完成。");
                }
        
        return floorPositions;
    }

    /// <summary>
    /// Walker 完成生成后调用，放置物体
    /// </summary>
    public void OnLevelGenerationComplete()
    {
        if (!IsGenerationComplete()) return;

        ClearPlacedObjects();
        PlaceObjects();
    }

    //根据地板位置随机放置 prefab
    private void PlaceObjects()
    {
        if (prefab == null || walker == null || groundTilemap == null) return;

        var floorPositions = GetFloorPositions();
        if (floorPositions.Count == 0) return;

        // 根据层数计算实际要放置的数量
        int actualCount = prefabNumber;
        if (useLevelScaling)
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager != null)
            {
                actualCount = levelManager.GetEnemyCountForCurrentLevel();
                Debug.Log($"ObjectPlacer: 当前层数 {levelManager.GetCurrentLevel()}，放置 {actualCount} 个敌人");
            }
        }

        int count = Mathf.Clamp(actualCount, 0, floorPositions.Count);
        for (int i = 0; i < count; i++)
        {
            if (floorPositions.Count == 0)
            {
                Debug.LogWarning("ObjectPlacer: 没有可用的地板位置！");
                break;
            }
            
            Vector3 pos = floorPositions[Random.Range(0, floorPositions.Count)];
            
            // 二次验证：确保位置在可行走范围内（双重保险，理论上不应该失败）
            if (!IsPositionInWalkableBounds(pos))
            {
                Debug.LogWarning($"ObjectPlacer: 警告！位置 {pos} 不在可行走范围内，这可能是一个bug。已跳过该位置。");
                // 由于GetWalkableFloorPositions已经过滤了位置，这种情况理论上不应该发生
                // 如果发生了，记录警告并跳过，继续生成下一个
                continue;
            }
            
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
            placedObjects.Add(obj);
            
            // 如果是敌人，设置血量
            if (useLevelScaling)
            {
                SetEnemyHealth(obj);
            }
        }
    }

    /// <summary>
    /// 验证位置是否在可行走范围内
    /// </summary>
    private bool IsPositionInWalkableBounds(Vector3 worldPos)
    {
        if (walker == null || groundTilemap == null)
        {
            return false;
        }
        
        // 将世界坐标转换为网格坐标
        Vector3Int gridPos = groundTilemap.WorldToCell(worldPos);
        
        // 使用walker的IsWithinWalkableBounds方法验证
        Vector2Int pos2D = new Vector2Int(gridPos.x, gridPos.y);
        return walker.IsWithinWalkableBounds(pos2D);
    }

    /// <summary>
    /// 根据层数设置敌人血量
    /// </summary>
    private void SetEnemyHealth(GameObject enemy)
    {
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager == null) return;

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            float health = levelManager.GetEnemyHealthForCurrentLevel();
            // 使用反射或添加公共方法来设置血量
            // 由于EnemyHealth的maxHealth是私有的，我们需要修改它
            SetEnemyMaxHealth(enemyHealth, health);
        }
    }

    /// <summary>
    /// 设置敌人最大血量
    /// </summary>
    private void SetEnemyMaxHealth(EnemyHealth enemyHealth, float health)
    {
        // 直接调用公共方法设置血量
        enemyHealth.InitializeHealth(health);
    }
}
