using MFramework.Event;
using UnityEngine.AddressableAssets;

/// <summary>
/// 场景加载完成事件。在 fadeIn 之前广播，此时画面仍为黑场。
/// 订阅者可以在此事件中完成角色落点、UI 初始化等准备工作。
/// </summary>
public readonly struct SceneLoadedEvent : IEvent
{
    /// <summary>已加载的场景引用</summary>
    public readonly AssetReference Scene;
    /// <summary>加载后的游戏模式</summary>
    public readonly GameMode ModeAfterLoad;
    /// <summary>目标出生点 ID（可能为 null）</summary>
    public readonly string SpawnPointId;

    public SceneLoadedEvent(AssetReference scene, GameMode modeAfterLoad, string spawnPointId)
    {
        Scene = scene;
        ModeAfterLoad = modeAfterLoad;
        SpawnPointId = spawnPointId;
    }
}
