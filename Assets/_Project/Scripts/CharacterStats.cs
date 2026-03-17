using System;
using System.Text;
using UnityEngine;

public enum CharacterClass
{
    Unassigned,
    Melee,
    Ranger,
    Mage
}

public class CharacterStats : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private CharacterClass characterClass = CharacterClass.Unassigned;
    [SerializeField] private int level = 1;

    [Header("Base Primary Attributes")]
    [SerializeField] private int strength = 10;
    [SerializeField] private int constitution = 10;
    [SerializeField] private int dexterity = 10;
    [SerializeField] private int intelligence = 10;

    [Header("Base Values")]
    [SerializeField] private int baseMaxHP = 50;
    [SerializeField] private int baseMaxAP = 6;
    [SerializeField] private int baseArmor = 5;

    [SerializeField, Range(0f, 100f)] private float basePhysicalResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float baseFireResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float baseEarthResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float baseWindResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float baseLightningResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float baseIceResistance = 0f;

    [Header("References")]
    [SerializeField] private CharacterEquipment equipment;

    [Header("Attribute Bonuses")]
    [SerializeField] private int physicalPowerPerStrength = 2;
    [SerializeField] private int magicPowerPerIntelligence = 2;

    [SerializeField] private int hpPerConstitution = 12;
    [SerializeField] private float armorPerStrength = 0.5f;
    [SerializeField] private float physicalResistancePerStrength = 0.30f;
    [SerializeField] private float allResistancePerConstitution = 0.25f;

    [SerializeField] private float accuracyPerDexterity = 0.8f;
    [SerializeField] private float evasionPerDexterity = 0.5f;
    [SerializeField] private float critChancePerDexterity = 0.2f;
    [SerializeField] private int initiativePerDexterity = 1;

    [SerializeField] private float elementalDamageBonusPerIntelligence = 0.5f;

    [Header("Class Passive Bonuses")]
    [SerializeField] private int meleeArmorBonus = 4;
    [SerializeField] private float meleePhysicalResistanceBonus = 5f;

    [SerializeField] private float rangerAccuracyBonus = 5f;
    [SerializeField] private float rangerEvasionBonus = 5f;
    [SerializeField] private float rangerCritChanceBonus = 4f;
    [SerializeField] private int rangerInitiativeBonus = 5;

    [SerializeField] private int mageMagicPowerBonus = 10;
    [SerializeField] private float mageElementalDamageBonus = 5f;

    public event Action OnStatsChanged;

    public CharacterClass Class => characterClass;
    public int Level => level;

    public int Strength => Mathf.Max(1, strength + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.Strength)));
    public int Constitution => Mathf.Max(1, constitution + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.Constitution)));
    public int Dexterity => Mathf.Max(1, dexterity + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.Dexterity)));
    public int Intelligence => Mathf.Max(1, intelligence + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.Intelligence)));

    public int MaxHP => baseMaxHP + Constitution * hpPerConstitution + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.MaxHP));
    public int MaxAP => Mathf.Max(1, baseMaxAP + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.MaxAP)));

    public int PhysicalPower => Strength * physicalPowerPerStrength + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.PhysicalPower));
    public int MagicPower => Intelligence * magicPowerPerIntelligence + GetClassMagicPowerBonus() + Mathf.RoundToInt(GetEquipmentBonus(ItemBonusType.MagicPower));

    public float CritChance => Mathf.Clamp(
        3f +
        Dexterity * critChancePerDexterity +
        GetClassCritChanceBonus() +
        GetEquipmentBonus(ItemBonusType.CritChance), 0f, 100f);

    public int Initiative => Mathf.RoundToInt(
        10 +
        Dexterity * initiativePerDexterity +
        GetClassInitiativeBonus() +
        GetEquipmentBonus(ItemBonusType.Initiative));

    public float Accuracy => Mathf.Clamp(
        80f +
        Dexterity * accuracyPerDexterity +
        GetClassAccuracyBonus() +
        GetEquipmentBonus(ItemBonusType.Accuracy), 0f, 100f);

    public float Evasion => Mathf.Clamp(
        0f +
        Dexterity * evasionPerDexterity +
        GetClassEvasionBonus() +
        GetEquipmentBonus(ItemBonusType.Evasion), 0f, 95f);

    public int Armor => Mathf.Max(0, Mathf.RoundToInt(
        baseArmor +
        Strength * armorPerStrength +
        GetClassArmorBonus() +
        GetEquipmentBonus(ItemBonusType.Armor) +
        GetArmorFromEquipment()));

    public float PhysicalResistance => ClampResistance(
        basePhysicalResistance +
        Strength * physicalResistancePerStrength +
        Constitution * allResistancePerConstitution +
        GetClassPhysicalResistanceBonus() +
        GetEquipmentBonus(ItemBonusType.PhysicalResistance) +
        GetResistanceFromEquipment(DamageType.Physical));

    public float FireResistance => ClampResistance(
        baseFireResistance +
        Constitution * allResistancePerConstitution +
        GetEquipmentBonus(ItemBonusType.FireResistance) +
        GetResistanceFromEquipment(DamageType.Fire));

    public float EarthResistance => ClampResistance(
        baseEarthResistance +
        Constitution * allResistancePerConstitution +
        GetEquipmentBonus(ItemBonusType.EarthResistance) +
        GetResistanceFromEquipment(DamageType.Earth));

    public float WindResistance => ClampResistance(
        baseWindResistance +
        Constitution * allResistancePerConstitution +
        GetEquipmentBonus(ItemBonusType.WindResistance) +
        GetResistanceFromEquipment(DamageType.Wind));

    public float LightningResistance => ClampResistance(
        baseLightningResistance +
        Constitution * allResistancePerConstitution +
        GetEquipmentBonus(ItemBonusType.LightningResistance) +
        GetResistanceFromEquipment(DamageType.Lightning));

    public float IceResistance => ClampResistance(
        baseIceResistance +
        Constitution * allResistancePerConstitution +
        GetEquipmentBonus(ItemBonusType.IceResistance) +
        GetResistanceFromEquipment(DamageType.Ice));

    public float ElementalDamageBonusPercent => Mathf.Max(
        0f,
        Intelligence * elementalDamageBonusPerIntelligence +
        GetClassElementalDamageBonus() +
        GetEquipmentBonus(ItemBonusType.ElementalDamageBonusPercent));

    public float ArmorPhysicalReductionPercent => Mathf.Clamp(Armor * 0.2f, 0f, 70f);

    private void Awake()
    {
        if (equipment == null)
            equipment = GetComponent<CharacterEquipment>();

        ClampValues();
    }

    private void OnEnable()
    {
        if (equipment == null)
            equipment = GetComponent<CharacterEquipment>();

        if (equipment != null)
            equipment.OnEquipmentChanged += HandleEquipmentChanged;
    }

    private void OnDisable()
    {
        if (equipment != null)
            equipment.OnEquipmentChanged -= HandleEquipmentChanged;
    }

    private void OnValidate()
    {
        ClampValues();
        NotifyStatsChanged();
    }

    [ContextMenu("Apply Level 1 Defaults")]
    public void ApplyLevel1Defaults()
    {
        characterClass = CharacterClass.Unassigned;
        level = 1;

        strength = 10;
        constitution = 10;
        dexterity = 10;
        intelligence = 10;

        baseMaxHP = 50;
        baseMaxAP = 6;
        baseArmor = 5;

        basePhysicalResistance = 0f;
        baseFireResistance = 0f;
        baseEarthResistance = 0f;
        baseWindResistance = 0f;
        baseLightningResistance = 0f;
        baseIceResistance = 0f;

        NotifyStatsChanged();
    }

    public float GetWeaponMasteryBonusPercent(WeaponFamily family)
    {
        switch (characterClass)
        {
            case CharacterClass.Melee:
                switch (family)
                {
                    case WeaponFamily.Sword:
                    case WeaponFamily.Axe:
                    case WeaponFamily.Spear:
                        return 10f;
                }
                break;

            case CharacterClass.Ranger:
                switch (family)
                {
                    case WeaponFamily.Bow:
                    case WeaponFamily.Crossbow:
                    case WeaponFamily.Dagger:
                        return 10f;
                }
                break;

            case CharacterClass.Mage:
                switch (family)
                {
                    case WeaponFamily.Staff:
                    case WeaponFamily.Wand:
                    case WeaponFamily.Spellblade:
                        return 10f;
                }
                break;
        }

        return 0f;
    }

    public string GetStatsDisplayText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Class: {characterClass}");
        sb.AppendLine($"Strength: {Strength}");
        sb.AppendLine($"Constitution: {Constitution}");
        sb.AppendLine($"Dexterity: {Dexterity}");
        sb.AppendLine($"Intelligence: {Intelligence}");
        sb.AppendLine();
        sb.AppendLine($"Max HP: {MaxHP}");
        sb.AppendLine($"Max AP: {MaxAP}");
        sb.AppendLine($"Physical Power: {PhysicalPower}");
        sb.AppendLine($"Magic Power: {MagicPower}");
        sb.AppendLine($"Elemental Bonus: {ElementalDamageBonusPercent:F1}%");
        sb.AppendLine($"Crit Chance: {CritChance:F1}%");
        sb.AppendLine($"Initiative: {Initiative}");
        sb.AppendLine($"Accuracy: {Accuracy:F1}%");
        sb.AppendLine($"Evasion: {Evasion:F1}%");
        sb.AppendLine();
        sb.AppendLine($"Armor: {Armor}");
        sb.AppendLine($"Physical Resistance: {PhysicalResistance:F1}%");
        sb.AppendLine($"Fire Resistance: {FireResistance:F1}%");
        sb.AppendLine($"Earth Resistance: {EarthResistance:F1}%");
        sb.AppendLine($"Wind Resistance: {WindResistance:F1}%");
        sb.AppendLine($"Lightning Resistance: {LightningResistance:F1}%");
        sb.AppendLine($"Ice Resistance: {IceResistance:F1}%");

        return sb.ToString();
    }

    private void HandleEquipmentChanged()
    {
        NotifyStatsChanged();
    }

    private float GetEquipmentBonus(ItemBonusType type)
    {
        return equipment != null ? equipment.GetFlatBonus(type) : 0f;
    }

    private int GetArmorFromEquipment()
    {
        return equipment != null ? equipment.GetArmorValueBonus() : 0;
    }

    private float GetResistanceFromEquipment(DamageType damageType)
    {
        return equipment != null ? equipment.GetResistanceBonus(damageType) : 0f;
    }

    private void ClampValues()
    {
        level = Mathf.Max(1, level);

        strength = Mathf.Max(1, strength);
        constitution = Mathf.Max(1, constitution);
        dexterity = Mathf.Max(1, dexterity);
        intelligence = Mathf.Max(1, intelligence);

        baseMaxHP = Mathf.Max(1, baseMaxHP);
        baseMaxAP = Mathf.Max(1, baseMaxAP);
        baseArmor = Mathf.Max(0, baseArmor);
    }

    private float ClampResistance(float value)
    {
        return Mathf.Clamp(value, 0f, 100f);
    }

    private int GetClassArmorBonus()
    {
        return characterClass == CharacterClass.Melee ? meleeArmorBonus : 0;
    }

    private float GetClassPhysicalResistanceBonus()
    {
        return characterClass == CharacterClass.Melee ? meleePhysicalResistanceBonus : 0f;
    }

    private float GetClassAccuracyBonus()
    {
        return characterClass == CharacterClass.Ranger ? rangerAccuracyBonus : 0f;
    }

    private float GetClassEvasionBonus()
    {
        return characterClass == CharacterClass.Ranger ? rangerEvasionBonus : 0f;
    }

    private float GetClassCritChanceBonus()
    {
        return characterClass == CharacterClass.Ranger ? rangerCritChanceBonus : 0f;
    }

    private int GetClassInitiativeBonus()
    {
        return characterClass == CharacterClass.Ranger ? rangerInitiativeBonus : 0;
    }

    private int GetClassMagicPowerBonus()
    {
        return characterClass == CharacterClass.Mage ? mageMagicPowerBonus : 0;
    }

    private float GetClassElementalDamageBonus()
    {
        return characterClass == CharacterClass.Mage ? mageElementalDamageBonus : 0f;
    }

    public void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}