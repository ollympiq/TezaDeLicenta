using UnityEngine;

[CreateAssetMenu(fileName = "RuleBasedAdaptationGenerator", menuName = "Game/AI/Rule Based Adaptation Generator")]
public class RuleBasedAdaptationGenerator : ScriptableObject
{
    [Header("Resistance")]
    [SerializeField, Range(0f, 35f)] private float maxDominantResistanceBonus = 20f;
    [SerializeField, Range(0f, 35f)] private float maxSecondaryResistanceBonus = 8f;
    [SerializeField, Range(0f, 1f)] private float dominantDamageRatioThreshold = 0.45f;

    [Header("Performance Difficulty")]
    [SerializeField] private int easyMaxHpBonus = 25;
    [SerializeField] private int easyArmorBonus = 3;
    [SerializeField] private int easyPhysicalOrMagicBonus = 4;

    [SerializeField] private int hardMaxHpBonus = 0;
    [SerializeField] private int hardArmorBonus = 0;

    [Header("Playstyle")]
    [SerializeField] private float kitingDistanceThreshold = 7f;
    [SerializeField] private float closeCombatDistanceThreshold = 3f;

    [Header("Effect Chances")]
    [SerializeField, Range(0f, 1f)] private float maxMediumSlowChance = 0.30f;
    [SerializeField, Range(0f, 1f)] private float maxHeavySlowChance = 0.40f;

    [SerializeField, Range(0f, 1f)] private float maxMediumDotChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maxHeavyDotChance = 0.35f;

    [SerializeField, Range(0f, 1f)] private float maxMediumKnockChance = 0.10f;
    [SerializeField, Range(0f, 1f)] private float maxHeavyKnockChance = 0.20f;

    public EnemyAdaptationRuntimeConfig Generate(CombatTelemetryData data)
    {
        if (data == null)
            return EnemyAdaptationRuntimeConfig.Default();

        EnemyAdaptationRuntimeConfig config = new EnemyAdaptationRuntimeConfig();
        config.enabled = true;

        ApplyResistanceAdaptation(data, config);
        ApplyAttackTypeAdaptation(data, config);
        ApplyDifficultyAdaptation(data, config);
        ApplyEffectChanceAdaptation(data, config);
        ApplySpawnWeightPrototype(data, config);

        config.Clamp();
        return config;
    }

    private void ApplyResistanceAdaptation(CombatTelemetryData data, EnemyAdaptationRuntimeConfig config)
    {
        int totalDamage = data.TotalDamageDealt;
        if (totalDamage <= 0)
            return;

        DamageType dominantType = data.GetDominantDamageType();
        float dominantRatio = data.GetDamageRatio(dominantType);

        if (dominantRatio < dominantDamageRatioThreshold)
        {
            AddResistance(config, dominantType, maxSecondaryResistanceBonus * dominantRatio);
            return;
        }

        float bonus = Mathf.Lerp(
            maxSecondaryResistanceBonus,
            maxDominantResistanceBonus,
            Mathf.InverseLerp(dominantDamageRatioThreshold, 0.85f, dominantRatio)
        );

        AddResistance(config, dominantType, bonus);
    }

    private void ApplyAttackTypeAdaptation(CombatTelemetryData data, EnemyAdaptationRuntimeConfig config)
    {
        bool playerIsKiting = data.averageDistanceToEnemies >= kitingDistanceThreshold;
        bool playerIsCloseCombat = data.averageDistanceToEnemies <= closeCombatDistanceThreshold;

        if (playerIsKiting)
        {
            config.overrideMediumAttackDamageType = true;
            config.mediumAttackDamageType = DamageType.Ice;

            config.overrideHeavyAttackDamageType = true;
            config.heavyAttackDamageType = DamageType.Lightning;

            return;
        }

        if (playerIsCloseCombat)
        {
            config.overrideMediumAttackDamageType = true;
            config.mediumAttackDamageType = DamageType.Physical;

            config.overrideHeavyAttackDamageType = true;
            config.heavyAttackDamageType = DamageType.Earth;

            return;
        }

        DamageType dominantPlayerDamage = data.GetDominantDamageType();

        config.overrideMediumAttackDamageType = true;
        config.mediumAttackDamageType = PickCounterStyleDamageType(dominantPlayerDamage);

        config.overrideHeavyAttackDamageType = true;
        config.heavyAttackDamageType = PickHeavyDamageType(dominantPlayerDamage);
    }

    private void ApplyDifficultyAdaptation(CombatTelemetryData data, EnemyAdaptationRuntimeConfig config)
    {
        if (data.LooksTooHard())
        {
            config.maxHpBonus = hardMaxHpBonus;
            config.armorBonus = hardArmorBonus;
            return;
        }

        if (!data.LooksTooEasy())
            return;

        int level = Mathf.Max(1, data.completedLevel);

        config.maxHpBonus = easyMaxHpBonus + level * 5;
        config.armorBonus = easyArmorBonus;

        DamageType mediumType = config.overrideMediumAttackDamageType
            ? config.mediumAttackDamageType
            : DamageType.Physical;

        DamageType heavyType = config.overrideHeavyAttackDamageType
            ? config.heavyAttackDamageType
            : DamageType.Physical;

        bool usesElementalAttack =
            mediumType != DamageType.Physical ||
            heavyType != DamageType.Physical;

        if (usesElementalAttack)
            config.intelligenceBonus = easyPhysicalOrMagicBonus;
        else
            config.strengthBonus = easyPhysicalOrMagicBonus;
    }

    private void ApplyEffectChanceAdaptation(CombatTelemetryData data, EnemyAdaptationRuntimeConfig config)
    {
        bool playerIsKiting = data.averageDistanceToEnemies >= kitingDistanceThreshold;
        bool playerSurvivesTooWell = data.playerHpPercentAtEnd >= 0.65f;
        bool playerUsesManyPotions = data.potionsUsed >= 2;
        bool playerUsesManySkills = data.skillsUsed >= 4;

        if (playerIsKiting)
        {
            config.mediumSlowChance = maxMediumSlowChance;
            config.heavySlowChance = maxHeavySlowChance;
        }

        if (playerUsesManyPotions || playerSurvivesTooWell)
        {
            config.mediumDotChance = maxMediumDotChance;
            config.heavyDotChance = maxHeavyDotChance;
        }

        if (playerUsesManySkills && playerSurvivesTooWell)
        {
            config.mediumKnockChance = maxMediumKnockChance;
            config.heavyKnockChance = maxHeavyKnockChance;
        }

        if (data.LooksTooHard())
        {
            config.mediumSlowChance *= 0.35f;
            config.heavySlowChance *= 0.35f;

            config.mediumDotChance *= 0.35f;
            config.heavyDotChance *= 0.35f;

            config.mediumKnockChance = 0f;
            config.heavyKnockChance = 0f;
        }
    }

    private void ApplySpawnWeightPrototype(CombatTelemetryData data, EnemyAdaptationRuntimeConfig config)
    {
        config.normalEnemyWeight = 0.70f;
        config.miniBossWeight = 0.25f;
        config.bossWeight = 0.05f;

        if (data.LooksTooEasy())
        {
            config.normalEnemyWeight = 0.60f;
            config.miniBossWeight = 0.32f;
            config.bossWeight = 0.08f;
        }

        if (data.LooksTooHard())
        {
            config.normalEnemyWeight = 0.82f;
            config.miniBossWeight = 0.16f;
            config.bossWeight = 0.02f;
        }
    }

    private DamageType PickCounterStyleDamageType(DamageType dominantPlayerDamage)
    {
        switch (dominantPlayerDamage)
        {
            case DamageType.Fire:
                return DamageType.Ice;

            case DamageType.Ice:
                return DamageType.Fire;

            case DamageType.Earth:
                return DamageType.Wind;

            case DamageType.Wind:
                return DamageType.Lightning;

            case DamageType.Lightning:
                return DamageType.Earth;

            case DamageType.Physical:
            default:
                return DamageType.Fire;
        }
    }

    private DamageType PickHeavyDamageType(DamageType dominantPlayerDamage)
    {
        switch (dominantPlayerDamage)
        {
            case DamageType.Fire:
                return DamageType.Lightning;

            case DamageType.Ice:
                return DamageType.Earth;

            case DamageType.Earth:
                return DamageType.Fire;

            case DamageType.Wind:
                return DamageType.Ice;

            case DamageType.Lightning:
                return DamageType.Wind;

            case DamageType.Physical:
            default:
                return DamageType.Lightning;
        }
    }

    private void AddResistance(EnemyAdaptationRuntimeConfig config, DamageType type, float value)
    {
        switch (type)
        {
            case DamageType.Physical:
                config.physicalResistanceBonus += value;
                break;

            case DamageType.Fire:
                config.fireResistanceBonus += value;
                break;

            case DamageType.Earth:
                config.earthResistanceBonus += value;
                break;

            case DamageType.Wind:
                config.windResistanceBonus += value;
                break;

            case DamageType.Lightning:
                config.lightningResistanceBonus += value;
                break;

            case DamageType.Ice:
                config.iceResistanceBonus += value;
                break;
        }
    }
}