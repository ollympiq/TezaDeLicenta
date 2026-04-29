using System;
using System.Collections.Generic;
using UnityEngine;

public class LootGenerator : MonoBehaviour
{
    public static LootGenerator Instance { get; private set; }

    private enum LootCategory
    {
        Weapon,
        Armor,
        Potion,
        SkillBook
    }

    private struct WeightedCategory
    {
        public LootCategory Category;
        public float Weight;

        public WeightedCategory(LootCategory category, float weight)
        {
            Category = category;
            Weight = weight;
        }
    }

    [Serializable]
    private class WeaponFamilyWeight
    {
        public WeaponFamily family;
        public float weight = 1f;
    }

    [Serializable]
    private class ArmorSlotWeight
    {
        public EquipmentSlot slot;
        public float weight = 1f;
    }

    [Header("Pools")]
    [SerializeField] private List<WeaponDefinition> weaponPool = new List<WeaponDefinition>();
    [SerializeField] private List<ArmorDefinition> armorPool = new List<ArmorDefinition>();
    [SerializeField] private List<PotionDefinition> potionPool = new List<PotionDefinition>();
    [SerializeField] private List<SkillBookDefinition> skillBookPool = new List<SkillBookDefinition>();

    [Header("Tier Settings")]
    [SerializeField] private List<LootTierSettings> tierSettings = new List<LootTierSettings>();

    [Header("Weapon Family Weights")]
    [SerializeField]
    private List<WeaponFamilyWeight> weaponFamilyWeights = new List<WeaponFamilyWeight>
    {
        new WeaponFamilyWeight { family = WeaponFamily.Sword, weight = 1f },
        new WeaponFamilyWeight { family = WeaponFamily.Bow, weight = 1f },
        new WeaponFamilyWeight { family = WeaponFamily.Staff, weight = 1f }
    };

    [Header("Armor Slot Weights")]
    [SerializeField]
    private List<ArmorSlotWeight> armorSlotWeights = new List<ArmorSlotWeight>
    {
        new ArmorSlotWeight { slot = EquipmentSlot.Head, weight = 1f },
        new ArmorSlotWeight { slot = EquipmentSlot.Chest, weight = 1f },
        new ArmorSlotWeight { slot = EquipmentSlot.Hands, weight = 1f },
        new ArmorSlotWeight { slot = EquipmentSlot.Legs, weight = 1f },
        new ArmorSlotWeight { slot = EquipmentSlot.Feet, weight = 1f },
        new ArmorSlotWeight { slot = EquipmentSlot.Belt, weight = 0.8f },
        new ArmorSlotWeight { slot = EquipmentSlot.Ring, weight = 0.8f },
        new ArmorSlotWeight { slot = EquipmentSlot.Amulet, weight = 0.8f }
    };

    private readonly Dictionary<WeaponFamily, List<WeaponDefinition>> weaponsByFamily = new Dictionary<WeaponFamily, List<WeaponDefinition>>();
    private readonly Dictionary<EquipmentSlot, List<ArmorDefinition>> armorsBySlot = new Dictionary<EquipmentSlot, List<ArmorDefinition>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RebuildGroupedPools();
    }

    private void OnValidate()
    {
        RebuildGroupedPools();
    }

    public GeneratedLootResult GenerateLoot(EnemyLootTier tier, int itemLevel = 1)
    {
        GeneratedLootResult result = new GeneratedLootResult();
        LootTierSettings settings = GetSettingsForTier(tier);

        if (settings == null)
        {
            Debug.LogWarning("LootGenerator: lipsesc setarile pentru tier-ul " + tier);
            return result;
        }

        result.SetGold(UnityEngine.Random.Range(settings.MinGold, settings.MaxGold + 1));

        int itemCount = UnityEngine.Random.Range(settings.MinItems, settings.MaxItems + 1);
        itemLevel = Mathf.Max(1, itemLevel);

        for (int i = 0; i < itemCount; i++)
        {
            ItemDefinition definition = RollDefinition(settings);
            if (definition == null)
                continue;

            ItemInstance item = CreateGeneratedItem(definition, settings, itemLevel);
            if (item != null && item.IsValid)
                result.AddItem(item);
        }

        return result;
    }

    public List<ItemInstance> GenerateTraderItems(int itemCount, int itemLevel, EnemyLootTier tier)
    {
        List<ItemInstance> result = new List<ItemInstance>();

        LootTierSettings settings = GetSettingsForTier(tier);
        if (settings == null)
        {
            Debug.LogWarning("LootGenerator: lipsesc setarile pentru stock-ul traderului la tier-ul " + tier);
            return result;
        }

        itemCount = Mathf.Max(0, itemCount);
        itemLevel = Mathf.Max(1, itemLevel);

        for (int i = 0; i < itemCount; i++)
        {
            ItemDefinition definition = RollDefinition(settings);
            if (definition == null)
                continue;

            ItemInstance item = CreateGeneratedItem(definition, settings, itemLevel);
            if (item != null && item.IsValid)
                result.Add(item);
        }

        return result;
    }

    private void RebuildGroupedPools()
    {
        weaponsByFamily.Clear();
        armorsBySlot.Clear();

        for (int i = 0; i < weaponPool.Count; i++)
        {
            WeaponDefinition weapon = weaponPool[i];
            if (weapon == null)
                continue;

            if (!weaponsByFamily.TryGetValue(weapon.WeaponFamily, out List<WeaponDefinition> list))
            {
                list = new List<WeaponDefinition>();
                weaponsByFamily.Add(weapon.WeaponFamily, list);
            }

            list.Add(weapon);
        }

        for (int i = 0; i < armorPool.Count; i++)
        {
            ArmorDefinition armor = armorPool[i];
            if (armor == null)
                continue;

            EquipmentSlot slot = armor.EquipmentSlot;

            if (!armorsBySlot.TryGetValue(slot, out List<ArmorDefinition> list))
            {
                list = new List<ArmorDefinition>();
                armorsBySlot.Add(slot, list);
            }

            list.Add(armor);
        }
    }

    private LootTierSettings GetSettingsForTier(EnemyLootTier tier)
    {
        for (int i = 0; i < tierSettings.Count; i++)
        {
            if (tierSettings[i] != null && tierSettings[i].Tier == tier)
                return tierSettings[i];
        }

        return null;
    }

    private ItemDefinition RollDefinition(LootTierSettings settings)
    {
        List<WeightedCategory> availableCategories = new List<WeightedCategory>();

        if (weaponPool.Count > 0 && settings.WeaponWeight > 0f)
            availableCategories.Add(new WeightedCategory(LootCategory.Weapon, settings.WeaponWeight));

        if (armorPool.Count > 0 && settings.ArmorWeight > 0f)
            availableCategories.Add(new WeightedCategory(LootCategory.Armor, settings.ArmorWeight));

        if (potionPool.Count > 0 && settings.PotionWeight > 0f)
            availableCategories.Add(new WeightedCategory(LootCategory.Potion, settings.PotionWeight));

        if (skillBookPool.Count > 0 && settings.SkillBookWeight > 0f)
            availableCategories.Add(new WeightedCategory(LootCategory.SkillBook, settings.SkillBookWeight));

        if (availableCategories.Count == 0)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < availableCategories.Count; i++)
            totalWeight += availableCategories[i].Weight;

        float roll = UnityEngine.Random.value * totalWeight;

        for (int i = 0; i < availableCategories.Count; i++)
        {
            roll -= availableCategories[i].Weight;
            if (roll > 0f)
                continue;

            switch (availableCategories[i].Category)
            {
                case LootCategory.Weapon:
                    return RollWeaponDefinitionBalanced();

                case LootCategory.Armor:
                    return RollArmorDefinitionBalanced();

                case LootCategory.Potion:
                    return potionPool[UnityEngine.Random.Range(0, potionPool.Count)];

                case LootCategory.SkillBook:
                    return skillBookPool[UnityEngine.Random.Range(0, skillBookPool.Count)];
            }
        }

        return null;
    }

    private WeaponDefinition RollWeaponDefinitionBalanced()
    {
        WeaponFamily family = RollWeaponFamily();
        if (!weaponsByFamily.TryGetValue(family, out List<WeaponDefinition> familyPool) || familyPool == null || familyPool.Count == 0)
            return RollAnyWeaponFallback();

        return familyPool[UnityEngine.Random.Range(0, familyPool.Count)];
    }

    private ArmorDefinition RollArmorDefinitionBalanced()
    {
        EquipmentSlot slot = RollArmorSlot();
        if (!armorsBySlot.TryGetValue(slot, out List<ArmorDefinition> slotPool) || slotPool == null || slotPool.Count == 0)
            return RollAnyArmorFallback();

        return slotPool[UnityEngine.Random.Range(0, slotPool.Count)];
    }

    private WeaponFamily RollWeaponFamily()
    {
        float totalWeight = 0f;

        for (int i = 0; i < weaponFamilyWeights.Count; i++)
        {
            WeaponFamilyWeight entry = weaponFamilyWeights[i];
            if (entry == null || entry.weight <= 0f)
                continue;

            if (!weaponsByFamily.ContainsKey(entry.family))
                continue;

            if (weaponsByFamily[entry.family] == null || weaponsByFamily[entry.family].Count == 0)
                continue;

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return RollAnyWeaponFallback()?.WeaponFamily ?? WeaponFamily.Sword;

        float roll = UnityEngine.Random.value * totalWeight;

        for (int i = 0; i < weaponFamilyWeights.Count; i++)
        {
            WeaponFamilyWeight entry = weaponFamilyWeights[i];
            if (entry == null || entry.weight <= 0f)
                continue;

            if (!weaponsByFamily.ContainsKey(entry.family))
                continue;

            if (weaponsByFamily[entry.family] == null || weaponsByFamily[entry.family].Count == 0)
                continue;

            roll -= entry.weight;
            if (roll <= 0f)
                return entry.family;
        }

        return RollAnyWeaponFallback()?.WeaponFamily ?? WeaponFamily.Sword;
    }

    private EquipmentSlot RollArmorSlot()
    {
        float totalWeight = 0f;

        for (int i = 0; i < armorSlotWeights.Count; i++)
        {
            ArmorSlotWeight entry = armorSlotWeights[i];
            if (entry == null || entry.weight <= 0f)
                continue;

            if (!armorsBySlot.ContainsKey(entry.slot))
                continue;

            if (armorsBySlot[entry.slot] == null || armorsBySlot[entry.slot].Count == 0)
                continue;

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return RollAnyArmorFallback()?.EquipmentSlot ?? EquipmentSlot.Head;

        float roll = UnityEngine.Random.value * totalWeight;

        for (int i = 0; i < armorSlotWeights.Count; i++)
        {
            ArmorSlotWeight entry = armorSlotWeights[i];
            if (entry == null || entry.weight <= 0f)
                continue;

            if (!armorsBySlot.ContainsKey(entry.slot))
                continue;

            if (armorsBySlot[entry.slot] == null || armorsBySlot[entry.slot].Count == 0)
                continue;

            roll -= entry.weight;
            if (roll <= 0f)
                return entry.slot;
        }

        return RollAnyArmorFallback()?.EquipmentSlot ?? EquipmentSlot.Head;
    }

    private WeaponDefinition RollAnyWeaponFallback()
    {
        if (weaponPool == null || weaponPool.Count == 0)
            return null;

        return weaponPool[UnityEngine.Random.Range(0, weaponPool.Count)];
    }

    private ArmorDefinition RollAnyArmorFallback()
    {
        if (armorPool == null || armorPool.Count == 0)
            return null;

        return armorPool[UnityEngine.Random.Range(0, armorPool.Count)];
    }

    private ItemInstance CreateGeneratedItem(ItemDefinition definition, LootTierSettings settings, int itemLevel)
    {
        if (definition == null)
            return null;

        bool isEquipment =
            definition.Category == ItemCategory.Weapon ||
            definition.Category == ItemCategory.Armor;

        ItemRarity rarity = isEquipment ? RollEquipmentRarity(settings) : definition.BaseRarity;

        ItemInstance item = new ItemInstance(definition, rarity, 1);
        item.SetItemLevel(itemLevel);

        if (definition is WeaponDefinition weapon)
            RollWeaponStats(item, weapon, rarity, itemLevel);
        else if (definition is ArmorDefinition armor)
            RollArmorStats(item, armor, rarity, itemLevel);

        return item;
    }

    private ItemRarity RollEquipmentRarity(LootTierSettings settings)
    {
        float common = settings.CommonWeight;
        float uncommon = settings.UncommonWeight;
        float rare = settings.RareWeight;
        float epic = settings.EpicWeight;
        float legendary = settings.LegendaryWeight;
        float unique = settings.UniqueWeight;

        float total = common + uncommon + rare + epic + legendary + unique;
        if (total <= 0f)
            return ItemRarity.Common;

        float roll = UnityEngine.Random.value * total;

        if ((roll -= common) <= 0f) return ItemRarity.Common;
        if ((roll -= uncommon) <= 0f) return ItemRarity.Uncommon;
        if ((roll -= rare) <= 0f) return ItemRarity.Rare;
        if ((roll -= epic) <= 0f) return ItemRarity.Epic;
        if ((roll -= legendary) <= 0f) return ItemRarity.Legendary;
        return ItemRarity.Unique;
    }

    private void RollWeaponStats(ItemInstance item, WeaponDefinition weapon, ItemRarity rarity, int itemLevel)
    {
        float rarityMultiplier = GetRarityPowerMultiplier(rarity);
        float levelMultiplier = 1f + Mathf.Max(0, itemLevel - 1) * 0.12f;

        int rolledMin = Mathf.Max(1, Mathf.RoundToInt(weapon.MinDamage * rarityMultiplier * levelMultiplier));
        int rolledMax = Mathf.Max(rolledMin, Mathf.RoundToInt(weapon.MaxDamage * rarityMultiplier * levelMultiplier));

        item.SetRolledWeaponDamage(rolledMin, rolledMax);
        AddRandomModifiers(item, weapon, rarity, itemLevel);
    }

    private void RollArmorStats(ItemInstance item, ArmorDefinition armor, ItemRarity rarity, int itemLevel)
    {
        float rarityBonus = (GetRarityPowerMultiplier(rarity) - 1f) * 0.65f;
        float levelBonus = Mathf.Max(0, itemLevel - 1) * 0.08f;

        int extraArmor = Mathf.RoundToInt(armor.ArmorValue * (rarityBonus + levelBonus));
        if (extraArmor > 0)
            item.AddRolledBonus(ItemBonusType.Armor, extraArmor);

        AddRandomModifiers(item, armor, rarity, itemLevel);
    }

    private void AddRandomModifiers(ItemInstance item, ItemDefinition definition, ItemRarity rarity, int itemLevel)
    {
        int modifierCount = GetModifierCountForRarity(rarity);
        if (modifierCount <= 0)
            return;

        List<ItemBonusType> availableBonuses = GetAllowedBonuses(definition);
        if (availableBonuses.Count == 0)
            return;

        for (int i = 0; i < modifierCount && availableBonuses.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, availableBonuses.Count);
            ItemBonusType bonusType = availableBonuses[index];
            availableBonuses.RemoveAt(index);

            float value = RollModifierValue(bonusType, rarity, itemLevel);
            if (Mathf.Abs(value) > 0.001f)
                item.AddRolledBonus(bonusType, value);
        }
    }

    private List<ItemBonusType> GetAllowedBonuses(ItemDefinition definition)
    {
        List<ItemBonusType> result = new List<ItemBonusType>();

        WeaponDefinition weapon = definition as WeaponDefinition;
        if (weapon != null)
        {
            if (weapon.DamageType == DamageType.Physical)
            {
                result.Add(ItemBonusType.Strength);
                result.Add(ItemBonusType.Dexterity);
                result.Add(ItemBonusType.PhysicalPower);
                result.Add(ItemBonusType.Accuracy);
                result.Add(ItemBonusType.CritChance);
                result.Add(ItemBonusType.Initiative);
            }
            else
            {
                result.Add(ItemBonusType.Intelligence);
                result.Add(ItemBonusType.MagicPower);
                result.Add(ItemBonusType.Accuracy);
                result.Add(ItemBonusType.CritChance);
                result.Add(ItemBonusType.Initiative);
                result.Add(ItemBonusType.MaxAP);
            }

            return result;
        }

        ArmorDefinition armor = definition as ArmorDefinition;
        if (armor != null)
        {
            switch (armor.EquipmentSlot)
            {
                case EquipmentSlot.Ring:
                case EquipmentSlot.Amulet:
                case EquipmentSlot.Belt:
                    result.Add(ItemBonusType.Strength);
                    result.Add(ItemBonusType.Constitution);
                    result.Add(ItemBonusType.Dexterity);
                    result.Add(ItemBonusType.Intelligence);
                    result.Add(ItemBonusType.MaxHP);
                    result.Add(ItemBonusType.MaxAP);
                    result.Add(ItemBonusType.PhysicalPower);
                    result.Add(ItemBonusType.MagicPower);
                    result.Add(ItemBonusType.Accuracy);
                    result.Add(ItemBonusType.Evasion);
                    result.Add(ItemBonusType.CritChance);
                    result.Add(ItemBonusType.Initiative);
                    result.Add(ItemBonusType.PhysicalResistance);
                    result.Add(ItemBonusType.FireResistance);
                    result.Add(ItemBonusType.EarthResistance);
                    result.Add(ItemBonusType.WindResistance);
                    result.Add(ItemBonusType.LightningResistance);
                    result.Add(ItemBonusType.IceResistance);
                    break;

                default:
                    result.Add(ItemBonusType.Constitution);
                    result.Add(ItemBonusType.MaxHP);
                    result.Add(ItemBonusType.Armor);
                    result.Add(ItemBonusType.Evasion);
                    result.Add(ItemBonusType.PhysicalResistance);
                    result.Add(ItemBonusType.FireResistance);
                    result.Add(ItemBonusType.EarthResistance);
                    result.Add(ItemBonusType.WindResistance);
                    result.Add(ItemBonusType.LightningResistance);
                    result.Add(ItemBonusType.IceResistance);
                    break;
            }
        }

        return result;
    }

    private float RollModifierValue(ItemBonusType bonusType, ItemRarity rarity, int itemLevel)
    {
        int rarityIndex = GetRarityIndex(rarity);
        float levelScalar = 1f + Mathf.Max(0, itemLevel - 1) * 0.15f;
        float resistScalar = 1f + Mathf.Max(0, itemLevel - 1) * 0.08f;

        switch (bonusType)
        {
            case ItemBonusType.Strength:
            case ItemBonusType.Constitution:
            case ItemBonusType.Dexterity:
            case ItemBonusType.Intelligence:
                return Mathf.Round(UnityEngine.Random.Range(1f + rarityIndex, 2f + rarityIndex * 2f) * levelScalar);

            case ItemBonusType.MaxHP:
                return Mathf.Round(UnityEngine.Random.Range(8f + rarityIndex * 4f, 14f + rarityIndex * 8f) * levelScalar);

            case ItemBonusType.MaxAP:
                return Mathf.Round(UnityEngine.Random.Range(1f, 1f + Mathf.CeilToInt((rarityIndex + 1) * 0.5f)));

            case ItemBonusType.PhysicalPower:
            case ItemBonusType.MagicPower:
                return Mathf.Round(UnityEngine.Random.Range(2f + rarityIndex, 4f + rarityIndex * 3f) * levelScalar);

            case ItemBonusType.Armor:
                return Mathf.Round(UnityEngine.Random.Range(1f + rarityIndex, 2f + rarityIndex * 2f) * levelScalar);

            case ItemBonusType.Accuracy:
            case ItemBonusType.Evasion:
            case ItemBonusType.CritChance:
            case ItemBonusType.Initiative:
                return Mathf.Round(UnityEngine.Random.Range(1f, 2f + rarityIndex * 1.5f) * levelScalar);

            case ItemBonusType.PhysicalResistance:
            case ItemBonusType.FireResistance:
            case ItemBonusType.EarthResistance:
            case ItemBonusType.WindResistance:
            case ItemBonusType.LightningResistance:
            case ItemBonusType.IceResistance:
                return Mathf.Round(UnityEngine.Random.Range(2f + rarityIndex, 4f + rarityIndex * 2f) * resistScalar);

            default:
                return 0f;
        }
    }

    private int GetModifierCountForRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return 1;
            case ItemRarity.Rare: return 2;
            case ItemRarity.Epic: return 3;
            case ItemRarity.Legendary: return 4;
            case ItemRarity.Unique: return 5;
            default: return 0;
        }
    }

    private int GetRarityIndex(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return 1;
            case ItemRarity.Rare: return 2;
            case ItemRarity.Epic: return 3;
            case ItemRarity.Legendary: return 4;
            case ItemRarity.Unique: return 5;
            default: return 0;
        }
    }

    private float GetRarityPowerMultiplier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return 1.08f;
            case ItemRarity.Rare: return 1.18f;
            case ItemRarity.Epic: return 1.32f;
            case ItemRarity.Legendary: return 1.50f;
            case ItemRarity.Unique: return 1.70f;
            default: return 1f;
        }
    }
}