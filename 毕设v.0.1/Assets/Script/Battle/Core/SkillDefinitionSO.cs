using UnityEngine;

/// <summary>技能效果类型</summary>
public enum SkillEffectType
{
    Damage = 0,    // 造成伤害
    Heal = 1,      // 治疗（回复量 = 施法者魔攻 × Power/100）
    RestoreSP = 2  // 恢复SP（数值 = Power）
}

/// <summary>
/// 战斗技能定义资产。创建：Project 窗口右键 → Create → Battle → Skill。
/// </summary>
[CreateAssetMenu(menuName = "Battle/Skill", fileName = "Skill_New")]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("基础信息")]
    public string SkillName;
    [TextArea] public string Description;
    public Sprite Icon;

    [Header("消耗与威力")]
    [Min(0)] public int SpCost = 5;
    [Tooltip("威力百分比，100 约等于一次普攻")]
    [Range(10, 500)] public int Power = 100;
    [Tooltip("攻击段数：多段技能每一段都能削一层弱点护盾")]
    [Min(1)] public int Hits = 1;

    [Header("属性判定")]
    [Tooltip("None = 无属性，打不出弱点")]
    public DamageType DamageType = DamageType.None;
    [Tooltip("勾选后用魔攻/魔防结算，否则用物攻/物防")]
    public bool IsMagical;

    [Header("目标")]
    public SkillTargetType TargetType = SkillTargetType.EnemySingle;

    [Header("效果")]
    public SkillEffectType EffectType = SkillEffectType.Damage;
}
