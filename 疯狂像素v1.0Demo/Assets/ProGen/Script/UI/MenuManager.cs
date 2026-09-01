using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private string gameSceneName = "Game"; // 游戏场景名称，在 Inspector 中设置
    
    /// <summary>
    /// 开始游戏，加载下一个场景
    /// </summary>
    public void StartGame()
    {
        // 如果指定了场景名称，加载指定场景
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // 否则加载下一个场景（按 Build Settings 中的顺序）
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.LogWarning("没有下一个场景可以加载！");
            }
        }
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void ExitGame()
    {
        #if UNITY_EDITOR
        // 在编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在构建版本中退出应用程序
        Application.Quit();
        #endif
    }
}