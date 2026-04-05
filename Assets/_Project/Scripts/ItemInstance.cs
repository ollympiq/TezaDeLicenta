using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    [SerializeField] private ItemDefinition definition;
    [SerializeField] private ItemRarity rarity;
    [SerializeField] private int stackCount = 1;

    public ItemInstance(ItemDefinition definition, int stackCount = 1)
    {
        this.definition = definition;
        this.rarity = definition != null ? definition.BaseRarity : ItemRarity.Common;
        this.stackCount = Mathf.Max(1, stackCount);
    }

    public ItemInstance(ItemDefinition definition, ItemRarity rarity, int stackCount = 1)
    {
        this.definition = definition;
        this.rarity = rarity;
        this.stackCount = Mathf.Max(1, stackCount);
    }

    public ItemDefinition Definition => definition;
    public ItemRarity Rarity => rarity;
    public int StackCount => stackCount;

    public bool IsValid => definition != null && stackCount > 0;

    public string DisplayName => definition != null ? definition.DisplayName : "Missing Item";
    public Sprite Icon => definition != null ? definition.Icon : null;
    public bool CanStack => definition != null && definition.Stackable;
    public int MaxStack => definition != null ? definition.MaxStack : 1;

    public WeaponDefinition WeaponDefinition => definition as WeaponDefinition;
    public ArmorDefinition ArmorDefinition => definition as ArmorDefinition;
    public PotionDefinition PotionDefinition => definition as PotionDefinition;

    public bool CanStackWith(ItemInstance other)
    {
        if (other == null || !IsValid || !other.IsValid)
            return false;

        return definition == other.definition &&
               rarity == other.rarity &&
               CanStack;
    }

    public void AddToStack(int amount)
    {
        stackCount = Mathf.Clamp(stackCount + amount, 1, MaxStack);
    }

    public void RemoveFromStack(int amount)
    {
        stackCount = Mathf.Max(0, stackCount - amount);
    }

    public ItemInstance Clone()
    {
        if (!IsValid)
            return null;

        return new ItemInstance(definition, rarity, stackCount);
    }
}