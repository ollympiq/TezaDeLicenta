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

    public WeaponDefinition EquippedWeaponDefinition
    {
        get
        {
            ItemInstance item = GetItemInSlot(EquipmentSlot.Weapon);
            return item != null ? item.WeaponDefinition : null;
        }
    }

    private void Awake()
    {
        NormalizeEquippedItems();

        if (weaponItem == null && startingWeapon != null)
            weaponItem = new ItemInstance(startingWeapon, 1);

        NormalizeEquippedItems();
    }

    private void OnValidate()
    {
        NormalizeEquippedItems();
    }

    public ItemInstance EquipItem(ItemInstance newItem)
    {
        if (newItem == null || !newItem.IsValid)
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
        ItemInstance raw = GetRawItemInSlot(slot);

        if (raw == null || !raw.IsValid)
            return null;

        return raw;
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
        if (GetItemInSlot(EquipmentSlot.Weapon) != null) yield return GetItemInSlot(EquipmentSlot.Weapon);
        if (GetItemInSlot(EquipmentSlot.Head) != null) yield return GetItemInSlot(EquipmentSlot.Head);
        if (GetItemInSlot(EquipmentSlot.Chest) != null) yield return GetItemInSlot(EquipmentSlot.Chest);
        if (GetItemInSlot(EquipmentSlot.Hands) != null) yield return GetItemInSlot(EquipmentSlot.Hands);
        if (GetItemInSlot(EquipmentSlot.Legs) != null) yield return GetItemInSlot(EquipmentSlot.Legs);
        if (GetItemInSlot(EquipmentSlot.Feet) != null) yield return GetItemInSlot(EquipmentSlot.Feet);
        if (GetItemInSlot(EquipmentSlot.Ring) != null) yield return GetItemInSlot(EquipmentSlot.Ring);
        if (GetItemInSlot(EquipmentSlot.Amulet) != null) yield return GetItemInSlot(EquipmentSlot.Amulet);
    }

    public void ForceRefresh()
    {
        NormalizeEquippedItems();
        OnEquipmentChanged?.Invoke();
    }

    private void NormalizeEquippedItems()
    {
        weaponItem = NormalizeItem(weaponItem);
        headItem = NormalizeItem(headItem);
        chestItem = NormalizeItem(chestItem);
        handsItem = NormalizeItem(handsItem);
        legsItem = NormalizeItem(legsItem);
        feetItem = NormalizeItem(feetItem);
        ringItem = NormalizeItem(ringItem);
        amuletItem = NormalizeItem(amuletItem);
    }

    private ItemInstance NormalizeItem(ItemInstance item)
    {
        if (item == null || !item.IsValid)
            return null;

        return item;
    }

    private ItemInstance GetRawItemInSlot(EquipmentSlot slot)
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

    private void SetItemInSlot(EquipmentSlot slot, ItemInstance item)
    {
        ItemInstance cleanItem = NormalizeItem(item);

        switch (slot)
        {
            case EquipmentSlot.Weapon: weaponItem = cleanItem; break;
            case EquipmentSlot.Head: headItem = cleanItem; break;
            case EquipmentSlot.Chest: chestItem = cleanItem; break;
            case EquipmentSlot.Hands: handsItem = cleanItem; break;
            case EquipmentSlot.Legs: legsItem = cleanItem; break;
            case EquipmentSlot.Feet: feetItem = cleanItem; break;
            case EquipmentSlot.Ring: ringItem = cleanItem; break;
            case EquipmentSlot.Amulet: amuletItem = cleanItem; break;
        }
    }
}