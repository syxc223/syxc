using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private string gameSceneName = "Game"; // 游戏场景名称
    
    /// <summary>
    /// 点击开始游戏按钮时调用
    /// </summary>
    public void OnStartButtonClick()
    {
        // 重置游戏数据
        CoinSystem.ResetCoins();
        PlayerStats.ResetStats();
        PlayerHealth.InitializeHealth();
        ExitCounter.ResetCounter(); // 重置传送门计数器
        
        // 重置对话系统标志（允许新游戏开始时播放对话）
        DialogueSystem.ResetDialogueFlag();
        
        // 重置层数（如果有LevelManager）
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.ResetLevel();
        }
        
        // 加载游戏场景
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // 加载下一个场景
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.LogError("没有找到下一个场景！请检查 Build Settings。");
            }
        }
    }
}