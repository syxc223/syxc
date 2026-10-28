using UnityEngine;
using Game;

using System.Collections.Generic;
namespace Game
{
    [CreateAssetMenu(menuName = "Character/Ally")]
    public class AllyDefinitionSO : CharacterDefinitionSO
    {
    [Header("Ally Specific")]
    public PlayerJob Job;

    [Header("Growth Settings")]
    public GlobalGrowthConfigSO globalGrowthConfigSO;

    public GrowthProfile growthProfile; ////TODO: make this a list of GrowthProfile

    [Header("Equipment Capability")]
    public List<WeaponType> EquipableWeaponTyps = new();

    [Header("Initial Equipment")]
    public List<InitialEquipmentEntry> InitialEquipment = new();

    [Header("战斗技能（普攻之外可用的技能）")]
    public List<SkillDefinitionSO> Skills = new();

    
    public bool CanEquipWeaponType(WeaponType weaponType)
    {
    if (weaponType == WeaponType.None) return false;
    //
    return EquipableWeaponTyps.Contains(weaponType);
    }

    [System.Serializable]
    public struct InitialEquipmentEntry
   {
    public EquipSlot slot;
    public EquipmentItemSO item;
   }

    public StatBlock GetStatForLevel(int level)
{
    float hpMult = globalGrowthConfigSO.GetCurveByRank(growthProfile.HP).Evaluate(level);
    float spMult = globalGrowthConfigSO.GetCurveByRank(growthProfile.SP).Evaluate(level);
    float pAtkMult = globalGrowthConfigSO.GetCurveByRank(growthProfile.PAtk).Evaluate(level);
    float pDefMult = globalGrowthConfigSO.GetCurveByRank(growthProfile.PDef).Evaluate(level);
    float mAtkMult = globalGrowthConfigSO.GetCurveByRank(growthProfile.MAtk).Evaluate(level);
    float mDefMult = globalGrowthConfigSO.GetCurveByRank(growthProfile.MDef).Evaluate(level);
    float speedMult = globalGrowthConfigSO.GetCurveByRank(growthProfile.Speed).Evaluate(level);

    return new StatBlock
    {
        MaxHP = Mathf.RoundToInt(BaseStats.MaxHP * hpMult),
        MaxSP = Mathf.RoundToInt(BaseStats.MaxSP * spMult),
        PAtk = Mathf.RoundToInt(BaseStats.PAtk * pAtkMult),
        PDef = Mathf.RoundToInt(BaseStats.PDef * pDefMult),
        MAtk = Mathf.RoundToInt(BaseStats.MAtk * mAtkMult),
        MDef = Mathf.RoundToInt(BaseStats.MDef * mDefMult),
        Speed = Mathf.RoundToInt(BaseStats.Speed * speedMult),
        Accuracy = BaseStats.Accuracy,
        Evasion = BaseStats.Evasion,
    };
}

}

    [System.Serializable]
    
    public struct GrowthProfile
    {
        public GrowthRank HP;
        public GrowthRank SP;
        public GrowthRank PAtk;
        public GrowthRank PDef;
        public GrowthRank MAtk;
        public GrowthRank MDef;
        public GrowthRank Speed;
    }
}
