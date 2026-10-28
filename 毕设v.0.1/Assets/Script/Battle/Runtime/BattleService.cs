using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 战斗对外入口（教程 BattleService）：暗雷/明雷/调试都只跟它打交道——
/// StartBattle 负责进场（独立战斗场景或当前场景叠加），结束时负责返回探索。
/// M1 默认“当前场景叠加”以保证逻辑闭环可直接运行；建好 Addressables 战斗场景后，
/// 在 Inspector 勾选 useSeparateBattleScene 并指定战斗场景即可切换为教程的独立场景方案。
/// </summary>
public class BattleService : Singleton<BattleService>
{
    [Tooltip("是否加载独立战斗场景（需要 Addressables 战斗场景配置）；关闭则在当前场景叠加战斗。")]
    [SerializeField] private bool useSeparateBattleScene = false;

    [Tooltip("默认战斗场景（独立场景模式使用）")]
    [SerializeField] private AssetReference battleSceneReference;

    private AssetReference _previousScene;
    private BattleController _activeController;

    public static PendingBattleData PendingBattle { get; private set; }
    public bool IsBattleActive => _activeController != null;
    public static bool HasInstance => Instance != null;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>场景里没有挂 BattleService 时自动创建一个，降低配置成本。</summary>
    public static BattleService Ensure()
    {
        if (Instance != null)
            return Instance;
        var go = new GameObject(nameof(BattleService));
        return go.AddComponent<BattleService>();
    }

    /// <summary>开战。allies 为队伍快照，enemyDefs 为敌人定义，encounter 为遭遇配置。</summary>
    public void StartBattle(List<CharacterRuntimeData> allies, List<EnemyDefinitionSO> enemyDefs,
        EncounterSO encounter, AssetReference explicitScene = null)
    {
        if (IsBattleActive)
        {
            Debug.LogWarning("[BattleService] 已有战斗进行中，忽略重复开战请求");
            return;
        }

        var data = new PendingBattleData(
            new List<CharacterRuntimeData>(allies),
            new List<EnemyDefinitionSO>(enemyDefs),
            encounter);

        if (SceneLoadManager.Instance != null)
            _previousScene = SceneLoadManager.Instance.activeScene;

        AssetReference targetScene = explicitScene != null ? explicitScene : battleSceneReference;
        if (useSeparateBattleScene && targetScene != null && targetScene.RuntimeKeyIsValid())
        {
            // 教程正路：切到独立战斗场景，由场景内 BattleSceneStarter 接管
            PendingBattle = data;
            var request = new SceneLoadRequest(targetScene, FadeStyle.PanelFade, GameMode.Battle, null);
            SceneLoadManager.Instance.RequestLoad(request);
        }
        else
        {
            // M1：当前场景叠加，立刻可跑通逻辑闭环
            StartInCurrentScene(data);
        }
    }

    private void StartInCurrentScene(PendingBattleData data)
    {
        GameModeManager.Instance?.RequestChangeMode(GameMode.Battle);
        var go = new GameObject("BattleController");
        _activeController = go.AddComponent<BattleController>();
        _activeController.Init(data.Allies, data.EnemyDefs, data.Encounter);
    }

    /// <summary>独立战斗场景加载完成后，由场景内 BattleSceneStarter 调用。</summary>
    public void ConsumePendingInBattleScene()
    {
        if (PendingBattle == null)
            return;
        var data = PendingBattle;
        var go = new GameObject("BattleController");
        _activeController = go.AddComponent<BattleController>();
        _activeController.Init(data.Allies, data.EnemyDefs, data.Encounter);
    }

    /// <summary>返回进入战斗前的场景（教程 090 逃跑回场）。</summary>
    public void ReturnToPreviousScene()
    {
        if (useSeparateBattleScene
            && _previousScene != null && _previousScene.RuntimeKeyIsValid()
            && SceneLoadManager.Instance != null)
        {
            var request = new SceneLoadRequest(_previousScene, FadeStyle.PanelFade, GameMode.Explore, null);
            SceneLoadManager.Instance.RequestLoad(request);
        }
    }

    /// <summary>战斗结束统一收尾：独立场景则回场，叠加模式则回到探索。</summary>
    public void FinishBattleAndExit(BattleResult result)
    {
        _activeController = null;

        if (useSeparateBattleScene
            && _previousScene != null && _previousScene.RuntimeKeyIsValid()
            && SceneLoadManager.Instance != null)
        {
            ReturnToPreviousScene();
        }
        else
        {
            GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
        }

        PendingBattle = null;
    }
}

/// <summary>跨场景传递一场战斗所需数据（黑场期间暂存，战斗场景读取后清空）。</summary>
public class PendingBattleData
{
    public readonly List<CharacterRuntimeData> Allies;
    public readonly List<EnemyDefinitionSO> EnemyDefs;
    public readonly EncounterSO Encounter;

    public PendingBattleData(List<CharacterRuntimeData> allies, List<EnemyDefinitionSO> enemyDefs, EncounterSO encounter)
    {
        Allies = allies;
        EnemyDefs = enemyDefs;
        Encounter = encounter;
    }
}

/// <summary>挂在独立战斗场景根节点：场景启动时消费暂存数据、创建战斗控制器。</summary>
public class BattleSceneStarter : MonoBehaviour
{
    private void Start()
    {
        if (BattleService.HasInstance)
            BattleService.Instance.ConsumePendingInBattleScene();
    }
}
