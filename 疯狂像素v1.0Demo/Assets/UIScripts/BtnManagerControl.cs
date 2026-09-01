using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BtnManagerControl : MonoBehaviour
{
    [Header("Buttons")]
    public Button BtnExit;
    public Button BtnSetting;
    public Button BtnProducer;
    public Button BtnExitSetting;
    public Button BtnExitProducer;
    public Button BtnStartGame; // 开始游戏按钮

    [Header("Panels")]
    public GameObject SetPanel;
    public GameObject ProducerPanel;

    [Header("Scene Settings")]
    [Tooltip("要加载的游戏场景名称（例如：Game）")]
    public string gameSceneName = "Game"; // 默认场景名称

    // runtime components
    private ButtonMediator _mediator;
    private UIManagerFacade _facade;

    void Awake()
    {
        // Add mediator and facade components on this GameObject
        _mediator = gameObject.AddComponent<ButtonMediator>();
        _mediator.Init(BtnExit, BtnSetting, BtnProducer, BtnExitSetting, BtnExitProducer, SetPanel, ProducerPanel);

        _facade = gameObject.AddComponent<UIManagerFacade>();
        _facade.Init(_mediator);
    }

    void Start()
    {
        // 检查关键引用
        if (SetPanel == null)
        {
            Debug.LogError("BtnManagerControl: SetPanel 未分配！请在 Inspector 中分配 SetPanel GameObject。");
        }
        if (ProducerPanel == null)
        {
            Debug.LogWarning("BtnManagerControl: ProducerPanel 未分配！");
        }
        if (_facade == null)
        {
            Debug.LogError("BtnManagerControl: _facade 初始化失败！");
            return;
        }
        
        // 先清除所有旧的监听器，避免重复绑定
        // wire buttons to facade
        if (BtnExit != null)
        {
            BtnExit.onClick.RemoveAllListeners(); // 清除旧监听器
            BtnExit.onClick.AddListener(() => _facade.ExitGame());
            Debug.Log("BtnManagerControl: BtnExit 事件已绑定（已清除旧监听器）");
        }
        else
        {
            Debug.LogWarning("BtnManagerControl: BtnExit 未分配！");
        }
        
        if (BtnSetting != null)
        {
            BtnSetting.onClick.RemoveAllListeners(); // 清除旧监听器
            BtnSetting.onClick.AddListener(() => 
            {
                Debug.Log("BtnManagerControl: BtnSetting 被点击");
                _facade.ShowSetting();
            });
            Debug.Log("BtnManagerControl: BtnSetting 事件已绑定（已清除旧监听器）");
        }
        else
        {
            Debug.LogError("BtnManagerControl: BtnSetting 未分配！请在 Inspector 中分配 BtnSetting 按钮。");
        }
        
        if (BtnProducer != null)
        {
            BtnProducer.onClick.RemoveAllListeners(); // 清除旧监听器
            BtnProducer.onClick.AddListener(() => _facade.ShowProducer());
            Debug.Log("BtnManagerControl: BtnProducer 事件已绑定（已清除旧监听器）");
        }
        else
        {
            Debug.LogWarning("BtnManagerControl: BtnProducer 未分配！");
        }
        
        if (BtnExitSetting != null)
        {
            BtnExitSetting.onClick.RemoveAllListeners(); // 清除旧监听器
            BtnExitSetting.onClick.AddListener(() => _facade.CloseSetting());
            Debug.Log("BtnManagerControl: BtnExitSetting 事件已绑定（已清除旧监听器）");
        }
        else
        {
            Debug.LogWarning("BtnManagerControl: BtnExitSetting 未分配！");
        }
        
        if (BtnExitProducer != null)
        {
            BtnExitProducer.onClick.RemoveAllListeners(); // 清除旧监听器
            BtnExitProducer.onClick.AddListener(() => _facade.CloseProducer());
            Debug.Log("BtnManagerControl: BtnExitProducer 事件已绑定（已清除旧监听器）");
        }
        else
        {
            Debug.LogWarning("BtnManagerControl: BtnExitProducer 未分配！");
        }
        
        if (BtnStartGame != null)
        {
            BtnStartGame.onClick.RemoveAllListeners(); // 清除旧监听器
            BtnStartGame.onClick.AddListener(() => 
            {
                Debug.Log($"BtnManagerControl: BtnStartGame 被点击，准备加载场景: {gameSceneName}");
                _facade.StartGame(gameSceneName);
            });
            Debug.Log("BtnManagerControl: BtnStartGame 事件已绑定（已清除旧监听器）");
        }
        else
        {
            Debug.LogWarning("BtnManagerControl: BtnStartGame 未分配！请在 Inspector 中分配开始游戏按钮。");
        }
    }
}
