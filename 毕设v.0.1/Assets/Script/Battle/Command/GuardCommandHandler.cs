using System.Collections;
using MFramework.Event;
using UnityEngine;

/// <summary>防御：本回合进入防御姿态，受到伤害减半（计算引擎读取 Guarding），下回合开始清除。</summary>
public class GuardCommandHandler : BattleCommandHandlerBase
{
    public GuardCommandHandler(BattleEntity actor) : base(actor) { }

    public override IEnumerator Execute(BattleController controller)
    {
        Actor.Guarding = true;
        EventBus.Publish(new BattleLogEvent($"{Actor.DisplayName} 摆出防御姿态"));
        yield return new WaitForSeconds(controller.Config.HitInterval);
    }
}
