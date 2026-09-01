using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏结束界面控制器
/// 当玩家死亡时显示，提供重新开始和返回标题选项
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject gameOverPanel; // 游戏结束面板（父对象）
    [SerializeField] private Button restartButton; // 重新开始按钮
    [SerializeField] private Button returnToTitleButton; // 返回标题按钮

    [Header("场景设置")]
    [SerializeField] private int titleSceneIndex = 0; // 标题场景索引（Build Settings中的场景索引）

    private static GameOverUI instance; // 单例实例

    public static GameOverUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameOverUI>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 确保只有一个实例
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 初始化时隐藏面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 绑定按钮事件
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClick);
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(OnReturnToTitleButtonClick);
        }
    }

    private void OnDestroy()
    {
        // 取消事件绑定，防止内存泄漏
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClick);
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.RemoveListener(OnReturnToTitleButtonClick);
        }
    }

    /// <summary>
    /// 显示游戏结束界面
    /// </summary>
    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // 暂停游戏（可选）
            Time.timeScale = 0f;
        }
        else
        {
            Debug.LogWarning("GameOverUI: gameOverPanel 未设置！");
        }
    }

    /// <summary>
    /// 隐藏游戏结束界面
    /// </summary>
    public void HideGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Time.timeScale = 1f; // 恢复游戏时间
        }
    }

    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    private void OnRestartButtonClick()
    {
        // 恢复游戏时间
        Time.timeScale = 1f;
        
        // 重置玩家生命值
        PlayerHealth.Health = 5.0f;
        
        // 重置金币（可选：如果需要保留金币可以注释掉）
        // CoinSystem.ResetCoins();
        
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// 返回标题按钮点击事件
    /// </summary>
    private void OnReturnToTitleButtonClick()
    {
        // 恢复游戏时间
        Time.timeScale = 1f;
        
        // 重置玩家生命值
        PlayerHealth.Health = 5.0f;
        
        // 加载标题场景（场景索引0）
        int sceneIndex = titleSceneIndex;
        
        // 验证场景索引是否有效
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"GameOverUI: 场景索引 {sceneIndex} 无效，使用默认索引 0");
            sceneIndex = 0;
        }
        
        SceneManager.LoadScene(sceneIndex);
    }
}

