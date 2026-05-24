using System;
using UnityEngine;

[Serializable]
public class EnemyAdaptationRuntimeConfig
{
    [Header("General")]
    public bool enabled = true;

    [Header("Level Target")]
    public int sourceCompletedLevel = 0;
    public int targetLevel = 0;

    [Header("Attack Damage Type")]
    public bool overrideMediumAttackDamageType;
    public DamageType mediumAttackDamageType = DamageType.Physical;

    public bool overrideHeavyAttackDamageType;
    public DamageType heavyAttackDamageType = DamageType.Physical;

    [Header("Medium Damage Type Weights")]
    public float mediumPhysicalWeight;
    public float mediumFireWeight;
    public float mediumEarthWeight;
    public float mediumWindWeight;
    public float mediumLightningWeight;
    public float mediumIceWeight;

    [Header("Heavy Damage Type Weights")]
    public float heavyPhysicalWeight;
    public float heavyFireWeight;
    public float heavyEarthWeight;
    public float heavyWindWeight;
    public float heavyLightningWeight;
    public float heavyIceWeight;

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

    [Header("Effect Chances")]
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
            sourceCompletedLevel = 0,
            targetLevel = 0,
            normalEnemyWeight = 0.7f,
            miniBossWeight = 0.25f,
            bossWeight = 0.05f
        };
    }

    public EnemyAdaptationRuntimeConfig CreateScaledCopy(float intensityMultiplier)
    {
        intensityMultiplier = Mathf.Clamp(intensityMultiplier, 0f, 2f);

        EnemyAdaptationRuntimeConfig copy = new EnemyAdaptationRuntimeConfig();

        copy.enabled = enabled;

        copy.sourceCompletedLevel = sourceCompletedLevel;
        copy.targetLevel = targetLevel;

        copy.overrideMediumAttackDamageType = overrideMediumAttackDamageType;
        copy.mediumAttackDamageType = mediumAttackDamageType;

        copy.overrideHeavyAttackDamageType = overrideHeavyAttackDamageType;
        copy.heavyAttackDamageType = heavyAttackDamageType;

        copy.mediumPhysicalWeight = mediumPhysicalWeight;
        copy.mediumFireWeight = mediumFireWeight;
        copy.mediumEarthWeight = mediumEarthWeight;
        copy.mediumWindWeight = mediumWindWeight;
        copy.mediumLightningWeight = mediumLightningWeight;
        copy.mediumIceWeight = mediumIceWeight;

        copy.heavyPhysicalWeight = heavyPhysicalWeight;
        copy.heavyFireWeight = heavyFireWeight;
        copy.heavyEarthWeight = heavyEarthWeight;
        copy.heavyWindWeight = heavyWindWeight;
        copy.heavyLightningWeight = heavyLightningWeight;
        copy.heavyIceWeight = heavyIceWeight;

        copy.strengthBonus = Mathf.RoundToInt(strengthBonus * intensityMultiplier);
        copy.constitutionBonus = Mathf.RoundToInt(constitutionBonus * intensityMultiplier);
        copy.dexterityBonus = Mathf.RoundToInt(dexterityBonus * intensityMultiplier);
        copy.intelligenceBonus = Mathf.RoundToInt(intelligenceBonus * intensityMultiplier);

        copy.maxHpBonus = Mathf.RoundToInt(maxHpBonus * intensityMultiplier);
        copy.armorBonus = Mathf.RoundToInt(armorBonus * intensityMultiplier);

        copy.physicalResistanceBonus = physicalResistanceBonus * intensityMultiplier;
        copy.fireResistanceBonus = fireResistanceBonus * intensityMultiplier;
        copy.earthResistanceBonus = earthResistanceBonus * intensityMultiplier;
        copy.windResistanceBonus = windResistanceBonus * intensityMultiplier;
        copy.lightningResistanceBonus = lightningResistanceBonus * intensityMultiplier;
        copy.iceResistanceBonus = iceResistanceBonus * intensityMultiplier;

        copy.mediumSlowChance = mediumSlowChance * intensityMultiplier;
        copy.mediumDotChance = mediumDotChance * intensityMultiplier;
        copy.mediumKnockChance = mediumKnockChance * intensityMultiplier;

        copy.heavySlowChance = heavySlowChance * intensityMultiplier;
        copy.heavyDotChance = heavyDotChance * intensityMultiplier;
        copy.heavyKnockChance = heavyKnockChance * intensityMultiplier;

        copy.normalEnemyWeight = normalEnemyWeight;
        copy.miniBossWeight = miniBossWeight;
        copy.bossWeight = bossWeight;

        copy.Clamp();
        return copy;
    }

    public void SetMediumDamageWeights(
        float physical,
        float fire,
        float earth,
        float wind,
        float lightning,
        float ice)
    {
        mediumPhysicalWeight = Mathf.Max(0f, physical);
        mediumFireWeight = Mathf.Max(0f, fire);
        mediumEarthWeight = Mathf.Max(0f, earth);
        mediumWindWeight = Mathf.Max(0f, wind);
        mediumLightningWeight = Mathf.Max(0f, lightning);
        mediumIceWeight = Mathf.Max(0f, ice);
    }

    public void SetHeavyDamageWeights(
        float physical,
        float fire,
        float earth,
        float wind,
        float lightning,
        float ice)
    {
        heavyPhysicalWeight = Mathf.Max(0f, physical);
        heavyFireWeight = Mathf.Max(0f, fire);
        heavyEarthWeight = Mathf.Max(0f, earth);
        heavyWindWeight = Mathf.Max(0f, wind);
        heavyLightningWeight = Mathf.Max(0f, lightning);
        heavyIceWeight = Mathf.Max(0f, ice);
    }

    public bool HasMediumDamageWeights()
    {
        return mediumPhysicalWeight +
               mediumFireWeight +
               mediumEarthWeight +
               mediumWindWeight +
               mediumLightningWeight +
               mediumIceWeight > 0.001f;
    }

    public bool HasHeavyDamageWeights()
    {
        return heavyPhysicalWeight +
               heavyFireWeight +
               heavyEarthWeight +
               heavyWindWeight +
               heavyLightningWeight +
               heavyIceWeight > 0.001f;
    }

    public DamageType RollMediumDamageType(DamageType fallback)
    {
        return RollDamageTypeFromWeights(
            fallback,
            DamageType.Physical,
            1f,
            mediumPhysicalWeight,
            mediumFireWeight,
            mediumEarthWeight,
            mediumWindWeight,
            mediumLightningWeight,
            mediumIceWeight
        );
    }

    public DamageType RollHeavyDamageType(
        DamageType fallback,
        DamageType discouragedType,
        float discouragedMultiplier = 0.25f)
    {
        return RollDamageTypeFromWeights(
            fallback,
            discouragedType,
            discouragedMultiplier,
            heavyPhysicalWeight,
            heavyFireWeight,
            heavyEarthWeight,
            heavyWindWeight,
            heavyLightningWeight,
            heavyIceWeight
        );
    }

    private DamageType RollDamageTypeFromWeights(
        DamageType fallback,
        DamageType discouragedType,
        float discouragedMultiplier,
        float physical,
        float fire,
        float earth,
        float wind,
        float lightning,
        float ice)
    {
        physical = Mathf.Max(0f, physical);
        fire = Mathf.Max(0f, fire);
        earth = Mathf.Max(0f, earth);
        wind = Mathf.Max(0f, wind);
        lightning = Mathf.Max(0f, lightning);
        ice = Mathf.Max(0f, ice);

        float multiplier = Mathf.Clamp01(discouragedMultiplier);

        switch (discouragedType)
        {
            case DamageType.Physical:
                physical *= multiplier;
                break;
            case DamageType.Fire:
                fire *= multiplier;
                break;
            case DamageType.Earth:
                earth *= multiplier;
                break;
            case DamageType.Wind:
                wind *= multiplier;
                break;
            case DamageType.Lightning:
                lightning *= multiplier;
                break;
            case DamageType.Ice:
                ice *= multiplier;
                break;
        }

        float total = physical + fire + earth + wind + lightning + ice;

        if (total <= 0.001f)
            return fallback;

        float roll = UnityEngine.Random.Range(0f, total);

        if ((roll -= physical) <= 0f)
            return DamageType.Physical;

        if ((roll -= fire) <= 0f)
            return DamageType.Fire;

        if ((roll -= earth) <= 0f)
            return DamageType.Earth;

        if ((roll -= wind) <= 0f)
            return DamageType.Wind;

        if ((roll -= lightning) <= 0f)
            return DamageType.Lightning;

        return DamageType.Ice;
    }

    public string BuildDamageWeightDebugText()
    {
        return
            $"MediumWeights=[Physical {mediumPhysicalWeight:0.##}, Fire {mediumFireWeight:0.##}, Earth {mediumEarthWeight:0.##}, Wind {mediumWindWeight:0.##}, Lightning {mediumLightningWeight:0.##}, Ice {mediumIceWeight:0.##}] | " +
            $"HeavyWeights=[Physical {heavyPhysicalWeight:0.##}, Fire {heavyFireWeight:0.##}, Earth {heavyEarthWeight:0.##}, Wind {heavyWindWeight:0.##}, Lightning {heavyLightningWeight:0.##}, Ice {heavyIceWeight:0.##}]";
    }

    public void ClearAllResistanceBonuses()
    {
        physicalResistanceBonus = 0f;
        fireResistanceBonus = 0f;
        earthResistanceBonus = 0f;
        windResistanceBonus = 0f;
        lightningResistanceBonus = 0f;
        iceResistanceBonus = 0f;
    }

    public float GetResistanceBonus(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return physicalResistanceBonus;
            case DamageType.Fire:
                return fireResistanceBonus;
            case DamageType.Earth:
                return earthResistanceBonus;
            case DamageType.Wind:
                return windResistanceBonus;
            case DamageType.Lightning:
                return lightningResistanceBonus;
            case DamageType.Ice:
                return iceResistanceBonus;
            default:
                return 0f;
        }
    }

    public void SetResistanceBonus(DamageType damageType, float value)
    {
        value = Mathf.Max(0f, value);

        switch (damageType)
        {
            case DamageType.Physical:
                physicalResistanceBonus = value;
                break;
            case DamageType.Fire:
                fireResistanceBonus = value;
                break;
            case DamageType.Earth:
                earthResistanceBonus = value;
                break;
            case DamageType.Wind:
                windResistanceBonus = value;
                break;
            case DamageType.Lightning:
                lightningResistanceBonus = value;
                break;
            case DamageType.Ice:
                iceResistanceBonus = value;
                break;
        }
    }

    public string BuildResistanceDebugText()
    {
        return
            $"Physical +{physicalResistanceBonus:0.#}%, " +
            $"Fire +{fireResistanceBonus:0.#}%, " +
            $"Earth +{earthResistanceBonus:0.#}%, " +
            $"Wind +{windResistanceBonus:0.#}%, " +
            $"Lightning +{lightningResistanceBonus:0.#}%, " +
            $"Ice +{iceResistanceBonus:0.#}%";
    }

    public void Clamp()
    {
        sourceCompletedLevel = Mathf.Max(0, sourceCompletedLevel);
        targetLevel = Mathf.Max(0, targetLevel);

        mediumPhysicalWeight = Mathf.Max(0f, mediumPhysicalWeight);
        mediumFireWeight = Mathf.Max(0f, mediumFireWeight);
        mediumEarthWeight = Mathf.Max(0f, mediumEarthWeight);
        mediumWindWeight = Mathf.Max(0f, mediumWindWeight);
        mediumLightningWeight = Mathf.Max(0f, mediumLightningWeight);
        mediumIceWeight = Mathf.Max(0f, mediumIceWeight);

        heavyPhysicalWeight = Mathf.Max(0f, heavyPhysicalWeight);
        heavyFireWeight = Mathf.Max(0f, heavyFireWeight);
        heavyEarthWeight = Mathf.Max(0f, heavyEarthWeight);
        heavyWindWeight = Mathf.Max(0f, heavyWindWeight);
        heavyLightningWeight = Mathf.Max(0f, heavyLightningWeight);
        heavyIceWeight = Mathf.Max(0f, heavyIceWeight);

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
