using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景加载管理器。使用 Addressables 异步加载/卸载场景。
/// 典型流程：锁定输入 → FadeOut → 卸载旧场景 → 加载新场景 → 落点定位 → 黑场缓冲 → FadeIn → 切换模式。
/// </summary>
public class SceneLoadManager : Singleton<SceneLoadManager>
{
    private bool isLoading;

    [Header("默认场景")]
    [SerializeField] private AssetReference menuScene;

    /// <summary>当前活跃的场景引用</summary>
    public AssetReference activeScene;

    /// <summary>当前活跃的场景实例（由 Addressables 加载）</summary>
    public SceneInstance ActiveSceneInstance { get; private set; }

    /// <summary>游戏启动时的初始场景（由 Unity Build Settings 加载，非 Addressables）</summary>
    private Scene _initialScene;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 捕获游戏启动时的初始场景（由 Unity 直接加载，不在 Addressables 管理范围内）
        _initialScene = SceneManager.GetActiveScene();
        if (_initialScene.IsValid())
        {
            Debug.Log($"[SceneLoadManager] 初始场景已追踪: {_initialScene.name}");
        }
    }

    // ========================================================================
    // 公共入口
    // ========================================================================

    /// <summary>
    /// 请求加载场景（完整流程：淡出 → 卸载 → 加载 → 落点 → 黑场 → 淡入）。
    /// 如果当前正在加载中，新的请求会被忽略。
    /// </summary>
    public void RequestLoad(SceneLoadRequest request)
    {
        if (isLoading)
        {
            Debug.LogWarning("[SceneLoadManager] 当前正在加载场景，忽略新请求。");
            return;
        }

        StartCoroutine(LoadFlow(request));
    }

    // ========================================================================
    // 完整加载流程
    // ========================================================================

    private IEnumerator LoadFlow(SceneLoadRequest request)
    {
        // 入口校验
        if (request.Scene == null || string.IsNullOrEmpty(request.Scene.RuntimeKey.ToString()))
        {
            Debug.LogError("[SceneLoadManager] LoadFlow: request.Scene 为空或无效，取消加载。");
            yield break;
        }

        isLoading = true;
        Debug.Log($"[SceneLoadManager] 开始加载流程 → 目标场景: {request.Scene}, 目标模式: {request.ModeAfterLoad}");

        try
        {
            // ---- 1. 锁定输入：切到过渡模式 ----
            Debug.Log("[SceneLoadManager] 步骤1: 切换到过渡模式");
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RequestChangeMode(GameMode.InteractionMenu);
            }

            // ---- 2. 配置 Fade 样式和单次时长覆盖 ----
            Debug.Log("[SceneLoadManager] 步骤2: 配置淡入淡出");
            if (FadeController.Instance != null)
            {
                FadeController.Instance.SetStyle(request.FadeStyle);
                // 注意：参数顺序为 (fadeInDuration, fadeOutDuration)
                FadeController.Instance.SetNextFadeDurations(
                    request.FadeInDurationOverride,
                    request.FadeOutDurationOverride);
            }

            // ---- 3. 淡出到全黑 ----
            Debug.Log("[SceneLoadManager] 步骤3: 开始淡出");
            yield return WaitForFade(isFadeOut: true);
            Debug.Log("[SceneLoadManager] 步骤3: 淡出完成");

            // ---- 4. 卸载所有旧场景 ----
            Debug.Log("[SceneLoadManager] 步骤4: 卸载旧场景");

            // 4a. 卸载由 Addressables 加载的旧场景
            if (ActiveSceneInstance.Scene.IsValid() && ActiveSceneInstance.Scene.isLoaded)
            {
                Debug.Log($"[SceneLoadManager] 卸载 Addressables 场景: {ActiveSceneInstance.Scene.name}");
                var unloadHandle = Addressables.UnloadSceneAsync(ActiveSceneInstance, true);
                yield return unloadHandle;

                if (unloadHandle.Status == AsyncOperationStatus.Succeeded)
                    Debug.Log($"[SceneLoadManager] Addressables 场景卸载成功: {ActiveSceneInstance.Scene.name}");
                else
                    Debug.LogWarning($"[SceneLoadManager] Addressables 场景卸载失败: {ActiveSceneInstance.Scene.name}");

                ActiveSceneInstance = default;
                activeScene = null;
            }

            // 4b. 卸载游戏启动时 Unity 直接加载的初始场景（非 Addressables 管理）
            if (_initialScene.IsValid() && _initialScene.isLoaded)
            {
                Debug.Log($"[SceneLoadManager] 卸载初始场景: {_initialScene.name}");
                var unloadOp = SceneManager.UnloadSceneAsync(_initialScene);
                if (unloadOp != null)
                {
                    yield return unloadOp;
                    Debug.Log($"[SceneLoadManager] 初始场景卸载完成: {_initialScene.name}");
                }
            }

            // ---- 5. 加载目标场景 ----
            Debug.Log($"[SceneLoadManager] 步骤5: 开始加载场景 {request.Scene}");
            var loadHandle = Addressables.LoadSceneAsync(request.Scene, LoadSceneMode.Additive, true);
            yield return loadHandle;

            bool loadSucceeded = loadHandle.Status == AsyncOperationStatus.Succeeded;

            if (loadSucceeded)
            {
                ActiveSceneInstance = loadHandle.Result;
                activeScene = request.Scene;
                SceneManager.SetActiveScene(loadHandle.Result.Scene);

                Debug.Log($"[SceneLoadManager] 步骤5: 场景加载成功 → {request.Scene}");

                // ---- 6. 广播"场景加载完成"事件 ----
                Debug.Log($"[SceneLoadManager] 步骤6: 广播 SceneLoadedEvent (spawnPointId={request.SpawnPointId ?? "null"})");
                EventBus.Publish(new SceneLoadedEvent(
                    request.Scene,
                    request.ModeAfterLoad,
                    request.SpawnPointId));
            }
            else
            {
                Debug.LogError($"[SceneLoadManager] 步骤5: 场景加载失败 → {request.Scene}\nError: {loadHandle.OperationException}");
            }

            // ---- 7. 如果目标是 Explore 模式，在黑场里恢复模式 + 归零摄像机 ----
            if (loadSucceeded && request.ModeAfterLoad == GameMode.Explore)
            {
                Debug.Log("[SceneLoadManager] 步骤7: Explore 模式 → 切模式 + 摄像机归零");
                if (GameModeManager.Instance != null)
                {
                    GameModeManager.Instance.RequestChangeMode(GameMode.Explore);
                }

                yield return null;

                if (Camera.main != null)
                {
                    Camera.main.transform.position = Vector3.zero;
                    Camera.main.transform.rotation = Quaternion.identity;
                }
            }

            // ---- 8. 黑场缓冲 ----
            if (request.PostLoadBlackScreenDuration > 0f)
            {
                Debug.Log($"[SceneLoadManager] 步骤8: 黑场缓冲 {request.PostLoadBlackScreenDuration}s");
                yield return new WaitForSecondsRealtime(request.PostLoadBlackScreenDuration);
            }

            // ---- 9. 画面淡入回来 ----
            Debug.Log("[SceneLoadManager] 步骤9: 开始淡入");
            yield return WaitForFade(isFadeOut: false);
            Debug.Log("[SceneLoadManager] 步骤9: 淡入完成");

            // ---- 10. 如果目标模式不是 Explore，淡入后再切模式 ----
            if (loadSucceeded && request.ModeAfterLoad != GameMode.Explore)
            {
                Debug.Log($"[SceneLoadManager] 步骤10: 切换到目标模式 {request.ModeAfterLoad}");
                if (GameModeManager.Instance != null)
                {
                    GameModeManager.Instance.RequestChangeMode(request.ModeAfterLoad);
                }
            }
        }
        finally
        {
            isLoading = false;
            Debug.Log("[SceneLoadManager] 加载流程结束");
        }
    }

    // ========================================================================
    // 辅助方法
    // ========================================================================

    /// <summary>
    /// 等待淡入或淡出完成。如果 FadeController 不可用则立即返回。
    /// </summary>
    private static IEnumerator WaitForFade(bool isFadeOut)
    {
        var fadeCtrl = FadeController.Instance;
        if (fadeCtrl == null)
        {
            Debug.LogWarning("[SceneLoadManager] FadeController.Instance 为空，跳过淡入淡出。");
            yield break;
        }

        bool complete = false;
        if (isFadeOut)
        {
            fadeCtrl.FadeOut(() => complete = true);
        }
        else
        {
            fadeCtrl.FadeIn(() => complete = true);
        }

        yield return new WaitUntil(() => complete);
    }

    // ========================================================================
    // 轻量级方法（兼容旧调用，不涉及淡入淡出）
    // ========================================================================

    /// <summary>
    /// 使用 Addressables 异步加载场景（Additive 模式）。
    /// 不包含淡入淡出和模式切换，适用于需要手动控制流程的场景。
    /// </summary>
    /// <param name="sceneRef">Addressables 场景引用</param>
    /// <param name="onLoaded">加载完成回调，传入 SceneInstance</param>
    public void LoadSceneAdditiveAsync(AssetReference sceneRef, Action<SceneInstance> onLoaded = null)
    {
        if (sceneRef == null || string.IsNullOrEmpty(sceneRef.RuntimeKey.ToString()))
        {
            Debug.LogWarning("[SceneLoadManager] LoadSceneAdditiveAsync: sceneRef 为空");
            onLoaded?.Invoke(default);
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneRef, onLoaded));
    }

    private IEnumerator LoadSceneRoutine(AssetReference sceneRef, Action<SceneInstance> onLoaded)
    {
        var handle = sceneRef.LoadSceneAsync(LoadSceneMode.Additive);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            ActiveSceneInstance = handle.Result;
            activeScene = sceneRef;
            Debug.Log($"[SceneLoadManager] 场景加载成功: {sceneRef}");
        }
        else
        {
            Debug.LogError($"[SceneLoadManager] 场景加载失败: {sceneRef}, Error: {handle.OperationException}");
        }

        onLoaded?.Invoke(ActiveSceneInstance);
    }

    /// <summary>
    /// 卸载当前活跃场景。
    /// </summary>
    /// <param name="onUnloaded">卸载完成回调</param>
    public void UnloadActiveSceneAsync(Action onUnloaded = null)
    {
        if (!ActiveSceneInstance.Scene.IsValid() || !ActiveSceneInstance.Scene.isLoaded)
        {
            onUnloaded?.Invoke();
            return;
        }

        StartCoroutine(UnloadSceneRoutine(onUnloaded));
    }

    private IEnumerator UnloadSceneRoutine(Action onUnloaded)
    {
        var scene = ActiveSceneInstance.Scene;
        var handle = Addressables.UnloadSceneAsync(ActiveSceneInstance);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"[SceneLoadManager] 场景卸载成功: {scene.name}");
        }
        else
        {
            Debug.LogError($"[SceneLoadManager] 场景卸载失败: {scene.name}");
        }

        ActiveSceneInstance = default;
        activeScene = null;
        onUnloaded?.Invoke();
    }
}
