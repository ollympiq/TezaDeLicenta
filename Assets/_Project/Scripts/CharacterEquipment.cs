using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private WeaponDefinition startingWeapon;

    [Header("Equipped Items")]
    [SerializeField] private ItemInstance weaponItem;
    [SerializeField] private ItemInstance headItem;
    [SerializeField] private ItemInstance chestItem;
    [SerializeField] private ItemInstance handsItem;
    [SerializeField] private ItemInstance legsItem;
    [SerializeField] private ItemInstance feetItem;
    [SerializeField] private ItemInstance ringItem;
    [SerializeField] private ItemInstance amuletItem;

    public event Action OnEquipmentChanged;

    public WeaponDefinition EquippedWeaponDefinition => weaponItem != null ? weaponItem.WeaponDefinition : null;

    private void Awake()
    {
        if (weaponItem == null && startingWeapon != null)
            weaponItem = new ItemInstance(startingWeapon, 1);
    }

    public ItemInstance EquipItem(ItemInstance newItem)
    {
        if (newItem == null || newItem.Definition == null)
            return null;

        EquipmentSlot slot = newItem.Definition.EquipmentSlot;
        if (slot == EquipmentSlot.None)
            return null;

        ItemInstance previous = GetItemInSlot(slot);
        SetItemInSlot(slot, newItem);

        OnEquipmentChanged?.Invoke();
        return previous;
    }

    public ItemInstance EquipDefinition(ItemDefinition definition)
    {
        if (definition == null)
            return null;

        return EquipItem(new ItemInstance(definition, 1));
    }

    public ItemInstance Unequip(EquipmentSlot slot)
    {
        ItemInstance previous = GetItemInSlot(slot);
        SetItemInSlot(slot, null);

        if (previous != null)
            OnEquipmentChanged?.Invoke();

        return previous;
    }

    public ItemInstance GetItemInSlot(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return weaponItem;
            case EquipmentSlot.Head: return headItem;
            case EquipmentSlot.Chest: return chestItem;
            case EquipmentSlot.Hands: return handsItem;
            case EquipmentSlot.Legs: return legsItem;
            case EquipmentSlot.Feet: return feetItem;
            case EquipmentSlot.Ring: return ringItem;
            case EquipmentSlot.Amulet: return amuletItem;
            default: return null;
        }
    }

    public float GetFlatBonus(ItemBonusType bonusType)
    {
        float total = 0f;

        foreach (ItemInstance item in GetAllEquippedItems())
        {
            if (item?.Definition == null)
                continue;

            var modifiers = item.Definition.StatModifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].BonusType == bonusType)
                    total += modifiers[i].Value;
            }
        }

        return total;
    }

    public int GetArmorValueBonus()
    {
        int total = 0;

        foreach (ItemInstance item in GetAllEquippedItems())
        {
            ArmorDefinition armor = item?.ArmorDefinition;
            if (armor != null)
                total += armor.ArmorValue;
        }

        return total;
    }

    public float GetResistanceBonus(DamageType damageType)
    {
        float total = 0f;

        foreach (ItemInstance item in GetAllEquippedItems())
        {
            ArmorDefinition armor = item?.ArmorDefinition;
            if (armor == null)
                continue;

            switch (damageType)
            {
                case DamageType.Physical: total += armor.PhysicalResistance; break;
                case DamageType.Fire: total += armor.FireResistance; break;
                case DamageType.Earth: total += armor.EarthResistance; break;
                case DamageType.Wind: total += armor.WindResistance; break;
                case DamageType.Lightning: total += armor.LightningResistance; break;
                case DamageType.Ice: total += armor.IceResistance; break;
            }
        }

        return total;
    }

    public IEnumerable<ItemInstance> GetAllEquippedItems()
    {
        if (weaponItem != null) yield return weaponItem;
        if (headItem != null) yield return headItem;
        if (chestItem != null) yield return chestItem;
        if (handsItem != null) yield return handsItem;
        if (legsItem != null) yield return legsItem;
        if (feetItem != null) yield return feetItem;
        if (ringItem != null) yield return ringItem;
        if (amuletItem != null) yield return amuletItem;
    }

    public void ForceRefresh()
    {
        OnEquipmentChanged?.Invoke();
    }

    private void SetItemInSlot(EquipmentSlot slot, ItemInstance item)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: weaponItem = item; break;
            case EquipmentSlot.Head: headItem = item; break;
            case EquipmentSlot.Chest: chestItem = item; break;
            case EquipmentSlot.Hands: handsItem = item; break;
            case EquipmentSlot.Legs: legsItem = item; break;
            case EquipmentSlot.Feet: feetItem = item; break;
            case EquipmentSlot.Ring: ringItem = item; break;
            case EquipmentSlot.Amulet: amuletItem = item; break;
        }
    }
}