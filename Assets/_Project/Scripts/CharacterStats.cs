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

    [Header("Primary Attributes")]
    [SerializeField] private int strength = 10;
    [SerializeField] private int constitution = 10;
    [SerializeField] private int dexterity = 10;
    [SerializeField] private int intelligence = 10;

    [Header("Derived Tuning")]
    [SerializeField] private int baseMaxHP = 50;
    [SerializeField] private int hpPerConstitution = 10;

    [SerializeField] private int baseMaxAP = 6;

    [SerializeField] private int physicalPowerPerStrength = 5;
    [SerializeField] private int magicPowerPerIntelligence = 5;

    [SerializeField] private float baseCritChance = 5f;
    [SerializeField] private float critChancePerDexterity = 0.5f;

    [SerializeField] private int baseInitiative = 10;
    [SerializeField] private int initiativePerDexterity = 1;

    [SerializeField] private float baseAccuracy = 80f;
    [SerializeField] private float accuracyPerDexterity = 1f;

    [SerializeField] private float baseEvasion = 0f;
    [SerializeField] private float evasionPerDexterity = 0.5f;

    [Header("Defense")]
    [SerializeField] private int armor = 10;

    [SerializeField, Range(0f, 100f)] private float physicalResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float fireResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float earthResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float windResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float lightningResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float iceResistance = 0f;

    public event Action OnStatsChanged;

    public CharacterClass Class => characterClass;
    public int Level => level;

    public int Strength => strength;
    public int Constitution => constitution;
    public int Dexterity => dexterity;
    public int Intelligence => intelligence;

    public int MaxHP => baseMaxHP + constitution * hpPerConstitution;
    public int MaxAP => baseMaxAP;

    public int PhysicalPower => strength * physicalPowerPerStrength;
    public int MagicPower => intelligence * magicPowerPerIntelligence;

    public float CritChance => Mathf.Clamp(baseCritChance + dexterity * critChancePerDexterity, 0f, 100f);
    public int Initiative => baseInitiative + dexterity * initiativePerDexterity;
    public float Accuracy => Mathf.Clamp(baseAccuracy + dexterity * accuracyPerDexterity, 0f, 100f);
    public float Evasion => Mathf.Clamp(baseEvasion + dexterity * evasionPerDexterity, 0f, 95f);

    public int Armor => armor;

    public float PhysicalResistance => physicalResistance;
    public float FireResistance => fireResistance;
    public float EarthResistance => earthResistance;
    public float WindResistance => windResistance;
    public float LightningResistance => lightningResistance;
    public float IceResistance => iceResistance;

    public float ArmorPhysicalReductionPercent => Mathf.Clamp(armor * 0.2f, 0f, 70f);

    private void Awake()
    {
        ClampValues();
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
        hpPerConstitution = 10;

        baseMaxAP = 6;

        physicalPowerPerStrength = 5;
        magicPowerPerIntelligence = 5;

        baseCritChance = 5f;
        critChancePerDexterity = 0.5f;

        baseInitiative = 10;
        initiativePerDexterity = 1;

        baseAccuracy = 80f;
        accuracyPerDexterity = 1f;

        baseEvasion = 0f;
        evasionPerDexterity = 0.5f;

        armor = 10;

        physicalResistance = 0f;
        fireResistance = 0f;
        earthResistance = 0f;
        windResistance = 0f;
        lightningResistance = 0f;
        iceResistance = 0f;

        NotifyStatsChanged();
    }

    private void ClampValues()
    {
        level = Mathf.Max(1, level);

        strength = Mathf.Max(1, strength);
        constitution = Mathf.Max(1, constitution);
        dexterity = Mathf.Max(1, dexterity);
        intelligence = Mathf.Max(1, intelligence);

        baseMaxHP = Mathf.Max(1, baseMaxHP);
        hpPerConstitution = Mathf.Max(0, hpPerConstitution);

        baseMaxAP = Mathf.Max(1, baseMaxAP);

        physicalPowerPerStrength = Mathf.Max(0, physicalPowerPerStrength);
        magicPowerPerIntelligence = Mathf.Max(0, magicPowerPerIntelligence);

        armor = Mathf.Max(0, armor);

        physicalResistance = Mathf.Clamp(physicalResistance, 0f, 100f);
        fireResistance = Mathf.Clamp(fireResistance, 0f, 100f);
        earthResistance = Mathf.Clamp(earthResistance, 0f, 100f);
        windResistance = Mathf.Clamp(windResistance, 0f, 100f);
        lightningResistance = Mathf.Clamp(lightningResistance, 0f, 100f);
        iceResistance = Mathf.Clamp(iceResistance, 0f, 100f);
    }

    public void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }

    public string GetStatsDisplayText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Class: {characterClass}");
        sb.AppendLine($"Strength: {strength}");
        sb.AppendLine($"Constitution: {constitution}");
        sb.AppendLine($"Dexterity: {dexterity}");
        sb.AppendLine($"Intelligence: {intelligence}");
        sb.AppendLine();
        sb.AppendLine($"Max HP: {MaxHP}");
        sb.AppendLine($"Max AP: {MaxAP}");
        sb.AppendLine($"Physical Power: {PhysicalPower}");
        sb.AppendLine($"Magic Power: {MagicPower}");
        sb.AppendLine($"Crit Chance: {CritChance:F1}%");
        sb.AppendLine($"Initiative: {Initiative}");
        sb.AppendLine($"Accuracy: {Accuracy:F1}%");
        sb.AppendLine($"Evasion: {Evasion:F1}%");
        sb.AppendLine();
        sb.AppendLine($"Armor: {armor}");
        sb.AppendLine($"Physical Resistance: {physicalResistance:F1}%");
        sb.AppendLine($"Fire Resistance: {fireResistance:F1}%");
        sb.AppendLine($"Earth Resistance: {earthResistance:F1}%");
        sb.AppendLine($"Wind Resistance: {windResistance:F1}%");
        sb.AppendLine($"Lightning Resistance: {lightningResistance:F1}%");
        sb.AppendLine($"Ice Resistance: {iceResistance:F1}%");

        return sb.ToString();
    }
}