using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 在当前场景“播放完”后自动跳转到下一个场景。
/// 默认行为：场景加载完成后等待一段时间，然后按 Build Settings 里的顺序加载下一个场景。
/// </summary>
public class AutoLoadNextScene : MonoBehaviour
{
    [Header("时间设置")]
    [Tooltip("从场景开始运行到跳转下一场景的等待时间（秒）。")]
    public float delayBeforeLoad = 3f;

    [Header("场景设置")]
    [Tooltip("是否按 Build Settings 的顺序自动切到下一个场景。"+
             "若关闭此选项，则使用 explicitSceneName 指定场景名。")]
    public bool useNextBuildIndex = true;

    [Tooltip("当 useNextBuildIndex 为 false 时，使用这个场景名进行跳转。")]
    public string explicitSceneName;

    void Start()
    {
        StartCoroutine(LoadNextSceneRoutine());
    }

    IEnumerator LoadNextSceneRoutine()
    {
        if (delayBeforeLoad > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLoad);
        }

        if (useNextBuildIndex)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("AutoLoadNextScene: 已经是最后一个 Build 场景，无法再跳转。");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(explicitSceneName))
            {
                SceneManager.LoadScene(explicitSceneName);
            }
            else
            {
                Debug.LogWarning("AutoLoadNextScene: explicitSceneName 为空，无法跳转。");
            }
        }
    }
}

