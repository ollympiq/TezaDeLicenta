using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    [SerializeField] private ItemDefinition definition;
    [SerializeField] private ItemRarity rarity;
    [SerializeField] private int stackCount = 1;

    [Header("Generated Data")]
    [SerializeField] private int itemLevel = 1;
    [SerializeField] private int rolledMinDamage = -1;
    [SerializeField] private int rolledMaxDamage = -1;
    [SerializeField] private List<ItemRolledModifier> rolledModifiers = new List<ItemRolledModifier>();

    public ItemInstance(ItemDefinition definition, int stackCount = 1)
    {
        this.definition = definition;
        this.rarity = definition != null ? definition.BaseRarity : ItemRarity.Common;
        this.stackCount = Mathf.Max(1, stackCount);
        this.itemLevel = 1;
    }

    public ItemInstance(ItemDefinition definition, ItemRarity rarity, int stackCount = 1)
    {
        this.definition = definition;
        this.rarity = rarity;
        this.stackCount = Mathf.Max(1, stackCount);
        this.itemLevel = 1;
    }

    public ItemDefinition Definition => definition;
    public ItemRarity Rarity => rarity;
    public int StackCount => stackCount;
    public int ItemLevel => Mathf.Max(1, itemLevel);

    public bool IsValid => definition != null && stackCount > 0;

    public string DisplayName => definition != null ? definition.DisplayName : "Missing Item";
    public Sprite Icon => definition != null ? definition.Icon : null;
    public bool CanStack => definition != null && definition.Stackable;
    public int MaxStack => definition != null ? definition.MaxStack : 1;

    public IReadOnlyList<ItemRolledModifier> RolledModifiers => rolledModifiers;

    public WeaponDefinition WeaponDefinition => definition as WeaponDefinition;
    public ArmorDefinition ArmorDefinition => definition as ArmorDefinition;
    public PotionDefinition PotionDefinition => definition as PotionDefinition;
    public SkillBookDefinition SkillBookDefinition => definition as SkillBookDefinition;

    public bool HasRolledData =>
    HasValidRolledWeaponDamage() ||
    (rolledModifiers != null && rolledModifiers.Count > 0);

    private bool HasValidRolledWeaponDamage()
    {
        return rolledMinDamage > 0 && rolledMaxDamage >= rolledMinDamage;
    }

    public void SetItemLevel(int value)
    {
        itemLevel = Mathf.Max(1, value);
    }

    public void SetRolledWeaponDamage(int minDamage, int maxDamage)
    {
        rolledMinDamage = Mathf.Max(0, minDamage);
        rolledMaxDamage = Mathf.Max(rolledMinDamage, maxDamage);
    }

    public void ClearRolledWeaponDamage()
    {
        rolledMinDamage = -1;
        rolledMaxDamage = -1;
    }

    public int GetWeaponMinDamage()
    {
        WeaponDefinition weapon = WeaponDefinition;
        if (weapon == null)
            return 0;

        return HasValidRolledWeaponDamage() ? rolledMinDamage : weapon.MinDamage;
    }

    public int GetWeaponMaxDamage()
    {
        WeaponDefinition weapon = WeaponDefinition;
        if (weapon == null)
            return 0;

        if (HasValidRolledWeaponDamage())
            return rolledMaxDamage;

        return Mathf.Max(weapon.MinDamage, weapon.MaxDamage);
    }

    public int GetArmorValue()
    {
        int total = 0;

        if (ArmorDefinition != null)
            total += ArmorDefinition.ArmorValue;

        total += Mathf.RoundToInt(GetRolledBonus(ItemBonusType.Armor));
        return Mathf.Max(0, total);
    }

    public float GetRolledBonus(ItemBonusType bonusType)
    {
        if (rolledModifiers == null || rolledModifiers.Count == 0)
            return 0f;

        float total = 0f;
        for (int i = 0; i < rolledModifiers.Count; i++)
        {
            if (rolledModifiers[i] != null && rolledModifiers[i].BonusType == bonusType)
                total += rolledModifiers[i].Value;
        }

        return total;
    }

    public void SetRolledBonus(ItemBonusType bonusType, float value)
    {
        if (rolledModifiers == null)
            rolledModifiers = new List<ItemRolledModifier>();

        for (int i = 0; i < rolledModifiers.Count; i++)
        {
            if (rolledModifiers[i] != null && rolledModifiers[i].BonusType == bonusType)
            {
                rolledModifiers[i] = new ItemRolledModifier(bonusType, value);
                return;
            }
        }

        rolledModifiers.Add(new ItemRolledModifier(bonusType, value));
    }

    public void AddRolledBonus(ItemBonusType bonusType, float value)
    {
        float current = GetRolledBonus(bonusType);
        SetRolledBonus(bonusType, current + value);
    }

    public void ClearRolledBonuses()
    {
        if (rolledModifiers == null)
            rolledModifiers = new List<ItemRolledModifier>();
        else
            rolledModifiers.Clear();
    }

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

        ItemInstance clone = new ItemInstance(definition, rarity, stackCount);
        clone.itemLevel = itemLevel;
        clone.rolledMinDamage = rolledMinDamage;
        clone.rolledMaxDamage = rolledMaxDamage;

        if (rolledModifiers != null)
        {
            clone.rolledModifiers = new List<ItemRolledModifier>(rolledModifiers.Count);
            for (int i = 0; i < rolledModifiers.Count; i++)
            {
                ItemRolledModifier mod = rolledModifiers[i];
                if (mod != null)
                    clone.rolledModifiers.Add(new ItemRolledModifier(mod.BonusType, mod.Value));
            }
        }

        return clone;
    }
}