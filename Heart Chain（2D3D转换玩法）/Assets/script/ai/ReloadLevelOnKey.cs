using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 按下指定按键重新加载当前关卡（场景）。
/// 默认按键为 O。
/// </summary>
public class ReloadLevelOnKey : MonoBehaviour
{
    [Header("按键设置")]
    public KeyCode reloadKey = KeyCode.O;

    void Update()
    {
        if (Input.GetKeyDown(reloadKey))
        {
            ReloadCurrentScene();
        }
    }

    void ReloadCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}

