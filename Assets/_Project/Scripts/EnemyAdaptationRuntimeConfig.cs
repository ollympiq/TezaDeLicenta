using System;
using UnityEngine;

[Serializable]
public class EnemyAdaptationRuntimeConfig
{
    [Header("General")]
    public bool enabled = true;

    [Header("Attack Damage Type")]
    public bool overrideMediumAttackDamageType;
    public DamageType mediumAttackDamageType = DamageType.Physical;

    public bool overrideHeavyAttackDamageType;
    public DamageType heavyAttackDamageType = DamageType.Physical;

    [Header("Runtime Attribute Bonuses")]
    public int strengthBonus;
    public int constitutionBonus;
    public int dexterityBonus;
    public int intelligenceBonus;

    [Header("Runtime Base Bonuses")]
    public int maxHpBonus;
    public int armorBonus;

    [Header("Runtime Resistance Bonuses")]
    public float physicalResistanceBonus;
    public float fireResistanceBonus;
    public float earthResistanceBonus;
    public float windResistanceBonus;
    public float lightningResistanceBonus;
    public float iceResistanceBonus;

    [Header("Effect Chances - Future Step")]
    [Range(0f, 1f)] public float mediumSlowChance;
    [Range(0f, 1f)] public float mediumDotChance;
    [Range(0f, 1f)] public float mediumKnockChance;

    [Range(0f, 1f)] public float heavySlowChance;
    [Range(0f, 1f)] public float heavyDotChance;
    [Range(0f, 1f)] public float heavyKnockChance;

    [Header("Spawn Weights - Future Step")]
    [Range(0f, 1f)] public float normalEnemyWeight = 0.7f;
    [Range(0f, 1f)] public float miniBossWeight = 0.25f;
    [Range(0f, 1f)] public float bossWeight = 0.05f;

    public static EnemyAdaptationRuntimeConfig Default()
    {
        return new EnemyAdaptationRuntimeConfig
        {
            enabled = false,
            normalEnemyWeight = 0.7f,
            miniBossWeight = 0.25f,
            bossWeight = 0.05f
        };
    }

    public void Clamp()
    {
        strengthBonus = Mathf.Clamp(strengthBonus, 0, 30);
        constitutionBonus = Mathf.Clamp(constitutionBonus, 0, 30);
        dexterityBonus = Mathf.Clamp(dexterityBonus, 0, 30);
        intelligenceBonus = Mathf.Clamp(intelligenceBonus, 0, 30);

        maxHpBonus = Mathf.Clamp(maxHpBonus, 0, 300);
        armorBonus = Mathf.Clamp(armorBonus, 0, 50);

        physicalResistanceBonus = Mathf.Clamp(physicalResistanceBonus, 0f, 35f);
        fireResistanceBonus = Mathf.Clamp(fireResistanceBonus, 0f, 35f);
        earthResistanceBonus = Mathf.Clamp(earthResistanceBonus, 0f, 35f);
        windResistanceBonus = Mathf.Clamp(windResistanceBonus, 0f, 35f);
        lightningResistanceBonus = Mathf.Clamp(lightningResistanceBonus, 0f, 35f);
        iceResistanceBonus = Mathf.Clamp(iceResistanceBonus, 0f, 35f);

        mediumSlowChance = Mathf.Clamp01(mediumSlowChance);
        mediumDotChance = Mathf.Clamp01(mediumDotChance);
        mediumKnockChance = Mathf.Clamp01(mediumKnockChance);

        heavySlowChance = Mathf.Clamp01(heavySlowChance);
        heavyDotChance = Mathf.Clamp01(heavyDotChance);
        heavyKnockChance = Mathf.Clamp01(heavyKnockChance);

        normalEnemyWeight = Mathf.Clamp01(normalEnemyWeight);
        miniBossWeight = Mathf.Clamp01(miniBossWeight);
        bossWeight = Mathf.Clamp01(bossWeight);

        float total = normalEnemyWeight + miniBossWeight + bossWeight;
        if (total <= 0.001f)
        {
            normalEnemyWeight = 0.7f;
            miniBossWeight = 0.25f;
            bossWeight = 0.05f;
        }
        else
        {
            normalEnemyWeight /= total;
            miniBossWeight /= total;
            bossWeight /= total;
        }
    }
}