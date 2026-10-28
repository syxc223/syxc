using System.Collections;
using System.Collections.Generic;
using Game;
using MFramework.Event;
using UnityEngine;

/// <summary>
/// 技能命令：按 SkillDefinitionSO 的段数/属性/类型，对已解析好的目标集合结算。
/// 目标由 BattleController 依据技能 TargetType 统一解析（避免敌我视角歧义）。
/// </summary>
public class SkillCommandHandler : BattleCommandHandlerBase
{
    private readonly SkillDefinitionSO _skill;
    private readonly List<BattleEntity> _targets;

    public SkillCommandHandler(BattleEntity actor, SkillDefinitionSO skill, List<BattleEntity> targets) : base(actor)
    {
        _skill = skill;
        _targets = targets;
    }

    public override IEnumerator Execute(BattleController controller)
    {
        EventBus.Publish(new BattleLogEvent($"{Actor.DisplayName} 使用了 {_skill.SkillName}"));

        for (int hit = 0; hit < Mathf.Max(1, _skill.Hits); hit++)
        {
            foreach (var target in _targets)
            {
                if (target == null || !target.IsAlive)
                    continue;

                switch (_skill.EffectType)
                {
                    case SkillEffectType.Damage:
                    {
                        var outcome = DamageCalculator.DealDamage(Actor, target, _skill, _skill.DamageType);
                        EventBus.Publish(new EntityDamagedEvent(
                            target, outcome.Damage, outcome.IsWeakness, outcome.IsCritical,
                            outcome.BrokeShield, outcome.DamageType));
                        if (outcome.BrokeShield)
                            EventBus.Publish(new ShieldBrokenEvent(target));
                        break;
                    }
                    case SkillEffectType.Heal:
                    {
                        int healed = DamageCalculator.Heal(Actor, target, _skill);
                        EventBus.Publish(new EntityHealedEvent(target, healed));
                        EventBus.Publish(new BattleLogEvent($"{target.DisplayName} 回复 {healed} 点生命"));
                        break;
                    }
                    case SkillEffectType.RestoreSP:
                    {
                        if (target.IsAlly)
                        {
                            target.Ally.ModifySP(_skill.Power);
                            EventBus.Publish(new BattleLogEvent($"{target.DisplayName} 回复 {_skill.Power} 点技力"));
                        }
                        break;
                    }
                }
            }

            yield return new WaitForSeconds(controller.Config.HitInterval);
        }
    }
}
