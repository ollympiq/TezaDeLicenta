using System.Collections.Generic;
using UnityEngine;

public abstract class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemId = "new_item";
    [SerializeField] private string displayName = "New Item";
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Economy")]
    [SerializeField] private ItemRarity baseRarity = ItemRarity.Common;
    [SerializeField] private int buyPrice = 10;
    [SerializeField] private int sellPrice = 5;

    [Header("Stacking")]
    [SerializeField] private bool stackable = false;
    [SerializeField] private int maxStack = 1;

    [Header("Bonuses")]
    [SerializeField] private List<ItemStatModifier> statModifiers = new List<ItemStatModifier>();

    public abstract ItemCategory Category { get; }
    public virtual EquipmentSlot EquipmentSlot => EquipmentSlot.None;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

    public ItemRarity BaseRarity => baseRarity;
    public int BuyPrice => Mathf.Max(0, buyPrice);
    public int SellPrice => Mathf.Max(0, sellPrice);

    public bool Stackable => stackable;
    public int MaxStack => stackable ? Mathf.Max(1, maxStack) : 1;

    public IReadOnlyList<ItemStatModifier> StatModifiers => statModifiers;

    protected virtual void OnValidate()
    {
        buyPrice = Mathf.Max(0, buyPrice);
        sellPrice = Mathf.Max(0, sellPrice);
        maxStack = Mathf.Max(1, maxStack);
    }
}