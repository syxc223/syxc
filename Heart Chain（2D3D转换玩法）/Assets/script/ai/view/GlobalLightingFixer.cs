using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 解决切换场景后“材质变黑/环境光丢失”的兜底脚本。
/// 原因通常是 RenderSettings（天空盒/环境光/GI）是全局的，前一个场景修改后，后一个场景没刷新或缺光源。
///
/// 用法：
/// 1) 把该脚本挂到任意物体上（建议：第一个启动场景），勾选 DontDestroyOnLoad 即可全局生效。
/// 2) 指定一个 fallbackSkybox（可选）。若某场景没有设置 skybox，会用它兜底。
/// 3) 如果你不想自动改 Ambient，只想强制刷新 GI，把 applyFallbackAmbient 取消勾选即可。
/// </summary>
public class GlobalLightingFixer : MonoBehaviour
{
    [Header("持久化")]
    public bool dontDestroyOnLoad = true;

    [Header("Skybox 兜底（可选）")]
    public Material fallbackSkybox;

    [Header("Ambient 兜底（可选）")]
    public bool applyFallbackAmbient = true;
    public AmbientMode ambientMode = AmbientMode.Skybox;
    public Color ambientLight = new Color(0.35f, 0.35f, 0.35f, 1f);
    [Range(0f, 3f)]
    public float ambientIntensity = 1f;

    [Header("GI 刷新")]
    public bool updateDynamicGIOnSceneLoad = true;

    static GlobalLightingFixer _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1) 如果某个场景没有设置 skybox，或者被前一个场景清空/替换导致过暗，用 fallback 兜底
        if (fallbackSkybox != null && RenderSettings.skybox == null)
        {
            RenderSettings.skybox = fallbackSkybox;
        }

        // 2) 兜底环境光，避免 Ambient 变成全黑
        if (applyFallbackAmbient)
        {
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientIntensity = ambientIntensity;

            // 只有在 Flat/Trilight 等模式时 ambientLight 才直接生效；Skybox 模式下主要由 skybox 决定
            RenderSettings.ambientLight = ambientLight;
        }

        // 3) 强制刷新环境 GI（常见：切场景后没有更新导致整体发黑）
        if (updateDynamicGIOnSceneLoad)
        {
            DynamicGI.UpdateEnvironment();
        }
    }
}

