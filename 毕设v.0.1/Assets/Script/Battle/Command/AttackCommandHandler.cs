using System.Collections;
using Game;
using MFramework.Event;
using UnityEngine;

/// <summary>普通攻击：用武器属性打单体，可触发弱点削盾/破防。</summary>
public class AttackCommandHandler : BattleCommandHandlerBase
{
    private readonly BattleEntity _target;

    public AttackCommandHandler(BattleEntity actor, BattleEntity target) : base(actor)
    {
        _target = target;
    }

    public override IEnumerator Execute(BattleController controller)
    {
        // 我方普攻属性取武器类型；敌方普攻无属性
        DamageType type = Actor.IsAlly
            ? DamageCalculator.GetWeaponDamageType(Actor.Ally)
            : DamageType.None;

        EventBus.Publish(new BattleLogEvent($"{Actor.DisplayName} 发起攻击"));

        var outcome = DamageCalculator.DealDamage(Actor, _target, null, type);
        EventBus.Publish(new EntityDamagedEvent(
            _target, outcome.Damage, outcome.IsWeakness, outcome.IsCritical, outcome.BrokeShield, outcome.DamageType));

        if (outcome.BrokeShield)
            EventBus.Publish(new ShieldBrokenEvent(_target));

        yield return new WaitForSeconds(controller.Config.HitInterval);
    }
}
