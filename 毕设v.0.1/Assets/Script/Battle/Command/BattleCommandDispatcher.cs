using System.Collections.Generic;

/// <summary>
/// 命令分发器（教程 068）：把“指令类型 + 参数”统一翻译成可执行的命令对象。
/// 玩家输入与敌人 AI 都经由此处创建命令，控制器只 StartCoroutine(handler.Execute(this))。
/// </summary>
public static class BattleCommandDispatcher
{
    public static AttackCommandHandler CreateAttack(BattleEntity actor, BattleEntity target)
        => new AttackCommandHandler(actor, target);

    public static SkillCommandHandler CreateSkill(BattleEntity actor, SkillDefinitionSO skill, List<BattleEntity> targets)
        => new SkillCommandHandler(actor, skill, targets);

    public static ItemCommandHandler CreateItem(BattleEntity actor, ConsumableItemSO item, BattleEntity target)
        => new ItemCommandHandler(actor, item, target);

    public static GuardCommandHandler CreateGuard(BattleEntity actor)
        => new GuardCommandHandler(actor);

    public static EscapeCommandHandler CreateFlee(BattleEntity actor)
        => new EscapeCommandHandler(actor);
}
