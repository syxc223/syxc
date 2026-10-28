using System.Collections;
using System.Collections.Generic;
using Game;
using MFramework.Event;
using UnityEngine;

/// <summary>
/// 战斗场景内控制器（教程 BattleController）：持有全部战斗实体、状态机、调度器，
/// 用一个主协程按 FSM 状态推进整场战斗；玩家指令与敌人 AI 都转成命令对象执行。
/// 由 BattleService 创建（同场景叠加或独立战斗场景内）。
/// </summary>
public class BattleController : MonoBehaviour
{
    public BattleConfig Config = new();

    private readonly List<BattleEntity> _all = new();
    private readonly List<BattleEntity> _allies = new();
    private readonly List<BattleEntity> _enemies = new();

    public IReadOnlyList<BattleEntity> AllEntities => _all;
    public List<BattleEntity> AliveAllies => _allies.FindAll(e => e.IsAlive);
    public List<BattleEntity> AliveEnemies => _enemies.FindAll(e => e.IsAlive);

    public BattleEntity CurrentActor => _current;
    public BattleState State => _fsm != null ? _fsm.Current : BattleState.None;

    private TurnScheduler _scheduler;
    private readonly BattleStateMachine _fsm = new();
    private EncounterSO _encounter;
    private BattleEntity _current;
    private Coroutine _loop;
    private BattleUIController _ui;

    private bool _ended;
    private BattleResult _result = BattleResult.None;
    private bool _resultConfirmed;

    // 玩家指令挂起状态
    private CommandType _pendingType;
    private SkillDefinitionSO _pendingSkill;
    private ConsumableItemSO _pendingItem;
    private BattleCommandHandlerBase _resolvedCommand;

    /// <summary>由 BattleService 调用：用队伍快照与敌人定义构建实体并开战。</summary>
    public void Init(List<CharacterRuntimeData> allyData, List<EnemyDefinitionSO> enemyDefs, EncounterSO encounter)
    {
        _encounter = encounter;

        int allySlot = 0;
        foreach (var data in allyData)
        {
            var entity = new BattleEntity(data, allySlot++);
            _allies.Add(entity);
            _all.Add(entity);
        }

        int enemySlot = 0;
        foreach (var def in enemyDefs)
        {
            var runtime = new EnemyRuntimeData(def);
            var entity = new BattleEntity(runtime, enemySlot++);
            _enemies.Add(entity);
            _all.Add(entity);
        }

        _scheduler = new TurnScheduler(_all);

        GameModeManager.Instance?.RequestChangeMode(GameMode.Battle);
        EnsureUI();

        EventBus.Publish(new BattleStartedEvent(_all, encounter != null ? encounter.BattleBackground : null));
        _loop = StartCoroutine(BattleLoop());
    }

    private void EnsureUI()
    {
        if (_ui != null)
            return;
        var go = new GameObject("BattleUIController");
        _ui = go.AddComponent<BattleUIController>();
        _ui.Bind(this);
    }

    private IEnumerator BattleLoop()
    {
        _fsm.Change(BattleState.Intro);
        yield return new WaitForSeconds(Config.IntroDuration);
        EventBus.Publish(new TurnOrderBuiltEvent(_scheduler.CurrentOrder));

        while (!_ended)
        {
            _fsm.Change(BattleState.TurnStart);
            ClearGuardsAtRoundStart();

            _current = _scheduler.Next();
            if (_current == null)
                break;

            EventBus.Publish(new EntityTurnStartedEvent(_current));

            // 破防敌人跳过本次行动并恢复护盾
            if (!_current.IsAlly && _current.IsBroken)
            {
                EventBus.Publish(new BattleLogEvent($"{_current.DisplayName} 处于破防状态，无法行动"));
                _current.Enemy.RecoverFromBreak();
                yield return new WaitForSeconds(Config.ActionInterval);
                if (CheckBattleEnd())
                    break;
                _fsm.Change(BattleState.TurnEnd);
                continue;
            }

            BattleCommandHandlerBase handler;
            if (_current.IsAlly)
            {
                _fsm.Change(BattleState.AwaitCommand);
                EventBus.Publish(new AwaitPlayerCommandEvent(_current));

                // 等玩家把指令解析成命令对象（选目标是其中的异步步骤）
                yield return new WaitUntil(() => _ended || _resolvedCommand != null);
                if (_ended)
                    yield break;

                handler = _resolvedCommand;
                _resolvedCommand = null;
            }
            else
            {
                yield return new WaitForSeconds(Config.ActionInterval);
                handler = BuildEnemyCommand(_current);
            }

            _fsm.Change(BattleState.Execute);
            yield return StartCoroutine(handler.Execute(this));

            _fsm.Change(BattleState.TurnEnd);
            EventBus.Publish(new TurnOrderBuiltEvent(_scheduler.CurrentOrder));

            if (CheckBattleEnd())
                break;

            yield return new WaitForSeconds(Config.ActionInterval);
        }

        if (!_ended)
            yield return StartCoroutine(FinishBattle());
    }

    // ---------- 玩家指令入口（由 BattleUIController 调用） ----------

    /// <summary>玩家选定一条指令；需要选目标则广播目标请求，否则直接成立。</summary>
    public void BeginCommand(CommandType type, SkillDefinitionSO skill = null, ConsumableItemSO item = null)
    {
        if (_current == null || !_current.IsAlly || _ended)
            return;

        _pendingType = type;
        _pendingSkill = skill;
        _pendingItem = item;

        switch (type)
        {
            case CommandType.Guard:
                ResolveCommand(BattleCommandDispatcher.CreateGuard(_current));
                break;

            case CommandType.Flee:
                TryFlee();
                break;

            case CommandType.Attack:
                _fsm.Change(BattleState.SelectTarget);
                EventBus.Publish(new CommandTargetRequestEvent(
                    new CommandTargetRequest(_current, type, null, null, BattleSide.Enemy, false, AliveEnemies)));
                break;

            case CommandType.Skill:
                BeginSkill(skill);
                break;

            case CommandType.Item:
                BeginItem(item);
                break;
        }
    }

    private void BeginSkill(SkillDefinitionSO skill)
    {
        if (skill == null)
        {
            BackToCommandMenu();
            return;
        }
        if (_current.CurrentSP < skill.SpCost)
        {
            EventBus.Publish(new BattleLogEvent("技力不足"));
            BackToCommandMenu();
            return;
        }

        switch (skill.TargetType)
        {
            case SkillTargetType.EnemyAll:
                _current.Ally.ModifySP(-skill.SpCost);
                ResolveCommand(BattleCommandDispatcher.CreateSkill(_current, skill, AliveEnemies));
                break;
            case SkillTargetType.Self:
                _current.Ally.ModifySP(-skill.SpCost);
                ResolveCommand(BattleCommandDispatcher.CreateSkill(_current, skill, new List<BattleEntity> { _current }));
                break;
            case SkillTargetType.AllySingle:
                _fsm.Change(BattleState.SelectTarget);
                EventBus.Publish(new CommandTargetRequestEvent(
                    new CommandTargetRequest(_current, CommandType.Skill, skill, null, BattleSide.Ally, false, _allies)));
                break;
            default: // EnemySingle
                _fsm.Change(BattleState.SelectTarget);
                EventBus.Publish(new CommandTargetRequestEvent(
                    new CommandTargetRequest(_current, CommandType.Skill, skill, null, BattleSide.Enemy, false, AliveEnemies)));
                break;
        }
    }

    private void BeginItem(ConsumableItemSO item)
    {
        if (item == null)
        {
            BackToCommandMenu();
            return;
        }
        // 消耗品作用于我方单体（含阵亡队友，便于复活）
        _fsm.Change(BattleState.SelectTarget);
        EventBus.Publish(new CommandTargetRequestEvent(
            new CommandTargetRequest(_current, CommandType.Item, null, item, BattleSide.Ally, false, _allies)));
    }

    /// <summary>玩家在目标列表中选定目标后调用。</summary>
    public void SubmitTargets(List<BattleEntity> chosen)
    {
        if (chosen == null || chosen.Count == 0)
        {
            BackToCommandMenu();
            return;
        }

        switch (_pendingType)
        {
            case CommandType.Attack:
                ResolveCommand(BattleCommandDispatcher.CreateAttack(_current, chosen[0]));
                break;
            case CommandType.Skill:
                _current.Ally.ModifySP(-_pendingSkill.SpCost);
                ResolveCommand(BattleCommandDispatcher.CreateSkill(_current, _pendingSkill, chosen));
                break;
            case CommandType.Item:
                InventoryManager.Instance?.RemoveItem(_pendingItem, 1);
                ResolveCommand(BattleCommandDispatcher.CreateItem(_current, _pendingItem, chosen[0]));
                break;
        }
        ClearTargetSelection();
    }

    /// <summary>玩家取消选目标，回到指令菜单。</summary>
    public void CancelTargetSelection()
    {
        ClearTargetSelection();
        BackToCommandMenu();
    }

    public void ClearTargetSelection()
    {
        _pendingSkill = null;
        _pendingItem = null;
    }

    private void BackToCommandMenu()
    {
        _fsm.Change(BattleState.AwaitCommand);
        EventBus.Publish(new AwaitPlayerCommandEvent(_current));
    }

    private void ResolveCommand(BattleCommandHandlerBase handler)
    {
        _resolvedCommand = handler;
    }

    private void TryFlee()
    {
        if (Random.value <= Config.EscapeSuccessRate)
        {
            ResolveCommand(BattleCommandDispatcher.CreateFlee(_current));
        }
        else
        {
            EventBus.Publish(new BattleLogEvent("逃跑失败！"));
            ResolveCommand(new PassTurnHandler(_current, "但没能逃走"));
        }
    }

    // ---------- 敌人 AI ----------

    private BattleCommandHandlerBase BuildEnemyCommand(BattleEntity enemy)
    {
        var allies = AliveAllies;
        if (allies.Count == 0)
            return new PassTurnHandler(enemy, "失去了目标");

        var skill = EnemyAISelector.PickSkill(enemy);
        if (skill == null)
        {
            var target = allies[Random.Range(0, allies.Count)];
            return BattleCommandDispatcher.CreateAttack(enemy, target);
        }
        return BattleCommandDispatcher.CreateSkill(enemy, skill, ResolveTargets(enemy, skill));
    }

    /// <summary>以行动者视角解析技能目标：技能里的“敌方”=行动者的对面阵营。</summary>
    private List<BattleEntity> ResolveTargets(BattleEntity actor, SkillDefinitionSO skill)
    {
        bool opponentIsEnemySide = actor.IsAlly; // 我方释放时，对面是敌人
        var result = new List<BattleEntity>();

        switch (skill.TargetType)
        {
            case SkillTargetType.EnemyAll:
                result.AddRange(opponentIsEnemySide ? AliveEnemies : AliveAllies);
                break;
            case SkillTargetType.EnemySingle:
            {
                var pool = opponentIsEnemySide ? AliveEnemies : AliveAllies;
                if (pool.Count > 0)
                    result.Add(pool[Random.Range(0, pool.Count)]);
                break;
            }
            case SkillTargetType.AllySingle:
            case SkillTargetType.Self:
                result.Add(actor);
                break;
        }
        return result;
    }

    // ---------- 胜负与结算 ----------

    private bool CheckBattleEnd()
    {
        if (AliveEnemies.Count == 0)
        {
            _result = BattleResult.Victory;
            return true;
        }
        if (AliveAllies.Count == 0)
        {
            _result = BattleResult.Defeat;
            return true;
        }
        return false;
    }

    private IEnumerator FinishBattle()
    {
        _fsm.Change(BattleState.Settlement);

        SettlementResult settlement = null;
        if (_result == BattleResult.Victory)
        {
            settlement = BattleSettlement.Settle(_allies, _enemies, _encounter);
            if (settlement.Money > 0)
                InventoryManager.Instance?.AddCurrency(settlement.Money);
        }
        else if (_result == BattleResult.Defeat)
        {
            // 战败从宽：存活/阵亡队友统一回到半血以上，避免卡关
            foreach (var ally in _allies)
                ally.CurrentHP = Mathf.Max(1, ally.Stats.MaxHP / 2);
        }

        var levelUps = settlement != null ? settlement.LevelUpMessages : new List<string>();
        var drops = settlement != null ? settlement.Drops : new List<InventoryItem>();
        int exp = settlement != null ? settlement.TotalExp : 0;
        int money = settlement != null ? settlement.Money : 0;

        EventBus.Publish(new BattleResultReadyEvent(_result, exp, money, levelUps, drops));

        _resultConfirmed = false;
        yield return new WaitUntil(() => _resultConfirmed || _ended);
        if (_ended)
            yield break;

        _fsm.Change(BattleState.Ended);
        EventBus.Publish(new BattleEndedEvent(_result));

        if (BattleService.HasInstance)
            BattleService.Instance.FinishBattleAndExit(_result);

        if (_ui != null)
            Destroy(_ui.gameObject);
        Destroy(gameObject, 0.1f);
    }

    /// <summary>结果面板点击确认。</summary>
    public void ConfirmResult() => _resultConfirmed = true;

    /// <summary>逃跑命令调用：立即终止战斗（教程 090 controller.StopBattle）。</summary>
    public void StopBattle(BattleResult result)
    {
        if (_ended)
            return;
        _ended = true;
        _result = result;
        _fsm.Change(BattleState.Ended);
        EventBus.Publish(new BattleEndedEvent(result));

        if (BattleService.HasInstance)
            BattleService.Instance.FinishBattleAndExit(result);

        if (_ui != null)
            Destroy(_ui.gameObject);
        Destroy(gameObject, 0.1f);
    }

    private void ClearGuardsAtRoundStart()
    {
        foreach (var entity in _all)
            entity.Guarding = false;
    }
}

/// <summary>空过一回合的命令（逃跑失败等场景）。</summary>
public class PassTurnHandler : BattleCommandHandlerBase
{
    private readonly string _reason;
    public PassTurnHandler(BattleEntity actor, string reason) : base(actor) => _reason = reason;

    public override IEnumerator Execute(BattleController controller)
    {
        EventBus.Publish(new BattleLogEvent($"{Actor.DisplayName} {_reason}"));
        yield return new WaitForSeconds(controller.Config.ActionInterval);
    }
}
