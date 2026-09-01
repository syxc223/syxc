using System.Collections.Generic;//系统集合命名空间
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class Walker : MonoBehaviour
{
    [Header("Tilemp References")]//地形图引用
    [SerializeField] private Tilemap groundTilemap;//地面地形图
    [SerializeField] private Tilemap backgroundTilemap;//背景地形图

    [Header("Tile References")]//地形引用
    [SerializeField] private TileBase floorTile;//地板地形
    [SerializeField] private TileBase backgroundTile;//背景地形

    [Header("Generation Parameters")]//通用参数
    [SerializeField] private int maxIterations = 100;//最大迭代次数
    [SerializeField] private int walkersCount = 1;//行走概率
    [SerializeField] [Range(0f, 1f)] private float directionChangeChance = 1.0f;//方向改变概率
    [SerializeField] private Vector2Int startPositionOffset =new Vector2Int(0,0);//起始位置
    [SerializeField] private int borderSize = 3;//边界大小
    [SerializeField] private Vector2Int customGridSize = new Vector2Int(20,20);//行走边界
    [SerializeField] private PlayerHandler playerHandler;//玩家处理组件引用
    [SerializeField] private ObjectPlacer objectPlacer;//对象放置组件引用
    [SerializeField] private ObjectPlacer enemyPlacer;//敌人放置组件引用
    [SerializeField] private PotPlacer potPlacer;//罐子放置组件引用
    [SerializeField] private ExitHandler exitHandler;//出口处理组件引用
    [SerializeField] private PaidExitHandler paidExitHandler;//付费传送门处理组件引用
    [SerializeField] private BossPlacer bossPlacer;//Boss放置组件引用


    private bool[,] visitedTiles;//已访问地形数组
    private Vector2Int gridSize;//网格大小
    private Vector2Int gridMin;//网格最小值
    private Vector2Int gridMax;//网格最大值
    private Vector2Int WalkerMin;//行走最小值(防止走出去)
    private Vector2Int WalkerMax;//行走最大值(防止走出去)
    private PlayerInput playerInput;//玩家输入组件引用

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CalculateGridBounds();//计算网格边界
        GenerateLevel();//生成关卡
        if(playerHandler!=null)//如果玩家处理组件不为空，则重新生成玩家
        {
            playerHandler.RegeneratePlayer();//重新生成玩家
        }
    }

    public void OnRestart(InputAction.CallbackContext context)//
    {
        if (context.performed)//如果输入动作执行
        {
                CalculateGridBounds();//计算网格边界
            GenerateLevel();//生成关卡
            ObjectToRegenerate();//重新生成所有对象
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
       //定义网格尺寸和边界
    private void CalculateGridBounds()
    {
        //确保网格尺寸足够大以容纳边界
        gridSize = new Vector2Int(
          Mathf.Max(2* borderSize + 1, customGridSize.x),
          Mathf.Max(2* borderSize + 1, customGridSize.y)
          );

          //坐标转换成网片地图坐标
          gridMin= new Vector2Int(
            -gridSize.x / 2,
            -gridSize.y / 2             
          );
            gridMax = new Vector2Int(
                gridMin.x + gridSize.x-1,gridMin.y + gridSize.y-1 );

                //通过缩小边界来定义行走边界
            WalkerMin = new Vector2Int(gridMin.x + borderSize, gridMin.y + borderSize);//行走最小值
            WalkerMax = new Vector2Int(gridMax.x - borderSize, gridMax.y - borderSize);//行走最大值

            //确保边界是有效的
            WalkerMin.x = Mathf.Min(WalkerMin.x, WalkerMax.x);
            WalkerMin.y = Mathf.Min(WalkerMin.y, WalkerMax.y);
            WalkerMax.x = Mathf.Max(WalkerMin.x, WalkerMax.x);
            WalkerMax.y = Mathf.Max(WalkerMin.y, WalkerMax.y);
    }

    private void GenerateLevel()
    {
        //清除地形图，重新开始
        groundTilemap.ClearAllTiles();
        backgroundTilemap.ClearAllTiles();

        //初始化已访问图块数组
        visitedTiles = new bool[gridSize.x, gridSize.y];

        //计算每个Wallker起始位置
        Vector2Int startposition = new Vector2Int(
            WalkerMin.x + (WalkerMax.x - WalkerMin.x) / 2 + startPositionOffset.x,
            WalkerMin.y + (WalkerMax.y - WalkerMin.y) / 2 + startPositionOffset.y
        );

        //确保起始位置在边界内
        startposition.x = Mathf.Clamp(startposition.x, WalkerMin.x, WalkerMax.x);
        startposition.y = Mathf.Clamp(startposition.y, WalkerMin.y, WalkerMax.y);

        //为每个Walker执行随机行走
        for (int i = 0; i < walkersCount; i++)
        {
            RandomWalker(startposition);
        }
        //根据已访问的图块填充地形图
        FillTilemaps();

        //触发放置物体功能
        if(objectPlacer != null)
        {
            objectPlacer.OnLevelGenerationComplete();
        }

        //触发放置罐子功能
        if(potPlacer != null)
        {
            potPlacer.OnLevelGenerationComplete();
        }

    }

         //根据已访问的图块填充地形图
    private void FillTilemaps()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3Int tilemapPosition = new Vector3Int(x+ gridMin.x, y + gridMin.y, 0);

                //如果该位置已被访问，则放置地板瓷砖，否则放置背景瓷砖
                if (visitedTiles[x, y])
                {
                    groundTilemap.SetTile(tilemapPosition, floorTile);
                }
                else
                {
                    backgroundTilemap.SetTile(tilemapPosition, backgroundTile);
                }
            }
        }
    }
    private void RandomWalker(Vector2Int startPosition)
    {
        Vector2Int currentPosition = startPosition;
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Right
            new Vector2Int(0, -1),  // Left
            new Vector2Int(1, 0),   // Up
            new Vector2Int(-1, 0)   // Down
        };
        Vector2Int currentDirection = directions[Random.Range(0, directions.Length)];//随机初始方向


        //将起始位置标记为已经访问

        if(IsWithinBounds(currentPosition))//检查是否在边界内
        {
          int arrayX = currentPosition.x - gridMin.x;//
          int arrayY = currentPosition.y - gridMin.y;
          visitedTiles[arrayX, arrayY] = true;
        }

        //执行行走直到达到最大迭代次数
        for (int i = 0; i < maxIterations; i++)
        {
            //以一定概率改变方向
            if (Random.value < directionChangeChance)
            {
                currentDirection = directions[Random.Range(0, directions.Length)];
            }

           currentPosition += currentDirection;

          currentPosition.x = Mathf.Clamp(currentPosition.x, WalkerMin.x, WalkerMax.x );
            currentPosition.y = Mathf.Clamp(currentPosition.y, WalkerMin.y, WalkerMax.y );
    
                //标记当前位置为已访问
                if (IsWithinBounds(currentPosition))//检查是否在边界内
                {
                    int arrayX = currentPosition.x - gridMin.x;//转换为数组索引
                    int arrayY = currentPosition.y - gridMin.y;
                    if(!visitedTiles[arrayX, arrayY])
                    {
                        visitedTiles[arrayX, arrayY] = true;//标记为已访问
                    }
                }
        }
    }

    private bool IsWithinBounds(Vector2Int position)///检查位置是否在边界内
    {
        return position.x >= WalkerMin.x && position.x <= WalkerMax.x &&
               position.y >= WalkerMin.y && position.y <= WalkerMax.y;  // 检查位置是否在边界内
    }

    /// <summary>
    /// 检查位置是否在可行走范围内（公共方法，供外部调用）
    /// </summary>
    public bool IsWithinWalkableBounds(Vector2Int position)
    {
        return position.x >= WalkerMin.x && position.x <= WalkerMax.x &&
               position.y >= WalkerMin.y && position.y <= WalkerMax.y;
    }

    public Vector2Int GetGridMin() => gridMin;
    public Vector2Int GetGridMax() => gridMax;
    
    /// <summary>
    /// 获取所有可行走的地板位置（世界坐标）
    /// 这个方法返回所有被 Walker 访问过的位置，确保对象只生成在可行走区域
    /// 只返回在 WalkerMin 和 WalkerMax 边界内的位置，防止对象生成在地图外部
    /// </summary>
    public List<Vector3> GetWalkableFloorPositions()
    {
        List<Vector3> floorPositions = new List<Vector3>();
        
        if (groundTilemap == null)
        {
            Debug.LogWarning("Walker: groundTilemap 未设置，无法获取可行走位置！");
            return floorPositions;
        }
        
        // 遍历整个网格，只选择被访问过的位置（这些是地板）
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                // 只选择被访问过的位置
                if (visitedTiles != null && visitedTiles[x, y])
                {
                    // 将数组索引转换回网格坐标
                    Vector3Int tilePos = new Vector3Int(
                        x + gridMin.x,
                        y + gridMin.y,
                        0
                    );
                    
                    // 关键修复：只返回在可行走边界内的位置，防止生成在地图外部
                    if (tilePos.x < WalkerMin.x || tilePos.x > WalkerMax.x ||
                        tilePos.y < WalkerMin.y || tilePos.y > WalkerMax.y)
                    {
                        continue; // 跳过边界外的位置
                    }
                    
                    // 验证该位置确实有地板瓦片（双重验证）
                    if (groundTilemap.HasTile(tilePos))
                    {
                        // 将网格坐标转换为世界坐标
                        Vector3 worldPos = groundTilemap.CellToWorld(tilePos);
                        
                        // 添加瓦片锚点偏移（确保位置在瓦片中心）
                        worldPos += groundTilemap.tileAnchor;
                        
                        floorPositions.Add(worldPos);
                    }
                }
            }
        }
        
        Debug.Log($"Walker: 找到 {floorPositions.Count} 个可行走的地板位置（基于 visitedTiles，且在可行走边界内）");
        return floorPositions;
    }

    /// <summary>
    /// 统一重新生成所有对象（玩家、出口、物体、敌人等）
    /// </summary>
    private void ObjectToRegenerate()
    {
        if (playerHandler != null)
        {
            playerHandler.RegeneratePlayer();
        }

        if (exitHandler != null)
        {
            exitHandler.RegenerateExit();
        }

        if (objectPlacer != null)
        {
            objectPlacer.OnLevelGenerationComplete();
        }

        if (enemyPlacer != null)
        {
            enemyPlacer.OnLevelGenerationComplete();
        }

        if (potPlacer != null)
        {
            potPlacer.OnLevelGenerationComplete();
        }

        if (paidExitHandler != null)
        {
            paidExitHandler.RegeneratePaidExit();
        }

        if (bossPlacer != null)
        {
            bossPlacer.RegenerateBoss();
        }
    }

    //当玩家碰到退出触发器时，允许触发关卡重新生成
    public void TriggerRestart()
    {
        CalculateGridBounds();
        GenerateLevel();
        ObjectToRegenerate();
    }
    
}
