using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerHandler : MonoBehaviour
{
    [SerializeField] private Walker walker;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private CinemachineCamera virtualCamera;
    private GameObject playerInstance;//玩家实例引用
    private Coroutine placePlayerCoroutine;//放置玩家协程引用
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        placePlayerCoroutine = StartCoroutine(PlacePlayerAfterGeneration());
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void RegeneratePlayer()
    {
        // 如果已有协程在运行，先停止它
        if (placePlayerCoroutine != null)
        {
            StopCoroutine(placePlayerCoroutine);
        }
        placePlayerCoroutine = StartCoroutine(PlacePlayerAfterGeneration());
    }

    private Vector2Int? GetLeftmostFloorPosition()
    {
        Vector2Int? leftmost = null;
        // 在网格边界扫描地面瓦片地图
        for (int x = walker.GetGridMin().x; x <= walker.GetGridMax().x; x++)
        {
            for (int y = walker.GetGridMin().y; y <= walker.GetGridMax().y; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                if (groundTilemap.HasTile(tilePosition))
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!leftmost.HasValue || position.x < leftmost.Value.x || (position.x == leftmost.Value.x && position.y < leftmost.Value.y))
                    {
                        leftmost = position;
                    }
                }
            }
        }
        return leftmost;
    }
    private bool IsGenerationComplete()
    {
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

    private IEnumerator PlacePlayerAfterGeneration()
    {
        //等待一帧以确保地形生成完成
        while (!IsGenerationComplete())
        {
            yield return new WaitForSeconds(0.1f);
        }
        yield return StartCoroutine(PlacePlayerCoroutine());
    }

    private IEnumerator PlacePlayerCoroutine()
    {
        // 如果玩家已存在，先销毁旧实例
        if (playerInstance != null)
        {
            PlayerInput oldPlayerInput = playerInstance.GetComponent<PlayerInput>();
            if (oldPlayerInput != null)
            {
                oldPlayerInput.enabled = false;
            }
            Destroy(playerInstance);
            playerInstance = null;

            // 等待一帧，确保控制方案被完全释放
            yield return null;
        }

        // 现在可以安全地创建新实例
        PlacePlayer();
    }
    private void PlacePlayer()
    {
        //获取最左侧的地面位置
        Vector2Int? leftmostPosition = GetLeftmostFloorPosition();
        if (leftmostPosition.HasValue)
        {
            //在该位置生成或移动玩家实例
            Vector3 worldPosition = groundTilemap.CellToWorld(new Vector3Int(leftmostPosition.Value.x, leftmostPosition.Value.y, 0));
            worldPosition += new Vector3(0.5f, 0.5f, 0f); //调整位置到瓦片中心

            // 实例化新的玩家预制件
            playerInstance = Instantiate(playerPrefab, worldPosition, Quaternion.identity);

            // 配置新实例的 PlayerInput 组件，避免控制方案冲突
            PlayerInput newPlayerInput = playerInstance.GetComponent<PlayerInput>();
            if (newPlayerInput != null)
            {
                // 禁用自动切换控制方案，避免警告
                newPlayerInput.neverAutoSwitchControlSchemes = true;
            }
        }
        // 设置虚拟摄像机跟随玩家
        if (virtualCamera != null && playerInstance != null)
        {
            virtualCamera.Follow = playerInstance.transform;//设置摄像机跟随玩家
        }

        // 初始化玩家生命值（使用PlayerStats的最大值）
        PlayerHealth.InitializeHealth();
    }
    public GameObject GetPlayerInstance()
    {
        return playerInstance;
    }
}
