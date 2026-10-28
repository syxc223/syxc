using UnityEngine;

public class ItemDefinitionSO : ScriptableObject
{
    public string ItemName;

    [TextArea] public string ItemDescription;

    public ItemType itemType;

    public ItemIconKey itemIconKey;

    public int BuyPrice;

    public int SellPrice => (int)(BuyPrice * 0.75f);

    public int MaxStack = 99;

    [Header("稀有度")]
    public int RarityWeight = 100; // 稀有度权重
}