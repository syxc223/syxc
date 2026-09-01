using UnityEngine;

public class ExitGameButton : MonoBehaviour
{
    /// <summary>
    /// 点击退出游戏按钮时调用
    /// </summary>
    public void OnExitButtonClick()
    {
        #if UNITY_EDITOR
        // 在 Unity 编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在构建的游戏中退出应用程序
        Application.Quit();
        #endif
    }
}