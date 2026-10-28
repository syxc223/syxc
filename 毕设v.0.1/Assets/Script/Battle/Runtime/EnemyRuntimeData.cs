using Game;

/// <summary>
/// 战斗中一名敌人的运行时状态：HP / 弱点护盾 / 破防 / AI 阶段。
/// 与静态定义 EnemyDefinitionSO 分离——定义只读，运行时状态可被战斗改写。
/// </summary>
public class EnemyRuntimeData
{
    /// <summary>静态定义（名字、立绘、属性、弱点、技能、奖励）</summary>
    public EnemyDefinitionSO Definition { get; }

    public int CurrentHP { get; set; }

    /// <summary>当前剩余护盾层数，被弱点属性命中一次减一层，归零破防</summary>
    public int Shield { get; set; }

    /// <summary>是否处于破防（眩晕）状态：跳过下一次行动，受伤加深，随后回满护盾</summary>
    public bool IsBroken { get; set; }

    public bool IsDead => CurrentHP <= 0;

    /// <summary>战斗属性（敌人不随等级成长，直接取定义里的 BaseStats）</summary>
    public StatBlock Stats => Definition != null ? Definition.BaseStats : StatBlock.Zero;

    /// <summary>当前 AI 阶段索引（教程 083 多阶段转换，M3 使用，先预留）</summary>
    public int AiPhaseIndex { get; set; }

    /// <summary>正在蓄力的技能（教程 084 蓄力，M3 使用，先预留）</summary>
    public SkillDefinitionSO ChargingSkill { get; set; }

    public EnemyRuntimeData(EnemyDefinitionSO definition)
    {
        Definition = definition;
        CurrentHP = definition.BaseStats.MaxHP;
        Shield = definition.MaxShield;
    }

    /// <summary>破防恢复：清除破防标记并把护盾回满（对齐教程：破防只跳过一回合）</summary>
    public void RecoverFromBreak()
    {
        IsBroken = false;
        Shield = Definition != null ? Definition.MaxShield : 0;
    }
}
