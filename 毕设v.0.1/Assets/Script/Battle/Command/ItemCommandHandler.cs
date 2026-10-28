using System.Collections;
using Game;
using MFramework.Event;
using UnityEngine;

/// <summary>
/// 道具命令：对单体使用消耗品。道具的数量扣减在玩家确认时由 BattleController 完成，
/// 这里只负责把效果作用到目标，符合“计算/效果与输入分离”。
/// </summary>
public class ItemCommandHandler : BattleCommandHandlerBase
{
    private readonly ConsumableItemSO _item;
    private readonly BattleEntity _target;

    public ItemCommandHandler(BattleEntity actor, ConsumableItemSO item, BattleEntity target) : base(actor)
    {
        _item = item;
        _target = target;
    }

    public override IEnumerator Execute(BattleController controller)
    {
        int amount = _item.restoreAmount;

        switch (_item.itemIconKey)
        {
            case ItemIconKey.SP: // 回技力
                if (_target.IsAlly)
                {
                    _target.Ally.ModifySP(amount);
                    EventBus.Publish(new BattleLogEvent($"{_target.DisplayName} 使用 {_item.ItemName}，回复 {amount} 点技力"));
                }
                break;

            case ItemIconKey.Revive: // 复活：阵亡目标以 1 血复活
                if (_target.IsAlly && !_target.IsAlive)
                {
                    _target.CurrentHP = 1;
                    EventBus.Publish(new EntityHealedEvent(_target, 1));
                    EventBus.Publish(new BattleLogEvent($"{_target.DisplayName} 被 {_item.ItemName} 复活"));
                }
                break;

            default: // Healing / Cure / 其它：按回复生命处理
            {
                int before = _target.CurrentHP;
                _target.CurrentHP = Mathf.Min(_target.Stats.MaxHP, before + amount);
                int healed = _target.CurrentHP - before;
                EventBus.Publish(new EntityHealedEvent(_target, healed));
                EventBus.Publish(new BattleLogEvent($"{_target.DisplayName} 使用 {_item.ItemName}，回复 {healed} 点生命"));
                break;
            }
        }

        yield return new WaitForSeconds(controller.Config.HitInterval);
    }
}
