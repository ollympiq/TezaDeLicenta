using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RuleBasedAdaptationGenerator", menuName = "Game/AI/Rule Based Adaptation Generator")]
public class RuleBasedAdaptationGenerator : ScriptableObject
{
    [Header("Resistance")]
    [SerializeField, Range(0f, 35f)] private float maxDominantResistanceBonus = 10f;
    [SerializeField, Range(0f, 35f)] private float maxSecondaryResistanceBonus = 5f;
    [SerializeField, Range(0f, 10f)] private float minResistanceBonusWhenRelevant = 2f;
    [SerializeField, Range(0f, 1f)] private float minDamageRatioForResistance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float fullResistanceRatio = 0.65f;

    [Header("Performance Difficulty")]
    [SerializeField] private int easyMaxHpBonus = 25;
    [SerializeField] private int easyArmorBonus = 3;
    [SerializeField] private int easyPhysicalOrMagicBonus = 4;

    [SerializeField] private int hardMaxHpBonus = 0;
    [SerializeField] private int hardArmorBonus = 0;

    [Header("Playstyle")]
    [SerializeField] private float kitingDistanceThreshold = 7f;
    [SerializeField] private float closeCombatDistanceThreshold = 3f;

    [Header("Damage Type Adaptation")]
    [SerializeField, Range(0f, 5f)] private float dominantDamageInfluence = 2.25f;
    [SerializeField, Range(0f, 5f)] private float secondaryDamageInfluence = 1.25f;
    [SerializeField, Range(0f, 1f)] private float minSecondaryDamageRatioForAttackType = 0.18f;
    [SerializeField, Range(0f, 1f)] private float randomVariationPercent = 0.20f;
    [SerializeField] private bool avoidSameMediumAndHeavyType = true;

    [Header("Effect Chances")]
    [SerializeField, Range(0f, 1f)] private float maxMediumSlowChance = 0.30f;
    [SerializeField, Range(0f, 1f)] private float maxHeavySlowChance = 0.40f;

    [SerializeField, Range(0f, 1f)] private float maxMediumDotChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maxHeavyDotChance = 0.35f;

    [SerializeField, Range(0f, 1f)] private float maxMediumKnockChance = 0.10f;
    [SerializeField, Range(0f, 1f)] private float maxHeavyKnockChance = 0.20f;

    private struct DamageTypeWeight
    {
        public DamageType damageType;
        public float weight;

        public DamageTypeWeight(DamageType damageType, float weight)
        {
            this.damageType = damageType;
            this.weight = weight;
        }
    }

    public EnemyAdaptationRuntimeConfig Generate(CombatTelemetryData data)
    {
        if (data == null)
            return EnemyAdaptationRuntimeConfig.Default();

        EnemyAdaptationRuntimeConfig config = new EnemyAdaptationRuntimeConfig();
        config.enabled = true;

        config.sourceCompletedLevel = Mathf.Max(1, data.completedLevel);
        config.targetLevel = config.sourceCompletedLevel + 1;

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

        ApplyResistanceForDamageType(data, config, DamageType.Physical, dominantType);
        ApplyResistanceForDamageType(data, config, DamageType.Fire, dominantType);
        ApplyResistanceForDamageType(data, config, DamageType.Earth, dominantType);
        ApplyResistanceForDamageType(data, config, DamageType.Wind, dominantType);
        ApplyResistanceForDamageType(data, config, DamageType.Lightning, dominantType);
        ApplyResistanceForDamageType(data, config, DamageType.Ice, dominantType);
    }

    private void ApplyResistanceForDamageType(
        CombatTelemetryData data,
        EnemyAdaptationRuntimeConfig config,
        DamageType damageType,
        DamageType dominantType)
    {
        float ratio = data.GetDamageRatio(damageType);

        if (ratio < minDamageRatioForResistance)
            return;

        bool isDominant = damageType == dominantType;

        float maxBonus = isDominant
            ? maxDominantResistanceBonus
            : maxSecondaryResistanceBonus;

        float t = Mathf.InverseLerp(
            minDamageRatioForResistance,
            fullResistanceRatio,
            ratio
        );

        float bonus = Mathf.Lerp(
            minResistanceBonusWhenRelevant,
            maxBonus,
            t
        );

        AddResistance(config, damageType, bonus);
    }

    private void ApplyAttackTypeAdaptation(CombatTelemetryData data, EnemyAdaptationRuntimeConfig config)
    {
        FindTopTwoPlayerDamageTypes(
            data,
            out DamageType dominantPlayerDamage,
            out float dominantRatio,
            out DamageType secondaryPlayerDamage,
            out float secondaryRatio
        );

        bool hasSecondary = secondaryRatio >= minSecondaryDamageRatioForAttackType;

        List<DamageTypeWeight> mediumWeights = BuildMediumDamageWeights(
            data,
            dominantPlayerDamage,
            dominantRatio,
            hasSecondary ? secondaryPlayerDamage : DamageType.Physical,
            hasSecondary ? secondaryRatio : 0f
        );

        DamageType mediumType = PickWeightedDamageType(mediumWeights, DamageType.Physical);

        List<DamageTypeWeight> heavyWeights = BuildHeavyDamageWeights(
            data,
            dominantPlayerDamage,
            dominantRatio,
            hasSecondary ? secondaryPlayerDamage : DamageType.Physical,
            hasSecondary ? secondaryRatio : 0f,
            mediumType
        );

        StoreDamageTypeWeights(config, mediumWeights, heavyWeights);

        DamageType heavyType = PickWeightedDamageType(heavyWeights, DamageType.Physical);

        if (avoidSameMediumAndHeavyType && heavyType == mediumType)
        {
            heavyType = PickWeightedDamageType(
                heavyWeights,
                DamageType.Physical,
                mediumType,
                0.25f
            );
        }

        config.overrideMediumAttackDamageType = true;
        config.mediumAttackDamageType = mediumType;

        config.overrideHeavyAttackDamageType = true;
        config.heavyAttackDamageType = heavyType;
    }

    private List<DamageTypeWeight> BuildMediumDamageWeights(
        CombatTelemetryData data,
        DamageType dominantPlayerDamage,
        float dominantRatio,
        DamageType secondaryPlayerDamage,
        float secondaryRatio)
    {
        List<DamageTypeWeight> weights = CreateBasePlaystyleWeights(data, false);

        AddCounterDamageWeights(weights, dominantPlayerDamage, dominantDamageInfluence * Mathf.Clamp01(dominantRatio));
        AddCounterDamageWeights(weights, secondaryPlayerDamage, secondaryDamageInfluence * Mathf.Clamp01(secondaryRatio));

        AddSmallGlobalVariety(weights, 0.35f);

        return weights;
    }

    private List<DamageTypeWeight> BuildHeavyDamageWeights(
        CombatTelemetryData data,
        DamageType dominantPlayerDamage,
        float dominantRatio,
        DamageType secondaryPlayerDamage,
        float secondaryRatio,
        DamageType mediumType)
    {
        List<DamageTypeWeight> weights = CreateBasePlaystyleWeights(data, true);

        AddCounterDamageWeights(weights, dominantPlayerDamage, dominantDamageInfluence * 1.25f * Mathf.Clamp01(dominantRatio));
        AddCounterDamageWeights(weights, secondaryPlayerDamage, secondaryDamageInfluence * 1.10f * Mathf.Clamp01(secondaryRatio));

        AddDamageTypeWeight(weights, mediumType, -0.75f);
        AddSmallGlobalVariety(weights, 0.50f);

        return weights;
    }

    private List<DamageTypeWeight> CreateBasePlaystyleWeights(CombatTelemetryData data, bool heavyAttack)
    {
        List<DamageTypeWeight> weights = new List<DamageTypeWeight>();

        bool playerIsKiting = data.averageDistanceToEnemies >= kitingDistanceThreshold;
        bool playerIsCloseCombat = data.averageDistanceToEnemies <= closeCombatDistanceThreshold;

        if (playerIsKiting)
        {
            AddDamageTypeWeight(weights, DamageType.Physical, heavyAttack ? 0.55f : 0.75f);
            AddDamageTypeWeight(weights, DamageType.Fire, heavyAttack ? 1.15f : 0.85f);
            AddDamageTypeWeight(weights, DamageType.Earth, heavyAttack ? 1.00f : 0.65f);
            AddDamageTypeWeight(weights, DamageType.Wind, heavyAttack ? 1.35f : 1.65f);
            AddDamageTypeWeight(weights, DamageType.Lightning, heavyAttack ? 1.75f : 1.25f);
            AddDamageTypeWeight(weights, DamageType.Ice, heavyAttack ? 1.35f : 1.85f);
            return weights;
        }

        if (playerIsCloseCombat)
        {
            AddDamageTypeWeight(weights, DamageType.Physical, heavyAttack ? 1.80f : 2.20f);
            AddDamageTypeWeight(weights, DamageType.Fire, heavyAttack ? 1.45f : 1.15f);
            AddDamageTypeWeight(weights, DamageType.Earth, heavyAttack ? 1.80f : 1.65f);
            AddDamageTypeWeight(weights, DamageType.Wind, heavyAttack ? 0.90f : 0.85f);
            AddDamageTypeWeight(weights, DamageType.Lightning, heavyAttack ? 0.95f : 0.75f);
            AddDamageTypeWeight(weights, DamageType.Ice, heavyAttack ? 0.95f : 0.85f);
            return weights;
        }

        AddDamageTypeWeight(weights, DamageType.Physical, heavyAttack ? 1.10f : 1.25f);
        AddDamageTypeWeight(weights, DamageType.Fire, heavyAttack ? 1.15f : 1.00f);
        AddDamageTypeWeight(weights, DamageType.Earth, heavyAttack ? 1.15f : 1.00f);
        AddDamageTypeWeight(weights, DamageType.Wind, heavyAttack ? 1.10f : 1.00f);
        AddDamageTypeWeight(weights, DamageType.Lightning, heavyAttack ? 1.20f : 1.00f);
        AddDamageTypeWeight(weights, DamageType.Ice, heavyAttack ? 1.10f : 1.00f);

        return weights;
    }

    private void AddCounterDamageWeights(List<DamageTypeWeight> weights, DamageType playerDamageType, float influence)
    {
        if (influence <= 0f)
            return;

        switch (playerDamageType)
        {
            case DamageType.Physical:
                AddDamageTypeWeight(weights, DamageType.Fire, influence * 0.80f);
                AddDamageTypeWeight(weights, DamageType.Earth, influence * 0.65f);
                AddDamageTypeWeight(weights, DamageType.Lightning, influence * 0.75f);
                break;

            case DamageType.Fire:
                AddDamageTypeWeight(weights, DamageType.Ice, influence * 1.00f);
                AddDamageTypeWeight(weights, DamageType.Lightning, influence * 0.75f);
                AddDamageTypeWeight(weights, DamageType.Earth, influence * 0.45f);
                break;

            case DamageType.Ice:
                AddDamageTypeWeight(weights, DamageType.Fire, influence * 1.00f);
                AddDamageTypeWeight(weights, DamageType.Earth, influence * 0.70f);
                AddDamageTypeWeight(weights, DamageType.Wind, influence * 0.40f);
                break;

            case DamageType.Earth:
                AddDamageTypeWeight(weights, DamageType.Wind, influence * 1.00f);
                AddDamageTypeWeight(weights, DamageType.Fire, influence * 0.65f);
                AddDamageTypeWeight(weights, DamageType.Lightning, influence * 0.45f);
                break;

            case DamageType.Wind:
                AddDamageTypeWeight(weights, DamageType.Lightning, influence * 1.00f);
                AddDamageTypeWeight(weights, DamageType.Ice, influence * 0.70f);
                AddDamageTypeWeight(weights, DamageType.Physical, influence * 0.35f);
                break;

            case DamageType.Lightning:
                AddDamageTypeWeight(weights, DamageType.Earth, influence * 1.00f);
                AddDamageTypeWeight(weights, DamageType.Wind, influence * 0.70f);
                AddDamageTypeWeight(weights, DamageType.Fire, influence * 0.45f);
                break;
        }
    }

    private void AddSmallGlobalVariety(List<DamageTypeWeight> weights, float amount)
    {
        AddDamageTypeWeight(weights, DamageType.Physical, amount * Random.Range(0.50f, 1.10f));
        AddDamageTypeWeight(weights, DamageType.Fire, amount * Random.Range(0.50f, 1.25f));
        AddDamageTypeWeight(weights, DamageType.Earth, amount * Random.Range(0.50f, 1.25f));
        AddDamageTypeWeight(weights, DamageType.Wind, amount * Random.Range(0.50f, 1.25f));
        AddDamageTypeWeight(weights, DamageType.Lightning, amount * Random.Range(0.50f, 1.25f));
        AddDamageTypeWeight(weights, DamageType.Ice, amount * Random.Range(0.50f, 1.25f));
    }

    private DamageType PickWeightedDamageType(
        List<DamageTypeWeight> weights,
        DamageType fallback,
        DamageType discouragedType = DamageType.Physical,
        float discouragedMultiplier = 1f)
    {
        if (weights == null || weights.Count == 0)
            return fallback;

        List<DamageTypeWeight> adjusted = new List<DamageTypeWeight>();
        float totalWeight = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            float weight = Mathf.Max(0f, weights[i].weight);

            if (weights[i].damageType == discouragedType)
                weight *= Mathf.Clamp01(discouragedMultiplier);

            float randomMultiplier = Random.Range(
                1f - randomVariationPercent,
                1f + randomVariationPercent
            );

            weight *= Mathf.Max(0.05f, randomMultiplier);

            adjusted.Add(new DamageTypeWeight(weights[i].damageType, weight));
            totalWeight += weight;
        }

        if (totalWeight <= 0.001f)
            return fallback;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < adjusted.Count; i++)
        {
            cumulative += adjusted[i].weight;

            if (roll <= cumulative)
                return adjusted[i].damageType;
        }

        return fallback;
    }

    private void AddDamageTypeWeight(List<DamageTypeWeight> weights, DamageType damageType, float value)
    {
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i].damageType != damageType)
                continue;

            float newWeight = Mathf.Max(0f, weights[i].weight + value);
            weights[i] = new DamageTypeWeight(damageType, newWeight);
            return;
        }

        weights.Add(new DamageTypeWeight(damageType, Mathf.Max(0f, value)));
    }

    private void StoreDamageTypeWeights(
        EnemyAdaptationRuntimeConfig config,
        List<DamageTypeWeight> mediumWeights,
        List<DamageTypeWeight> heavyWeights)
    {
        if (config == null)
            return;

        config.SetMediumDamageWeights(
            GetWeight(mediumWeights, DamageType.Physical),
            GetWeight(mediumWeights, DamageType.Fire),
            GetWeight(mediumWeights, DamageType.Earth),
            GetWeight(mediumWeights, DamageType.Wind),
            GetWeight(mediumWeights, DamageType.Lightning),
            GetWeight(mediumWeights, DamageType.Ice)
        );

        config.SetHeavyDamageWeights(
            GetWeight(heavyWeights, DamageType.Physical),
            GetWeight(heavyWeights, DamageType.Fire),
            GetWeight(heavyWeights, DamageType.Earth),
            GetWeight(heavyWeights, DamageType.Wind),
            GetWeight(heavyWeights, DamageType.Lightning),
            GetWeight(heavyWeights, DamageType.Ice)
        );
    }

    private float GetWeight(List<DamageTypeWeight> weights, DamageType damageType)
    {
        if (weights == null)
            return 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i].damageType == damageType)
                return Mathf.Max(0f, weights[i].weight);
        }

        return 0f;
    }

    private void FindTopTwoPlayerDamageTypes(
        CombatTelemetryData data,
        out DamageType dominantType,
        out float dominantRatio,
        out DamageType secondaryType,
        out float secondaryRatio)
    {
        dominantType = DamageType.Physical;
        dominantRatio = -1f;

        secondaryType = DamageType.Physical;
        secondaryRatio = -1f;

        CheckDamageRatioCandidate(data, DamageType.Physical, ref dominantType, ref dominantRatio, ref secondaryType, ref secondaryRatio);
        CheckDamageRatioCandidate(data, DamageType.Fire, ref dominantType, ref dominantRatio, ref secondaryType, ref secondaryRatio);
        CheckDamageRatioCandidate(data, DamageType.Earth, ref dominantType, ref dominantRatio, ref secondaryType, ref secondaryRatio);
        CheckDamageRatioCandidate(data, DamageType.Wind, ref dominantType, ref dominantRatio, ref secondaryType, ref secondaryRatio);
        CheckDamageRatioCandidate(data, DamageType.Lightning, ref dominantType, ref dominantRatio, ref secondaryType, ref secondaryRatio);
        CheckDamageRatioCandidate(data, DamageType.Ice, ref dominantType, ref dominantRatio, ref secondaryType, ref secondaryRatio);

        if (dominantRatio < 0f)
            dominantRatio = 0f;

        if (secondaryRatio < 0f)
            secondaryRatio = 0f;
    }

    private void CheckDamageRatioCandidate(
        CombatTelemetryData data,
        DamageType damageType,
        ref DamageType dominantType,
        ref float dominantRatio,
        ref DamageType secondaryType,
        ref float secondaryRatio)
    {
        float ratio = data.GetDamageRatio(damageType);

        if (ratio > dominantRatio)
        {
            secondaryType = dominantType;
            secondaryRatio = dominantRatio;

            dominantType = damageType;
            dominantRatio = ratio;
            return;
        }

        if (ratio > secondaryRatio)
        {
            secondaryType = damageType;
            secondaryRatio = ratio;
        }
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

        bool usesElementalAttack =
            config.mediumAttackDamageType != DamageType.Physical ||
            config.heavyAttackDamageType != DamageType.Physical;

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
