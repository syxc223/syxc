using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManagerFacade : MonoBehaviour
{
    private ButtonMediator _mediator;

    public void Init(ButtonMediator mediator)
    {
        _mediator = mediator;
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowSetting()
    {
        if (_mediator != null)
        {
            Debug.Log("UIManagerFacade: ShowSetting() 被调用");
            _mediator.OnSetting();
        }
        else
        {
            Debug.LogError("UIManagerFacade: _mediator 为 null！请检查 BtnManagerControl 是否正确初始化。");
        }
    }

    public void ShowProducer()
    {
        if (_mediator != null) _mediator.OnProducer();
    }

    public void CloseSetting()
    {
        if (_mediator != null) _mediator.OnExitSetting();
    }

    public void CloseProducer()
    {
        if (_mediator != null) _mediator.OnExitProducer();
    }

    public void StartGame(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("UIManagerFacade: 场景名称为空，无法加载场景！");
            return;
        }
        
        Debug.Log($"UIManagerFacade: 开始加载场景: {sceneName}");
        
        // 直接使用 Unity 的 SceneManager 加载场景
        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UIManagerFacade: 加载场景失败！场景名称: {sceneName}, 错误: {e.Message}");
            Debug.LogError("提示: 请确保场景已添加到 Build Settings 中（File → Build Settings → Add Open Scenes）");
        }
    }
}
