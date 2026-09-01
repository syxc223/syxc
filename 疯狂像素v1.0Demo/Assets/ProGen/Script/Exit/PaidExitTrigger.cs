using UnityEngine;

/// <summary>
/// 付费传送门触发器
/// 玩家进入时弹出支付确认UI
/// </summary>
public class PaidExitTrigger : MonoBehaviour
{
    [SerializeField] private int requiredCoins = 10; // 需要的金币数量
    private bool hasTriggered = false; // 防止重复触发

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PaidExitTrigger] 检测到碰撞 - {other.gameObject.name}, Tag: {other.tag}, HasTriggered: {hasTriggered}");

        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true; // 标记已触发，防止重复

            Debug.Log($"[PaidExitTrigger] 玩家进入付费传送门！需要金币: {requiredCoins}");

            // 显示支付确认界面
            PaidExitUI paidExitUI = PaidExitUI.Instance;
            if (paidExitUI != null)
            {
                paidExitUI.ShowPaymentPanel(requiredCoins, OnPaymentComplete, OnPaymentCancel);
            }
            else
            {
                Debug.LogWarning("[PaidExitTrigger] 未找到 PaidExitUI 实例！");
                hasTriggered = false; // 重置标记，允许重试
            }
        }
        else if (other.CompareTag("Player") && hasTriggered)
        {
            Debug.Log("[PaidExitTrigger] 付费传送门已触发过，忽略本次碰撞");
        }
    }

    /// <summary>
    /// 支付完成回调
    /// </summary>
    private void OnPaymentComplete()
    {
        Debug.Log("PaidExitTrigger: 支付完成，进入下一个场景");
        
        // 清理所有掉落物
        DropItemCleaner.CleanAllDropItems();
        
        // 进入下一个场景
        LoadNextScene();
    }

    /// <summary>
    /// 支付取消回调
    /// </summary>
    private void OnPaymentCancel()
    {
        Debug.Log("PaidExitTrigger: 支付取消");
        hasTriggered = false; // 重置标记，允许再次触发
    }

    /// <summary>
    /// 加载下一个场景
    /// </summary>
    private void LoadNextScene()
    {
        // 恢复游戏时间（确保场景切换时时间正常）
        Time.timeScale = 1f;
        
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;

        if (nextSceneIndex < sceneCount)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("PaidExitTrigger: 没有下一个场景！这是最后一个场景。");
            
            // 如果是最后一个场景，设置对话标志
            DialogueSystem.SetLastSceneDialogueFlag();
            
            // 重新加载当前场景（最后一个场景），这样PlayerController会在Start时检查标志并播放对话
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
        }
    }

    private void OnEnable()
    {
        // 当触发器重新启用时重置标记
        hasTriggered = false;
    }
}

