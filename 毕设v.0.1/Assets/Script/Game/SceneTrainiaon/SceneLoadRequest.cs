using UnityEngine.AddressableAssets;

/// <summary>
/// 场景加载请求 —— 纯数据载体。
/// 由调用方构造后传给 SceneLoadManager.RequestLoad()。
/// </summary>
public struct SceneLoadRequest
{
    public readonly AssetReference Scene;
    public readonly FadeStyle FadeStyle;
    public readonly GameMode ModeAfterLoad;
    public readonly string SpawnPointId;

    public readonly float FadeOutDurationOverride;
    public readonly float FadeInDurationOverride;
    public readonly float PostLoadBlackScreenDuration;

    public SceneLoadRequest(
        AssetReference scene,
        FadeStyle fadeStyle,
        GameMode modeAfterLoad,
        string spawnPointId = null,
        float fadeOutDurationOverride = -1f,
        float fadeInDurationOverride = -1f,
        float postLoadBlackScreenDuration = 0.35f)
    {
        Scene = scene;
        FadeStyle = fadeStyle;
        ModeAfterLoad = modeAfterLoad;
        SpawnPointId = spawnPointId;
        FadeOutDurationOverride = fadeOutDurationOverride;
        FadeInDurationOverride = fadeInDurationOverride;
        PostLoadBlackScreenDuration = postLoadBlackScreenDuration;
    }
}
