using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAdaptationConfig", menuName = "Game/AI/Enemy Adaptation Config")]
public class EnemyAdaptationConfig : ScriptableObject
{
    [Header("Prototype")]
    [SerializeField] private bool enabled = true;

    [Header("Medium Attack Damage Type")]
    [SerializeField] private bool overrideMediumAttackDamageType = false;
    [SerializeField] private DamageType mediumAttackDamageType = DamageType.Physical;

    [Header("Heavy Attack Damage Type")]
    [SerializeField] private bool overrideHeavyAttackDamageType = false;
    [SerializeField] private DamageType heavyAttackDamageType = DamageType.Physical;

    [Header("Runtime Primary Attribute Bonuses")]
    [SerializeField] private int strengthBonus = 0;
    [SerializeField] private int constitutionBonus = 0;
    [SerializeField] private int dexterityBonus = 0;
    [SerializeField] private int intelligenceBonus = 0;

    [Header("Runtime Base Bonuses")]
    [SerializeField] private int maxHpBonus = 0;
    [SerializeField] private int armorBonus = 0;

    [Header("Runtime Resistance Bonuses")]
    [SerializeField, Range(0f, 35f)] private float physicalResistanceBonus = 0f;
    [SerializeField, Range(0f, 35f)] private float fireResistanceBonus = 0f;
    [SerializeField, Range(0f, 35f)] private float earthResistanceBonus = 0f;
    [SerializeField, Range(0f, 35f)] private float windResistanceBonus = 0f;
    [SerializeField, Range(0f, 35f)] private float lightningResistanceBonus = 0f;
    [SerializeField, Range(0f, 35f)] private float iceResistanceBonus = 0f;

    [Header("Health")]
    [SerializeField] private bool refillHealthAfterApplying = true;

    public bool Enabled => enabled;

    public bool OverrideMediumAttackDamageType => overrideMediumAttackDamageType;
    public DamageType MediumAttackDamageType => mediumAttackDamageType;

    public bool OverrideHeavyAttackDamageType => overrideHeavyAttackDamageType;
    public DamageType HeavyAttackDamageType => heavyAttackDamageType;

    public int StrengthBonus => strengthBonus;
    public int ConstitutionBonus => constitutionBonus;
    public int DexterityBonus => dexterityBonus;
    public int IntelligenceBonus => intelligenceBonus;

    public int MaxHpBonus => maxHpBonus;
    public int ArmorBonus => armorBonus;

    public float PhysicalResistanceBonus => physicalResistanceBonus;
    public float FireResistanceBonus => fireResistanceBonus;
    public float EarthResistanceBonus => earthResistanceBonus;
    public float WindResistanceBonus => windResistanceBonus;
    public float LightningResistanceBonus => lightningResistanceBonus;
    public float IceResistanceBonus => iceResistanceBonus;

    public bool RefillHealthAfterApplying => refillHealthAfterApplying;
}