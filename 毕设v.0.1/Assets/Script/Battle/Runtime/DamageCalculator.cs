using Game;
using UnityEngine;

/// <summary>一次伤害结算的结果（供表现层播放飘字/特效）。</summary>
public struct DamageOutcome
{
    public int Damage;
    public bool IsCritical;
    public bool IsWeakness;
    public bool BrokeShield;
    public DamageType DamageType;
}

/// <summary>
/// 计算引擎（教程 069–071）：伤害 / 治疗 / 弱点破盾的纯计算。
/// 与表现层完全无关，可脱离场景单独验证数值；命令 Handler 调用它完成结算。
/// 由旧版 BattleRules 升级而来，统一面向 BattleEntity。
/// </summary>
public static class DamageCalculator
{
    public const float CriticalChance = 0.05f;       // 暴击率
    public const float CriticalMultiplier = 1.5f;   // 暴击倍率
    public const float BrokenDamageMultiplier = 1.5f; // 破防受伤倍率
    public const float GuardDamageMultiplier = 0.5f;  // 防御减伤倍率

    /// <summary>普攻的伤害属性 = 当前装备武器类型（没武器则无属性）。</summary>
    public static DamageType GetWeaponDamageType(CharacterRuntimeData member)
    {
        if (member == null || member.EquippedItems == null)
            return DamageType.None;

        foreach (var entry in member.EquippedItems)
        {
            if (entry.item == null || entry.item.category != EquipmentCategory.Weapon)
                continue;

            switch (entry.item.weaponType)
            {
                case WeaponType.Sword: return DamageType.Sword;
                case WeaponType.Spear: return DamageType.Spear;
                case WeaponType.Dagger: return DamageType.Dagger;
                case WeaponType.Axe: return DamageType.Axe;
                case WeaponType.Bow: return DamageType.Bow;
                case WeaponType.Staff: return DamageType.Staff;
            }
        }
        return DamageType.None;
    }

    /// <summary>该属性是否命中敌人弱点。</summary>
    public static bool IsWeakness(EnemyDefinitionSO enemy, DamageType type)
    {
        return enemy != null
            && type != DamageType.None
            && enemy.Weaknesses != null
            && enemy.Weaknesses.Contains(type);
    }

    /// <summary>
    /// 对目标造成一次伤害：计算数值 → 弱点削盾/破防 → 扣血，返回结算结果。
    /// skill 为 null 表示普攻（此时用 overrideType，通常是武器属性）。
    /// </summary>
    public static DamageOutcome DealDamage(BattleEntity attacker, BattleEntity target,
        SkillDefinitionSO skill, DamageType overrideType)
    {
        bool magical = skill != null && skill.IsMagical;
        int atk = magical ? attacker.Stats.MAtk : attacker.Stats.PAtk;
        int def = magical ? target.Stats.MDef : target.Stats.PDef;
        int power = skill != null ? skill.Power : 100;

        int raw = Mathf.Max(1, (atk - def) * power / 100);
        raw = Mathf.RoundToInt(raw * Random.Range(0.9f, 1.1f)); // ±10% 浮动

        bool isCritical = Random.value < CriticalChance;
        if (isCritical)
            raw = Mathf.RoundToInt(raw * CriticalMultiplier);

        if (target.IsBroken)
            raw = Mathf.RoundToInt(raw * BrokenDamageMultiplier);

        if (target.Guarding)
            raw = Mathf.RoundToInt(raw * GuardDamageMultiplier);

        raw = Mathf.Max(1, raw);

        DamageType damageType = skill != null ? skill.DamageType : overrideType;
        bool isWeakness = false;
        bool brokeShield = false;

        // 只有敌人才有弱点护盾
        if (!target.IsAlly)
        {
            isWeakness = !target.IsBroken && IsWeakness(target.Enemy.Definition, damageType);
            if (isWeakness)
            {
                var enemy = target.Enemy;
                enemy.Shield = Mathf.Max(0, enemy.Shield - 1);
                if (enemy.Shield == 0 && !enemy.IsBroken)
                {
                    enemy.IsBroken = true;
                    brokeShield = true;
                }
            }
        }

        target.CurrentHP = Mathf.Max(0, target.CurrentHP - raw);

        return new DamageOutcome
        {
            Damage = raw,
            IsCritical = isCritical,
            IsWeakness = isWeakness,
            BrokeShield = brokeShield,
            DamageType = damageType
        };
    }

    /// <summary>治疗量（基于施法者魔攻与技能威力）。</summary>
    public static int CalculateHeal(StatBlock caster, SkillDefinitionSO skill)
    {
        int power = skill != null ? skill.Power : 100;
        return Mathf.Max(1, caster.MAtk * power / 100);
    }

    /// <summary>对目标施加治疗并返回实际回复量（不超过上限）。</summary>
    public static int Heal(BattleEntity caster, BattleEntity target, SkillDefinitionSO skill)
    {
        int amount = CalculateHeal(caster.Stats, skill);
        int before = target.CurrentHP;
        target.CurrentHP = Mathf.Min(target.Stats.MaxHP, before + amount);
        return target.CurrentHP - before;
    }
}
