using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人行动选择器（教程 082–084）。
/// M1：在敌人配置的技能里随机挑一个可用技能，没有技能则返回 null（由控制器按普攻处理）。
/// M3 在此扩展：技能使用偏好权重、多阶段（EnemyRuntimeData.AiPhaseIndex）、蓄力（ChargingSkill）。
/// </summary>
public static class EnemyAISelector
{
    public static SkillDefinitionSO PickSkill(BattleEntity enemy)
    {
        if (enemy == null || enemy.IsAlly)
            return null;

        List<SkillDefinitionSO> skills = enemy.Enemy.Definition.Skills;
        if (skills == null || skills.Count == 0)
            return null;

        // M1：等概率随机；M3 按偏好权重与阶段筛选
        return skills[Random.Range(0, skills.Count)];
    }
}
