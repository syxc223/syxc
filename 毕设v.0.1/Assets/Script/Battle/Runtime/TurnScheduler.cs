using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 行动顺序调度器（教程 061 时间排序 / 072 敌我全员轮换）。
/// M1 采用“轮次模型”：每轮把所有存活者按速度从快到慢排一次，依次行动，走完一轮再重排，
/// 阵亡者自动跳过；M2 可在此基础上升级为连续时间轴（BattleEntity.NextActionTime 已预留）。
/// </summary>
public class TurnScheduler
{
    private readonly List<BattleEntity> _all;
    private readonly List<BattleEntity> _order = new();
    private int _cursor;

    public TurnScheduler(List<BattleEntity> all)
    {
        _all = all;
        Rebuild();
    }

    /// <summary>当前这一轮的行动顺序（供行动顺序 UI 显示）。</summary>
    public IReadOnlyList<BattleEntity> CurrentOrder => _order;

    /// <summary>按速度降序重建一轮顺序，快的先出手。</summary>
    public void Rebuild()
    {
        _order.Clear();
        foreach (var entity in _all)
        {
            if (entity.IsAlive)
                _order.Add(entity);
        }
        _order.Sort((a, b) => b.Stats.Speed.CompareTo(a.Stats.Speed));
        _cursor = 0;
    }

    /// <summary>取下一个行动者；一轮走完自动重建（敌我全员轮换），全部阵亡返回 null。</summary>
    public BattleEntity Next()
    {
        int guard = 0;
        while (guard++ < 1024)
        {
            if (_cursor >= _order.Count)
            {
                Rebuild();
                if (_order.Count == 0)
                    return null;
            }

            var entity = _order[_cursor++];
            if (entity.IsAlive)
                return entity;
        }
        return null;
    }
}
