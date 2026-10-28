using Game;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Equipment Item")]public class EquipmentItemSO : ItemDefinitionSO
{
    [Header("Equipment Config")]
    public EquipmentCategory category = EquipmentCategory.Weapon;
    public WeaponType weaponType = WeaponType.Sword;

    [Header("Stats Bonus")]
    public StatBlock statBonus = StatBlock.Zero;

    void OnValidate()
    {
        if (category != EquipmentCategory.Weapon)
        {
            weaponType = WeaponType.None;
        }
    }
}