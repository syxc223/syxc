using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "Character/Enemy")]
    public class EnemyDefinitionSO : CharacterDefinitionSO
    {
        [Header("战斗表现")]
        [Tooltip("战斗中显示的立绘")]
        public Sprite BattleSprite;

        [Header("破防设置")]
        [Tooltip("弱点护盾层数：被弱点属性打中一次减一层，归零后破防")]
        [Min(1)] public int MaxShield = 3;
        [Tooltip("弱点属性列表（剑/枪/短剑/斧/弓/杖 + 六种元素）")]
        public List<DamageType> Weaknesses = new();

        [Header("敌人技能（AI 每回合随机选择）")]
        public List<SkillDefinitionSO> Skills = new();

        [Header("奖励")]
        [Min(0)] public int ExpReward = 10;
    }
}
