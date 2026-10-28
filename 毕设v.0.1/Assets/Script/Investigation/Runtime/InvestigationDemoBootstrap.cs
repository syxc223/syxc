using UnityEngine;

/// <summary>
/// 游戏启动引导：确保核心服务存在（解谜进度、对话、调查、网络、调查UI）。
/// </summary>
public class InvestigationDemoBootstrap : MonoBehaviour
{
    private void Awake()
    {
        EnsureService<PuzzleProgressService>();
        EnsureService<DialogueService>();
        EnsureService<InvestigationService>();
        EnsureObject<NetworkBootstrap>("NetworkBootstrap");

        if (FindObjectOfType<InvestigationUISetup>() == null)
        {
            var uiGo = new GameObject("InvestigationUISetup");
            uiGo.AddComponent<InvestigationUISetup>();
        }
    }

    private static void EnsureService<T>() where T : Component
    {
        if (FindObjectOfType<T>() != null)
            return;
        var go = new GameObject(typeof(T).Name);
        go.AddComponent<T>();
        DontDestroyOnLoad(go);
    }

    private static void EnsureObject<T>(string objectName) where T : Component
    {
        if (FindObjectOfType<T>() != null)
            return;
        var go = new GameObject(objectName);
        go.AddComponent<T>();
        DontDestroyOnLoad(go);
    }
}
