using System;
using UnityEngine;

[Serializable]
public class SkillEffectData
{
    [SerializeField] private SkillEffectType effectType = SkillEffectType.HealInstant;
    [SerializeField] private int durationTurns = 0;

    [Header("Instant Heal")]
    [SerializeField] private int flatMinValue = 0;
    [SerializeField] private int flatMaxValue = 0;
    [SerializeField, Range(0f, 3f)] private float powerScaling = 0f;
    [SerializeField] private bool useMagicPower = true;

    [Header("Primary Attribute Buff")]
    [SerializeField] private int bonusStrength = 0;
    [SerializeField] private int bonusConstitution = 0;
    [SerializeField] private int bonusDexterity = 0;
    [SerializeField] private int bonusIntelligence = 0;

    [Header("Crit Buff")]
    [SerializeField, Range(-100f, 100f)] private float critChanceBonusPercent = 0f;

    [Header("Elemental Damage Buff")]
    [SerializeField, Range(-200f, 200f)] private float elementalDamageBonusPercent = 0f;
    [SerializeField] private bool affectAllElements = true;
    [SerializeField] private DamageType elementalDamageType = DamageType.Fire;

    [Header("Damage Over Time")]
    [SerializeField] private int dotMinDamage = 0;
    [SerializeField] private int dotMaxDamage = 0;
    [SerializeField, Range(0f, 3f)] private float dotPowerScaling = 0f;
    [SerializeField] private bool dotUsesMagicPower = true;
    [SerializeField] private DamageType dotDamageType = DamageType.Fire;

    [Header("Slow Movement")]
    [SerializeField, Min(1f)] private float movementCostMultiplier = 2f;

    public SkillEffectType EffectType => effectType;
    public int DurationTurns => Mathf.Max(0, durationTurns);

    public int FlatMinValue => flatMinValue;
    public int FlatMaxValue => Mathf.Max(flatMinValue, flatMaxValue);
    public float PowerScaling => Mathf.Max(0f, powerScaling);
    public bool UseMagicPower => useMagicPower;

    public int BonusStrength => bonusStrength;
    public int BonusConstitution => bonusConstitution;
    public int BonusDexterity => bonusDexterity;
    public int BonusIntelligence => bonusIntelligence;

    public float CritChanceBonusPercent => critChanceBonusPercent;

    public float ElementalDamageBonusPercent => elementalDamageBonusPercent;
    public bool AffectAllElements => affectAllElements;
    public DamageType ElementalDamageType => elementalDamageType;

    public int DotMinDamage => dotMinDamage;
    public int DotMaxDamage => Mathf.Max(dotMinDamage, dotMaxDamage);
    public float DotPowerScaling => Mathf.Max(0f, dotPowerScaling);
    public bool DotUsesMagicPower => dotUsesMagicPower;
    public DamageType DotDamageType => dotDamageType;

    public float MovementCostMultiplier => Mathf.Max(1f, movementCostMultiplier);

    public int RollFlatValue()
    {
        if (flatMaxValue < flatMinValue)
            return flatMinValue;

        return UnityEngine.Random.Range(flatMinValue, flatMaxValue + 1);
    }

    public void ClampValues()
    {
        durationTurns = Mathf.Max(0, durationTurns);

        if (flatMaxValue < flatMinValue)
            flatMaxValue = flatMinValue;

        if (dotMaxDamage < dotMinDamage)
            dotMaxDamage = dotMinDamage;

        movementCostMultiplier = Mathf.Max(1f, movementCostMultiplier);
    }
}