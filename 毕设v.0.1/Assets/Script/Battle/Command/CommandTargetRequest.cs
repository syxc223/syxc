using System.Collections.Generic;

/// <summary>
/// 指令目标请求（教程 067“命令目标请求的逻辑重构”）。
/// 把“选了哪条指令”和“选哪个/哪些目标”拆成两步：
/// 玩家先选攻击/技能/道具，逻辑层据此生成本请求并广播，
/// UI 进入选目标流程，玩家选定后再把目标回交，命令才真正执行——命令与选目标彻底解耦。
/// </summary>
public class CommandTargetRequest
{
    public readonly BattleEntity Actor;
    public readonly CommandType Command;
    public readonly SkillDefinitionSO Skill;   // Command==Skill 时有效
    public readonly ConsumableItemSO Item;       // Command==Item 时有效
    public readonly BattleSide TargetSide;       // 目标阵营
    public readonly bool TargetAll;              // 是否全体目标
    public readonly IReadOnlyList<BattleEntity> Candidates; // 可选目标

    public CommandTargetRequest(BattleEntity actor, CommandType command,
        SkillDefinitionSO skill, ConsumableItemSO item,
        BattleSide targetSide, bool targetAll, IReadOnlyList<BattleEntity> candidates)
    {
        Actor = actor;
        Command = command;
        Skill = skill;
        Item = item;
        TargetSide = targetSide;
        TargetAll = targetAll;
        Candidates = candidates;
    }
}
