public enum ActiveMap
{
    Player,
    UI,
    None
}

public enum CameraView
{
    Explore,
    PuzzleInspect
}

public enum PlayerJob
{
    Any,
    Warrior,
    Apothecary,
    Cleric,
    Dancer,
    Hunter,
    Merchant,
    Scholar,
    Thief
}

public enum GrowthRank
{
    S,
    A,
    B,
    C,
    D
}

public enum ItemType
{
   
   Equipment = 0,//装备
   Consumable = 1,//消耗品
}
public enum ItemIconKey
{
    Weapon,
    Armor,
    Accessory,
    Healing,
    SP,
    Revive,
    Cure,
    KeyItem,
}
public enum PanelType
{
    Item,
    Sell,
    Buy,
    Equipment
}
public enum EquipmentCategory
{
    Weapon,
    Shield,
    Head,
    Body,
    Accessory
}

public enum WeaponType
{
    None,
    Sword,
    Spear,
    Dagger,
    Axe,
    Bow,
    Staff,
}
public enum EquipSlot
{
    Sword,
    Spear,
    Dagger,
    Axe,
    Bow,
    Staff,
    Shield,
    Head,
    Body,
    Accessory1,
    Accessory2,
}

public enum StatType
{
    MaxHP = 0,
    MaxSP = 1,
    PAtk = 2,
    PDef = 3,
    MAtk = 4,
    MDef = 5,
    Speed = 6,
    Accuracy = 7,
    Evasion = 8,

    CurrentHP = 100,
    CurrentSP = 101,
}

public enum PuzzleConditionType
{
    HasClue,
    HasFlag,
    PuzzleSolved,
    PuzzleNotSolved,
    HasExamined,
    CasePhaseAtLeast,
}

public enum EvidenceCategory
{
    Physical,
    Document,
    Testimony,
    Audio,
    Other
}

public enum InvestigationPhase
{
    FreeInvestigation = 0,
    Interrogation = 1,
    Deduction = 2,
    Resolved = 3
}

public enum FadeStyle
{
    PanelFade,
    WipeFade
}
