
using UnityEngine;

namespace Game
{
    public abstract class CharacterDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string ID;
    public string Name;
    public Sprite Portrait;

    [Header("Stats")]
    public int BaseLevel = 1;

    public StatBlock BaseStats;

    [Header("Animator")]
    public AnimatorOverrideController fieldAnimator;
    public AnimatorOverrideController battleAnimaor;


}

[System.Serializable]
public struct StatBlock
{
    public int MaxHP;     // 最大生命值.
    public int MaxSP;     // 最大法力值.
    public int PAtk;      // 物理攻击力.
    public int PDef;      // 物理防御力.
    public int MAtk;      // 属性攻击力.
    public int MDef;      // 属性防御力.
    public int Speed;     // 速度.
    public int Accuracy;  // 命中率.
    public int Evasion;   // 闪避率.


    public static StatBlock Zero = new();
    public static StatBlock operator +(StatBlock a, StatBlock b)
    {
        return new ()
        {
            MaxHP = a.MaxHP + b.MaxHP,
            MaxSP = a.MaxSP + b.MaxSP,
            PAtk = a.PAtk + b.PAtk,
            PDef = a.PDef + b.PDef,
            MAtk = a.MAtk + b.MAtk,
            MDef = a.MDef + b.MDef,
            Speed = a.Speed + b.Speed,
            Accuracy = a.Accuracy + b.Accuracy,
            Evasion = a.Evasion + b.Evasion
        };
    }
}
}