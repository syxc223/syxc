using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在传送门预制体上：玩家进入触发器后切换到指定场景。
/// </summary>
public class PortalSceneLoader : MonoBehaviour
{
    [Header("目标场景名称")]
    public string targetSceneName;

    private void OnTriggerEnter(Collider other)
    {
        // 只响应吃豆人玩家
        PacmanPlayerController player = other.GetComponent<PacmanPlayerController>();
        if (player == null) return;

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("PortalSceneLoader: targetSceneName 未设置。");
        }
    }
}

