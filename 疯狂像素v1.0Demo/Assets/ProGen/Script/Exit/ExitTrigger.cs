using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    private Walker walker;
    private bool hasTriggered = false; // 防止重复触发

    public void SetWalker(Walker walkerReference)
    {
        walker = walkerReference;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ExitTrigger] 检测到碰撞 - {other.gameObject.name}, Tag: {other.tag}, HasTriggered: {hasTriggered}");
        
        if(other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true; // 标记已触发，防止重复
            
            // 增加传送门进入次数
            ExitCounter.IncrementExitCount();
            
            Debug.Log($"[ExitTrigger] 玩家进入出口！当前传送门计数: {ExitCounter.ExitCount}");
            
            // 显示升级界面
            LevelUpUI levelUpUI = LevelUpUI.Instance;
            if (levelUpUI != null)
            {
                // 显示升级界面，选择完成后刷新关卡
                levelUpUI.ShowLevelUpPanel(OnLevelUpComplete);
            }
            else
            {
                Debug.LogWarning("[ExitTrigger] 未找到 LevelUpUI 实例！直接刷新关卡。");
                // 如果没有升级UI，直接刷新关卡
                OnLevelUpComplete();
            }
        }
        else if (other.CompareTag("Player") && hasTriggered)
        {
            Debug.Log("[ExitTrigger] 传送门已触发过，忽略本次碰撞");
        }
    }

    /// <summary>
    /// 升级完成后的回调，用于刷新关卡
    /// </summary>
    private void OnLevelUpComplete()
    {
        // 清理所有掉落物
        DropItemCleaner.CleanAllDropItems();
        
        // 进入下一层
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            levelManager.AdvanceToNextLevel();
            Debug.Log($"ExitTrigger: 进入第 {levelManager.GetCurrentLevel()} 层！");
        }
        
        //检查walker是否不等于null
        if(walker != null)
        {
            walker.TriggerRestart();
            Debug.Log("ExitTrigger: 关卡刷新已触发！");
        }
        else
        {
            Debug.LogWarning("ExitTrigger: walker 引用为空，无法触发关卡刷新！请检查 ExitHandler 是否正确设置了 walker 引用。");
        }
        
        // 重置触发标记（关卡刷新后ExitTrigger会重新创建，但保险起见）
        hasTriggered = false;
    }

    private void OnEnable()
    {
        // 当触发器重新启用时重置标记
        hasTriggered = false;
    }
}
