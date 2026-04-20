using UnityEngine;

public static class UIRichTextColors
{
    public const string White = "#FFFFFF";

    public const string HP = "#E74C3C";
    public const string AP = "#56CCF2";

    public const string Strength = "#E67E22";
    public const string Constitution = "#FF6B6B";
    public const string Dexterity = "#58D68D";
    public const string Intelligence = "#BB6BD9";

    public const string Physical = "#C8C8C8";
    public const string PhysicalPower = "#D6D6D6";
    public const string MagicPower = "#C084FC";
    public const string ElementalBonus = "#C084FC";

    public const string Crit = "#F4D03F";
    public const string Initiative = "#F5B041";
    public const string Accuracy = "#82E0AA";
    public const string Evasion = "#76D7C4";
    public const string Level = "#F7DC6F";

    public const string Fire = "#F2994A";
    public const string Earth = "#8BC34A";
    public const string Wind = "#6FCF97";
    public const string Lightning = "#F2C94C";
    public const string Ice = "#7FDBFF";
    public const string Armor = "#8ED1FF";

    public const string Common = "#9E9E9E";
    public const string Uncommon = "#6FCF97";
    public const string Rare = "#4EA3FF";
    public const string Epic = "#BB6BD9";
    public const string Legendary = "#F2994A";

    public static string Paint(string text, string hex)
    {
        return $"<color={hex}>{text}</color>";
    }

    // coloreaza si label-ul, si valoarea
    public static string Line(string label, string value, string hex)
    {
        return $"{Paint(label, hex)}: {Paint(value, hex)}";
    }

    // pentru cazurile in care vrei label si value in culori diferite
    public static string DualLine(string label, string value, string labelHex, string valueHex)
    {
        return $"{Paint(label, labelHex)}: {Paint(value, valueHex)}";
    }

    public static string RarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Common;
            case ItemRarity.Uncommon: return Uncommon;
            case ItemRarity.Rare: return Rare;
            case ItemRarity.Epic: return Epic;
            case ItemRarity.Legendary:
            case ItemRarity.Unique: return Legendary;
            default: return White;
        }
    }

    public static string DamageTypeColor(DamageType type)
    {
        switch (type)
        {
            case DamageType.Physical: return Physical;
            case DamageType.Fire: return Fire;
            case DamageType.Earth: return Earth;
            case DamageType.Wind: return Wind;
            case DamageType.Lightning: return Lightning;
            case DamageType.Ice: return Ice;
            default: return White;
        }
    }

    public static string CategoryColor(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon: return Physical;
            case ItemCategory.Armor: return Armor;
            case ItemCategory.Consumable: return AP;
            case ItemCategory.SkillBook: return Intelligence;
            default: return White;
        }
    }

    public static string ClassColor(CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Melee: return Strength;
            case CharacterClass.Ranger: return Dexterity;
            case CharacterClass.Mage: return Intelligence;
            default: return White;
        }
    }

    public static string BonusTypeColor(ItemBonusType bonusType)
    {
        switch (bonusType)
        {
            case ItemBonusType.Strength:
                return Strength;

            case ItemBonusType.Constitution:
            case ItemBonusType.MaxHP:
                return Constitution;

            case ItemBonusType.Dexterity:
                return Dexterity;

            case ItemBonusType.Intelligence:
                return Intelligence;

            case ItemBonusType.MaxAP:
                return AP;

            case ItemBonusType.PhysicalPower:
                return PhysicalPower;

            case ItemBonusType.MagicPower:
            case ItemBonusType.ElementalDamageBonusPercent:
                return MagicPower;

            case ItemBonusType.Armor:
                return Armor;

            case ItemBonusType.Accuracy:
                return Accuracy;

            case ItemBonusType.Evasion:
                return Evasion;

            case ItemBonusType.CritChance:
                return Crit;

            case ItemBonusType.Initiative:
                return Initiative;

            case ItemBonusType.PhysicalResistance:
                return Physical;

            case ItemBonusType.FireResistance:
                return Fire;

            case ItemBonusType.EarthResistance:
                return Earth;

            case ItemBonusType.WindResistance:
                return Wind;

            case ItemBonusType.LightningResistance:
                return Lightning;

            case ItemBonusType.IceResistance:
                return Ice;

            default:
                return White;
        }
    }
}