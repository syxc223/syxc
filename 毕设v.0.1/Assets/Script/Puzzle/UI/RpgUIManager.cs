using System;
using System.Collections.Generic;
using MFramework.Event;
using UnityEngine;

public class RpgUIManager : MonoBehaviour, IEventReceiver<PanelRequestEvent>
{
    [Header("根节点与特殊面板引用")]
    [SerializeField, Tooltip("探索模式下显示的总体 UI 根节点")]
    private GameObject fieldUIRoot;
    [SerializeField]private GameObject mainPanel;



    private readonly Dictionary<Type, PanelController> _panelControllerDict = new();
    private readonly List<PanelController> _allPanelList = new();

    private void Awake()
    {
        _panelControllerDict.Clear();
        _allPanelList.Clear();

        GetPanelsFromRoot(transform);
    }

    void OnEnable()
    {
        EventBus.Subscribe<PanelRequestEvent>(this);
    }
    void OnDisable()
    {
        EventBus.Unsubscribe<PanelRequestEvent>(this);
    }

    void Update()
    {
        var mode = GameModeManager.Instance.CurrentGameMode;
        var input = InputSystemController.Instance;

        if (mode is GameMode.Dialogue or GameMode.PuzzleInspect or GameMode.EvidencePresent or GameMode.Contradiction)
        {
            if (input.GetUICancelPressed())
                HandleGlobalCancelInput(mode);
            return;
        }

        if (input.GetUICancelPressed())
            HandleGlobalCancelInput(mode);
        if (input.getMenuPressed())
            HandleGlobalMenuInput(mode);
    }

    private void HandleGlobalCancelInput(GameMode currentMode)
    {
        if (TryHandleMainPanelCancel())
            return;

        if (TryHandleCancelByActivePanel())
            return;

        if (IsAnyActionPanelActive())
        {
            CloseAllPanels();
            GameModeManager.Instance.RequestChangeMode(GameMode.Explore);
            return;
        }

        if (currentMode is GameMode.InteractionMenu or GameMode.Pause or GameMode.Dialogue or GameMode.PuzzleInspect or GameMode.EvidencePresent or GameMode.Contradiction)
        {
            GameModeManager.Instance.RequestChangeMode(GameMode.Explore);
            CloseAllPanels();
        }
    }

    private bool TryHandleMainPanelCancel()
    {
        if (mainPanel == null || !mainPanel.activeSelf)
            return false;

        if (mainPanel.TryGetComponent(out MainPanelController mainController)
            && mainController.HandleCancelInput())
            return true;

        CloseMainPanel();
        return true;
    }

    private void CloseMainPanel()
    {
        if (mainPanel == null)
            return;

        mainPanel.SetActive(false);
        GameModeManager.Instance.RequestChangeMode(GameMode.Explore);
    }


    private void HandleGlobalMenuInput(GameMode currenrMode)
   {
    if (mainPanel != null && mainPanel.activeInHierarchy)
    {
        CloseAllPanels();
        CloseMainPanel();
        return;
    }

    if (currenrMode == GameMode.InteractionMenu)
        return;
    if (IsAnyActionPanelActive())
        return;
    GameModeManager.Instance.RequestChangeMode(GameMode.Pause);
    mainPanel.SetActive(true);
    }

    private void GetPanelsFromRoot(Transform root)
    {
        var panels = root.GetComponentsInChildren<PanelController>(true);
        foreach (var panel in panels)
        {
            _allPanelList.Add(panel);
            if(panel.PanelActionType is null)
            continue;
            _panelControllerDict.Add(panel.PanelActionType, panel);
        }
    }



    private bool TryHandleCancelByActivePanel()
    {
        foreach (var panel in _panelControllerDict.Values)
        {
            if (!panel.gameObject.activeSelf)
                continue;
            if (panel.HandleCancelInput())
                return true;
        }
        return false;
    }

    private bool IsAnyActionPanelActive()
    {
        foreach (var panel in _panelControllerDict.Values)
        {
            if (panel.gameObject.activeSelf)
                return true;
        }
        return false;
    }

    private void CloseAllPanels()
{
    foreach(var panel in _allPanelList)
    {
        panel.gameObject.SetActive(false);
    }
}

    public void OnEvent(PanelRequestEvent evt)
    {
        var panelType = evt.actionBase.GetType();

        _panelControllerDict.TryGetValue(panelType, out var panelController);
        panelController?.gameObject.SetActive(true);
        panelController?.SetupPanel(evt.actionBase);
        GameModeManager.Instance.RequestChangeMode(GameMode.InteractionMenu);
    }
}