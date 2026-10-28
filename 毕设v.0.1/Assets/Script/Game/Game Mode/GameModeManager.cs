using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeManager : Singleton<GameModeManager>
{
    public GameMode CurrentGameMode;
    [SerializeField] private GameMode defaultMode = GameMode.Explore;

    protected override void Awake()
    {
        base.Awake();
        CurrentGameMode = defaultMode;
    }

    void Start()
    {
       //广播当前游戏模式
       ApplyMode(CurrentGameMode);
    }

    public void RequestChangeMode(GameMode newMode)
{
    if (Instance != this)
        return;

    if (!CanSwitchMode(newMode))
        return;

    ApplyMode(newMode);
}

    public bool CanSwitchMode(GameMode newMode) => true;

     private void ApplyMode(GameMode newMode)
{
    CurrentGameMode = newMode;
    EventBus.Publish(new GameModeChangedEvent(CurrentGameMode));
}
}
