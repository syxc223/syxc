// 战斗系统基础枚举。
// 说明：DamageType / SkillTargetType / BattleResult 的取值被现有 SO 资产引用，数值不可改动，只能新增。

/// <summary>伤害属性：前 6 个为武器类型，后 6 个为元素属性（弱点判定用）。</summary>
public enum DamageType
{
    None = 0,
    Sword = 1,      // 剑
    Spear = 2,      // 枪
    Dagger = 3,     // 短剑
    Axe = 4,        // 斧
    Bow = 5,        // 弓
    Staff = 6,      // 权杖
    Fire = 7,       // 火
    Ice = 8,        // 冰
    Lightning = 9,  // 雷
    Wind = 10,      // 风
    Light = 11,     // 光
    Dark = 12       // 暗
}

/// <summary>技能作用的目标范围</summary>
public enum SkillTargetType
{
    EnemySingle = 0, // 敌方单体
    EnemyAll = 1,     // 敌方全体
    AllySingle = 2,   // 我方单体
    Self = 3          // 自身
}

/// <summary>战斗结束的结局</summary>
public enum BattleResult
{
    None = 0,
    Victory = 1,
    Defeat = 2,
    Escape = 3
}

/// <summary>战斗阵营</summary>
public enum BattleSide
{
    Ally = 0,  // 我方
    Enemy = 1  // 敌方
}

/// <summary>
/// 战斗有限状态机的状态（对齐教程 053 初始化 / 057 状态切换）。
/// 战斗流程就是在这些状态之间单向/循环迁移，替代旧版“一个大协程 + 一堆 bool”。
/// </summary>
public enum BattleState
{
    None = 0,        // 未开始
    Intro = 1,       // 进场/站位/入场演出
    TurnStart = 2,   // 取出下一个行动者
    AwaitCommand = 3,// 等待我方玩家下达指令
    SelectTarget = 4,// 选择指令目标
    Execute = 5,     // 执行指令（攻击/技能/道具/防御/逃跑）
    TurnEnd = 6,     // 单个行动者结算完毕
    Settlement = 7,  // 整场战斗结算（经验/掉落）
    Ended = 8        // 战斗结束、退场
}

/// <summary>玩家可下达的指令类型（对齐教程 068 命令分发器）</summary>
public enum CommandType
{
    Attack = 0, // 普攻
    Skill = 1,  // 技能
    Item = 2,   // 道具
    Guard = 3,  // 防御
    Flee = 4    // 逃跑
}

/// <summary>敌方阵型（教程 051 站位布局，M1 先用标准阵型，M2 做前后排）</summary>
public enum BattleFormation
{
    Standard = 0, // 标准一字排开
    FrontBack = 1 // 前后两排
}
