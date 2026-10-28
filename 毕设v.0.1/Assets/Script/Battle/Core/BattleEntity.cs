using Game;
using UnityEngine;

/// <summary>
/// 统一战斗单位（教程 052/054）。我方与敌方都包装成 BattleEntity，
/// 调度器、状态机、命令、计算引擎只面向 BattleEntity 编程，不再区分两套循环。
/// 我方持有 CharacterRuntimeData，敌方持有 EnemyRuntimeData（二选一）。
/// </summary>
public class BattleEntity
{
    private static int _idCounter;

    public readonly int Id;
    public readonly BattleSide Side;

    /// <summary>我方运行时数据（Side==Ally 时非空）</summary>
    public readonly CharacterRuntimeData Ally;

    /// <summary>敌方运行时数据（Side==Enemy 时非空）</summary>
    public readonly EnemyRuntimeData Enemy;

    public bool IsAlly => Side == BattleSide.Ally;

    /// <summary>对齐教程命名：是否为玩家方单位</summary>
    public bool IsPlayer => IsAlly;

    /// <summary>战场站位序号（教程 051 布局用）</summary>
    public int SlotIndex;

    /// <summary>时间轴上的“下次行动时刻”（教程 061 时间排序，M2 连续时间轴使用，M1 预留）</summary>
    public float NextActionTime;

    /// <summary>本回合是否处于防御姿态（受伤减半，下回合开始清除）</summary>
    public bool Guarding;

    /// <summary>
    /// 战场视图（MonoBehaviour）。M1 逻辑闭环阶段允许为空；
    /// M2 接入战斗单位动画/移动后，命令演出通过它播放（如教程 entity.Unit.MoveToPosition）。
    /// </summary>
    public MonoBehaviour View;

    public BattleEntity(CharacterRuntimeData ally, int slot)
    {
        Id = ++_idCounter;
        Side = BattleSide.Ally;
        Ally = ally;
        SlotIndex = slot;
    }

    public BattleEntity(EnemyRuntimeData enemy, int slot)
    {
        Id = ++_idCounter;
        Side = BattleSide.Enemy;
        Enemy = enemy;
        SlotIndex = slot;
    }

    public string DisplayName => IsAlly ? Ally.DisplayName : Enemy.Definition.Name;

    /// <summary>当前用于战斗结算的属性：我方取（基础+装备）总值，敌方取定义值</summary>
    public StatBlock Stats => IsAlly ? Ally.GetTotalStats() : Enemy.Stats;

    public int CurrentHP
    {
        get => IsAlly ? Ally.CurrentHP : Enemy.CurrentHP;
        set
        {
            if (IsAlly) Ally.CurrentHP = value;
            else Enemy.CurrentHP = value;
        }
    }

    public int CurrentSP
    {
        get => IsAlly ? Ally.CurrentSP : 0;
        set { if (IsAlly) Ally.CurrentSP = value; }
    }

    public bool IsAlive => IsAlly ? Ally.CurrentHP > 0 : !Enemy.IsDead;

    public bool IsBroken => !IsAlly && Enemy.IsBroken;

    /// <summary>敌人剩余护盾（我方无护盾概念，返回 -1 表示不适用）</summary>
    public int Shield => IsAlly ? -1 : Enemy.Shield;
}
