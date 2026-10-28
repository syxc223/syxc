using System.Collections;
using System.Collections.Generic;
using MFramework.Event;
using UnityEngine;

/// <summary>
/// 逃跑命令（对齐教程 090 EscapeCommandHandler）：
/// 清目标 → 全体撤回出生点演出 → 停顿 → 返回上一场景 → 停止战斗。
/// M1 逻辑闭环阶段没有战场单位视图，移动演出判空跳过，只保留节奏等待；M2 接入视图后自动播放撤回。
/// </summary>
public class EscapeCommandHandler : BattleCommandHandlerBase
{
    public EscapeCommandHandler(BattleEntity actor) : base(actor) { }

    public override IEnumerator Execute(BattleController controller)
    {
        controller.ClearTargetSelection();

        yield return RunEscape(controller);
        yield return new WaitForSeconds(controller.Config.EscapeExitDelay);

        if (BattleService.HasInstance)
            BattleService.Instance.ReturnToPreviousScene();

        controller.StopBattle(BattleResult.Escape);
    }

    /// <summary>我方单位撤回出生点（教程为 entity.Unit.MoveToPosition；无视图时退化为等待）。</summary>
    private IEnumerator RunEscape(BattleController controller)
    {
        List<BattleEntity> allies = controller.AliveAllies;
        foreach (var entity in allies)
        {
            var mover = entity.View as IBattleMover;
            if (mover != null)
                mover.MoveToInitialPosition(controller.Config.EscapeRunDuration);
        }
        yield return new WaitForSeconds(controller.Config.EscapeRunDuration);
    }
}

/// <summary>战场单位可移动接口：M2 的战斗单位视图实现它以支持冲刺/撤回等位移演出。</summary>
public interface IBattleMover
{
    void MoveToInitialPosition(float duration);
    void MoveToTargetPosition(Vector3 targetPosition, float duration);
}
