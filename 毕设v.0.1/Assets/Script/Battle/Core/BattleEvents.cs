using System.Collections.Generic;
using Game;
using MFramework.Event;
using UnityEngine;

// 战斗事件集：逻辑层只负责 Publish，表现层（BattleUIController）订阅后刷新界面，二者解耦。

/// <summary>战斗开始：实体已按阵营生成完毕</summary>
public readonly struct BattleStartedEvent : IEvent
{
    public readonly IReadOnlyList<BattleEntity> AllEntities;
    public readonly Sprite Background;
    public BattleStartedEvent(IReadOnlyList<BattleEntity> allEntities, Sprite background)
    {
        AllEntities = allEntities;
        Background = background;
    }
}

/// <summary>战斗状态机发生迁移（教程 057）</summary>
public readonly struct BattleStateChangedEvent : IEvent
{
    public readonly BattleState OldState;
    public readonly BattleState NewState;
    public BattleStateChangedEvent(BattleState oldState, BattleState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

/// <summary>本轮行动顺序已排好（教程 060/062 行动顺序 UI）</summary>
public readonly struct TurnOrderBuiltEvent : IEvent
{
    public readonly IReadOnlyList<BattleEntity> Order;
    public TurnOrderBuiltEvent(IReadOnlyList<BattleEntity> order) => Order = order;
}

/// <summary>轮到某个实体行动（教程 063 当前行动者 HUD 高亮）</summary>
public readonly struct EntityTurnStartedEvent : IEvent
{
    public readonly BattleEntity Actor;
    public EntityTurnStartedEvent(BattleEntity actor) => Actor = actor;
}

/// <summary>轮到我方，需要弹出指令菜单等待玩家选择</summary>
public readonly struct AwaitPlayerCommandEvent : IEvent
{
    public readonly BattleEntity Actor;
    public AwaitPlayerCommandEvent(BattleEntity actor) => Actor = actor;
}

/// <summary>需要玩家选择指令目标（教程 058/067，命令与选目标解耦）</summary>
public readonly struct CommandTargetRequestEvent : IEvent
{
    public readonly CommandTargetRequest Request;
    public CommandTargetRequestEvent(CommandTargetRequest request) => Request = request;
}

/// <summary>某个实体受到伤害（教程 073 飘字 / 血条 / 护盾刷新）</summary>
public readonly struct EntityDamagedEvent : IEvent
{
    public readonly BattleEntity Target;
    public readonly int Damage;
    public readonly bool IsWeakness;
    public readonly bool IsCritical;
    public readonly bool BrokeShield;
    public readonly DamageType DamageType;
    public EntityDamagedEvent(BattleEntity target, int damage, bool isWeakness, bool isCritical, bool brokeShield, DamageType damageType)
    {
        Target = target;
        Damage = damage;
        IsWeakness = isWeakness;
        IsCritical = isCritical;
        BrokeShield = brokeShield;
        DamageType = damageType;
    }
}

/// <summary>某个实体被治疗</summary>
public readonly struct EntityHealedEvent : IEvent
{
    public readonly BattleEntity Target;
    public readonly int Amount;
    public EntityHealedEvent(BattleEntity target, int amount)
    {
        Target = target;
        Amount = amount;
    }
}

/// <summary>敌人护盾被击破（教程 080 破盾表现）</summary>
public readonly struct ShieldBrokenEvent : IEvent
{
    public readonly BattleEntity Enemy;
    public ShieldBrokenEvent(BattleEntity enemy) => Enemy = enemy;
}

/// <summary>战斗日志一行（UI 底部提示）</summary>
public readonly struct BattleLogEvent : IEvent
{
    public readonly string Message;
    public BattleLogEvent(string message) => Message = message;
}

/// <summary>战斗胜负已定，等待玩家在结果面板确认（教程 088）</summary>
public readonly struct BattleResultReadyEvent : IEvent
{
    public readonly BattleResult Result;
    public readonly int ExpGained;
    public readonly int MoneyGained;
    public readonly IReadOnlyList<string> LevelUpMessages;
    public readonly IReadOnlyList<InventoryItem> Drops;
    public BattleResultReadyEvent(BattleResult result, int expGained, int moneyGained,
        IReadOnlyList<string> levelUpMessages, IReadOnlyList<InventoryItem> drops)
    {
        Result = result;
        ExpGained = expGained;
        MoneyGained = moneyGained;
        LevelUpMessages = levelUpMessages;
        Drops = drops;
    }
}

/// <summary>整场战斗结束（UI 拆除战斗画布，控制器退场）</summary>
public readonly struct BattleEndedEvent : IEvent
{
    public readonly BattleResult Result;
    public BattleEndedEvent(BattleResult result) => Result = result;
}
