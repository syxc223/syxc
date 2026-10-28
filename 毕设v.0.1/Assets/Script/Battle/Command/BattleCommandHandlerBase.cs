using System.Collections;

/// <summary>
/// 战斗命令基类（教程 068 命令模式）。
/// 每条玩家/AI 指令都是一个独立对象，Execute 里以协程方式演出并通过计算引擎结算；
/// 控制器只负责“何时执行哪条命令”，不关心命令内部细节，便于扩展 Boost/多段/蓄力等。
/// </summary>
public abstract class BattleCommandHandlerBase
{
    protected readonly BattleEntity Actor;
    protected BattleCommandHandlerBase(BattleEntity actor) => Actor = actor;

    /// <summary>在 BattleController 的协程驱动下执行该命令。</summary>
    public abstract IEnumerator Execute(BattleController controller);
}
