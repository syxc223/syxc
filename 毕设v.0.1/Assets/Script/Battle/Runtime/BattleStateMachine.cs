using MFramework.Event;

using System.Collections.Generic;
/// <summary>
/// 战斗有限状态机（教程 053 初始化 / 057 状态切换）。
/// 显式记录当前状态、校验迁移合法性，并在迁移时广播 BattleStateChangedEvent，
/// 让 UI/演出跟随状态走，替代旧版散落的 bool 标志。
/// </summary>
public class BattleStateMachine
{
    public BattleState Current { get; private set; } = BattleState.None;

    /// <summary>允许的状态迁移表（key 的状态可以迁移到 value 中任一状态）。</summary>
    private static readonly Dictionary<BattleState, BattleState[]> Allowed = new()
    {
        { BattleState.None,      new[]{ BattleState.Intro } },
        { BattleState.Intro,     new[]{ BattleState.TurnStart, BattleState.Ended } },
        { BattleState.TurnStart, new[]{ BattleState.AwaitCommand, BattleState.Execute, BattleState.Settlement } },
        { BattleState.AwaitCommand, new[]{ BattleState.SelectTarget, BattleState.Execute, BattleState.Ended } },
        { BattleState.SelectTarget, new[]{ BattleState.Execute, BattleState.AwaitCommand } },
        { BattleState.Execute,  new[]{ BattleState.TurnEnd, BattleState.Settlement, BattleState.Ended } },
        { BattleState.TurnEnd,  new[]{ BattleState.TurnStart } },
        { BattleState.Settlement,new[]{ BattleState.Ended } },
        { BattleState.Ended,    new[]{ BattleState.None } },
    };

    public bool CanTransitionTo(BattleState next)
    {
        if (Current == next)
            return true;
        return Allowed.TryGetValue(Current, out var targets) && System.Array.IndexOf(targets, next) >= 0;
    }

    /// <summary>迁移状态并广播；非法迁移给出警告但不强行跳转。</summary>
    public bool Change(BattleState next)
    {
        if (!CanTransitionTo(next))
        {
            UnityEngine.Debug.LogWarning($"[BattleFSM] 非法状态迁移：{Current} -> {next}");
            return false;
        }

        var old = Current;
        Current = next;
        EventBus.Publish(new BattleStateChangedEvent(old, next));
        return true;
    }

    public void Reset()
    {
        Current = BattleState.None;
    }
}
