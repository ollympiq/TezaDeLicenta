using UnityEngine;
using Unity.InferenceEngine;

[CreateAssetMenu(fileName = "NeuralAdaptationGenerator", menuName = "Game/AI/Neural Adaptation Generator")]
public class NeuralAdaptationGenerator : ScriptableObject
{
    [Header("Model")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private BackendType backendType = BackendType.CPU;

    [Header("Output Scaling")]
    [SerializeField] private float resistanceMaxPercent = 35f;
    [SerializeField] private int primaryAttributeMaxBonus = 30;
    [SerializeField] private int maxHpMaxBonus = 120;
    [SerializeField] private int armorMaxBonus = 20;

    [Header("Resistance Post Processing")]
    [SerializeField] private bool maskUnusedResistanceOutputs = true;
    [SerializeField, Range(0f, 1f)] private float minDamageRatioToKeepResistance = 0.12f;

    [SerializeField] private bool rebalanceResistanceByDamageRatios = true;
    [SerializeField, Range(0f, 1f)] private float resistanceRatioRebalanceStrength = 0.75f;
    [SerializeField, Range(0.5f, 2f)] private float resistanceRatioCurve = 1.10f;
    [SerializeField] private float minRebalancedResistancePercent = 1.5f;

    [Header("Effect Chance Caps")]
    [SerializeField] private bool clampEffectChances = true;
    [SerializeField, Range(0f, 1f)] private float maxMediumSlowChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxHeavySlowChance = 0.45f;
    [SerializeField, Range(0f, 1f)] private float maxMediumDotChance = 0.45f;
    [SerializeField, Range(0f, 1f)] private float maxHeavyDotChance = 0.55f;
    [SerializeField, Range(0f, 1f)] private float maxMediumKnockChance = 0.20f;
    [SerializeField, Range(0f, 1f)] private float maxHeavyKnockChance = 0.30f;

    [Header("Difficulty Post Processing")]
    [SerializeField] private bool reduceTankBonusesForLongSafeFights = true;
    [SerializeField] private float longFightClearTimeRatio = 1.25f;
    [SerializeField, Range(0f, 1f)] private float safeHpPercentThreshold = 0.55f;
    [SerializeField] private int safeFightMaxPotionsUsed = 1;
    [SerializeField] private int maxHpBonusDuringLongSafeFight = 12;
    [SerializeField] private int maxArmorBonusDuringLongSafeFight = 2;

    [Header("Hard Fight Safety")]
    [SerializeField] private bool softenAdaptationWhenPlayerStruggles = true;
    [SerializeField, Range(0f, 1f)] private float strugglingHpPercentThreshold = 0.25f;
    [SerializeField] private int strugglingPotionsThreshold = 3;
    [SerializeField] private float strugglingEffectMultiplier = 0.55f;
    [SerializeField] private int maxHpBonusWhenStruggling = 10;
    [SerializeField] private int maxArmorBonusWhenStruggling = 2;

    [Header("Debug")]
    [SerializeField] private bool logNeuralOutput = false;

    private Model runtimeModel;
    private Worker worker;
    private bool isInitialized;

    public bool IsReady => modelAsset != null;

    private const int InputSize = 18;
    private const int OutputSize = 30;

    public EnemyAdaptationRuntimeConfig Generate(CombatTelemetryData data)
    {
        if (data == null)
            return EnemyAdaptationRuntimeConfig.Default();

        if (!InitializeIfNeeded())
        {
            GameLog.Warning("NeuralAdaptationGenerator: modelul nu este initializat. Se intoarce config default.");
            return EnemyAdaptationRuntimeConfig.Default();
        }

        float[] input = BuildInputVector(data);
        float[] output = RunModel(input);

        if (output == null || output.Length < OutputSize)
        {
            GameLog.Warning("NeuralAdaptationGenerator: output invalid de la model.");
            return EnemyAdaptationRuntimeConfig.Default();
        }

        EnemyAdaptationRuntimeConfig config = ConvertOutputToConfig(output);

        config.sourceCompletedLevel = Mathf.Max(1, data.completedLevel);
        config.targetLevel = config.sourceCompletedLevel + 1;
        config.enabled = true;

        ApplyPostProcessing(config, data);

        config.Clamp();

        if (logNeuralOutput)
            LogConfig(config);

        return config;
    }

    private bool InitializeIfNeeded()
    {
        if (isInitialized)
            return true;

        if (modelAsset == null)
        {
            Debug.LogWarning("NeuralAdaptationGenerator: ModelAsset lipseste.");
            return false;
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, backendType);

        isInitialized = true;
        return true;
    }

    private float[] RunModel(float[] inputValues)
    {
        TensorShape inputShape = new TensorShape(1, InputSize);

        using Tensor<float> inputTensor = new Tensor<float>(inputShape, inputValues);

        worker.Schedule(inputTensor);

        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;

        if (outputTensor == null)
        {
            Debug.LogWarning("NeuralAdaptationGenerator: output tensor este null.");
            return null;
        }

        using Tensor<float> cpuOutput = outputTensor.ReadbackAndClone();
        return cpuOutput.DownloadToArray();
    }

    private float[] BuildInputVector(CombatTelemetryData data)
    {
        float totalDamage = Mathf.Max(0f, data.TotalDamageDealt);

        float physicalRatio = SafeRatio(data.physicalDamageDealt, totalDamage);
        float fireRatio = SafeRatio(data.fireDamageDealt, totalDamage);
        float earthRatio = SafeRatio(data.earthDamageDealt, totalDamage);
        float windRatio = SafeRatio(data.windDamageDealt, totalDamage);
        float lightningRatio = SafeRatio(data.lightningDamageDealt, totalDamage);
        float iceRatio = SafeRatio(data.iceDamageDealt, totalDamage);

        return new float[]
        {
            Clamp01(data.completedLevel / 10f),
            Clamp01(data.clearTimeSeconds / Mathf.Max(1f, data.targetClearTimeSeconds)),
            Clamp01(data.playerHpPercentAtEnd),
            Clamp01(data.damageTaken / 500f),
            Clamp01(data.potionsUsed / 6f),
            Clamp01(data.skillsUsed / 12f),
            Clamp01(data.basicAttacksUsed / 12f),
            Clamp01(data.movementActions / 15f),
            Clamp01(data.averageDistanceToEnemies / 15f),

            physicalRatio,
            fireRatio,
            earthRatio,
            windRatio,
            lightningRatio,
            iceRatio,

            Clamp01(data.dotEffectsApplied / 8f),
            Clamp01(data.slowEffectsApplied / 8f),
            Clamp01(data.knockEffectsApplied / 8f)
        };
    }

    private EnemyAdaptationRuntimeConfig ConvertOutputToConfig(float[] output)
    {
        EnemyAdaptationRuntimeConfig config = new EnemyAdaptationRuntimeConfig();

        config.enabled = true;

        config.physicalResistanceBonus = Clamp01(output[0]) * resistanceMaxPercent;
        config.fireResistanceBonus = Clamp01(output[1]) * resistanceMaxPercent;
        config.earthResistanceBonus = Clamp01(output[2]) * resistanceMaxPercent;
        config.windResistanceBonus = Clamp01(output[3]) * resistanceMaxPercent;
        config.lightningResistanceBonus = Clamp01(output[4]) * resistanceMaxPercent;
        config.iceResistanceBonus = Clamp01(output[5]) * resistanceMaxPercent;

        config.SetMediumDamageWeights(
            Clamp01(output[6]),
            Clamp01(output[7]),
            Clamp01(output[8]),
            Clamp01(output[9]),
            Clamp01(output[10]),
            Clamp01(output[11])
        );

        config.SetHeavyDamageWeights(
            Clamp01(output[12]),
            Clamp01(output[13]),
            Clamp01(output[14]),
            Clamp01(output[15]),
            Clamp01(output[16]),
            Clamp01(output[17])
        );

        config.overrideMediumAttackDamageType = true;
        config.mediumAttackDamageType = config.RollMediumDamageType(DamageType.Physical);

        config.overrideHeavyAttackDamageType = true;
        config.heavyAttackDamageType = config.RollHeavyDamageType(
            DamageType.Physical,
            config.mediumAttackDamageType,
            0.25f
        );

        config.mediumSlowChance = Clamp01(output[18]);
        config.mediumDotChance = Clamp01(output[19]);
        config.mediumKnockChance = Clamp01(output[20]);

        config.heavySlowChance = Clamp01(output[21]);
        config.heavyDotChance = Clamp01(output[22]);
        config.heavyKnockChance = Clamp01(output[23]);

        config.strengthBonus = Mathf.RoundToInt(Clamp01(output[24]) * primaryAttributeMaxBonus);
        config.constitutionBonus = Mathf.RoundToInt(Clamp01(output[25]) * primaryAttributeMaxBonus);
        config.dexterityBonus = Mathf.RoundToInt(Clamp01(output[26]) * primaryAttributeMaxBonus);
        config.intelligenceBonus = Mathf.RoundToInt(Clamp01(output[27]) * primaryAttributeMaxBonus);

        config.maxHpBonus = Mathf.RoundToInt(Clamp01(output[28]) * maxHpMaxBonus);
        config.armorBonus = Mathf.RoundToInt(Clamp01(output[29]) * armorMaxBonus);

        return config;
    }

    private void ApplyPostProcessing(EnemyAdaptationRuntimeConfig config, CombatTelemetryData data)
    {
        ApplyResistanceDamageRatioMask(config, data);
        ApplyResistanceRatioRebalance(config, data);
        ApplyEffectChanceCaps(config);
        ApplyLongSafeFightTuning(config, data);
        ApplyStrugglingPlayerSafety(config, data);
    }

    private void ApplyResistanceDamageRatioMask(EnemyAdaptationRuntimeConfig config, CombatTelemetryData data)
    {
        if (!maskUnusedResistanceOutputs)
            return;

        if (config == null || data == null)
            return;

        float totalDamage = Mathf.Max(0f, data.TotalDamageDealt);

        if (totalDamage <= 0f)
        {
            config.ClearAllResistanceBonuses();
            return;
        }

        if (GetDamageRatio(data, DamageType.Physical, totalDamage) < minDamageRatioToKeepResistance)
            config.physicalResistanceBonus = 0f;

        if (GetDamageRatio(data, DamageType.Fire, totalDamage) < minDamageRatioToKeepResistance)
            config.fireResistanceBonus = 0f;

        if (GetDamageRatio(data, DamageType.Earth, totalDamage) < minDamageRatioToKeepResistance)
            config.earthResistanceBonus = 0f;

        if (GetDamageRatio(data, DamageType.Wind, totalDamage) < minDamageRatioToKeepResistance)
            config.windResistanceBonus = 0f;

        if (GetDamageRatio(data, DamageType.Lightning, totalDamage) < minDamageRatioToKeepResistance)
            config.lightningResistanceBonus = 0f;

        if (GetDamageRatio(data, DamageType.Ice, totalDamage) < minDamageRatioToKeepResistance)
            config.iceResistanceBonus = 0f;
    }

    private void ApplyResistanceRatioRebalance(EnemyAdaptationRuntimeConfig config, CombatTelemetryData data)
    {
        if (!rebalanceResistanceByDamageRatios)
            return;

        if (config == null || data == null)
            return;

        float totalDamage = Mathf.Max(0f, data.TotalDamageDealt);

        if (totalDamage <= 0f)
            return;

        DamageType[] types =
        {
            DamageType.Physical,
            DamageType.Fire,
            DamageType.Earth,
            DamageType.Wind,
            DamageType.Lightning,
            DamageType.Ice
        };

        int relevantCount = 0;
        float maxRatio = 0f;
        float maxResistance = 0f;

        for (int i = 0; i < types.Length; i++)
        {
            DamageType type = types[i];
            float ratio = GetDamageRatio(data, type, totalDamage);

            if (ratio < minDamageRatioToKeepResistance)
                continue;

            relevantCount++;
            maxRatio = Mathf.Max(maxRatio, ratio);
            maxResistance = Mathf.Max(maxResistance, GetResistanceBonus(config, type));
        }

        if (relevantCount <= 1)
            return;

        if (maxRatio <= 0.001f || maxResistance <= 0.001f)
            return;

        for (int i = 0; i < types.Length; i++)
        {
            DamageType type = types[i];
            float ratio = GetDamageRatio(data, type, totalDamage);

            if (ratio < minDamageRatioToKeepResistance)
            {
                SetResistanceBonus(config, type, 0f);
                continue;
            }

            float ratioRelativeToDominant = Mathf.Clamp01(ratio / maxRatio);
            float shapedRatio = Mathf.Pow(ratioRelativeToDominant, resistanceRatioCurve);

            float targetResistance = Mathf.Lerp(
                minRebalancedResistancePercent,
                maxResistance,
                shapedRatio
            );

            float currentResistance = GetResistanceBonus(config, type);

            float finalResistance = Mathf.Lerp(
                currentResistance,
                targetResistance,
                resistanceRatioRebalanceStrength
            );

            SetResistanceBonus(config, type, finalResistance);
        }
    }

    private void ApplyEffectChanceCaps(EnemyAdaptationRuntimeConfig config)
    {
        if (!clampEffectChances || config == null)
            return;

        config.mediumSlowChance = Mathf.Min(config.mediumSlowChance, maxMediumSlowChance);
        config.heavySlowChance = Mathf.Min(config.heavySlowChance, maxHeavySlowChance);

        config.mediumDotChance = Mathf.Min(config.mediumDotChance, maxMediumDotChance);
        config.heavyDotChance = Mathf.Min(config.heavyDotChance, maxHeavyDotChance);

        config.mediumKnockChance = Mathf.Min(config.mediumKnockChance, maxMediumKnockChance);
        config.heavyKnockChance = Mathf.Min(config.heavyKnockChance, maxHeavyKnockChance);
    }

    private void ApplyLongSafeFightTuning(EnemyAdaptationRuntimeConfig config, CombatTelemetryData data)
    {
        if (!reduceTankBonusesForLongSafeFights)
            return;

        if (config == null || data == null)
            return;

        float clearRatio = data.clearTimeSeconds / Mathf.Max(1f, data.targetClearTimeSeconds);

        bool fightWasLong = clearRatio >= longFightClearTimeRatio;
        bool playerWasSafe = data.playerHpPercentAtEnd >= safeHpPercentThreshold;
        bool playerDidNotUseManyPotions = data.potionsUsed <= safeFightMaxPotionsUsed;

        if (!fightWasLong || !playerWasSafe || !playerDidNotUseManyPotions)
            return;

        config.maxHpBonus = Mathf.Min(config.maxHpBonus, maxHpBonusDuringLongSafeFight);
        config.armorBonus = Mathf.Min(config.armorBonus, maxArmorBonusDuringLongSafeFight);
    }

    private void ApplyStrugglingPlayerSafety(EnemyAdaptationRuntimeConfig config, CombatTelemetryData data)
    {
        if (!softenAdaptationWhenPlayerStruggles)
            return;

        if (config == null || data == null)
            return;

        bool lowHp = data.playerHpPercentAtEnd <= strugglingHpPercentThreshold;
        bool manyPotions = data.potionsUsed >= strugglingPotionsThreshold;

        if (!lowHp && !manyPotions)
            return;

        config.maxHpBonus = Mathf.Min(config.maxHpBonus, maxHpBonusWhenStruggling);
        config.armorBonus = Mathf.Min(config.armorBonus, maxArmorBonusWhenStruggling);

        config.mediumSlowChance *= strugglingEffectMultiplier;
        config.heavySlowChance *= strugglingEffectMultiplier;

        config.mediumDotChance *= strugglingEffectMultiplier;
        config.heavyDotChance *= strugglingEffectMultiplier;

        config.mediumKnockChance *= strugglingEffectMultiplier;
        config.heavyKnockChance *= strugglingEffectMultiplier;
    }

    private float GetDamageRatio(CombatTelemetryData data, DamageType type, float totalDamage)
    {
        if (data == null || totalDamage <= 0f)
            return 0f;

        switch (type)
        {
            case DamageType.Physical:
                return SafeRatio(data.physicalDamageDealt, totalDamage);

            case DamageType.Fire:
                return SafeRatio(data.fireDamageDealt, totalDamage);

            case DamageType.Earth:
                return SafeRatio(data.earthDamageDealt, totalDamage);

            case DamageType.Wind:
                return SafeRatio(data.windDamageDealt, totalDamage);

            case DamageType.Lightning:
                return SafeRatio(data.lightningDamageDealt, totalDamage);

            case DamageType.Ice:
                return SafeRatio(data.iceDamageDealt, totalDamage);

            default:
                return 0f;
        }
    }

    private float GetResistanceBonus(EnemyAdaptationRuntimeConfig config, DamageType type)
    {
        if (config == null)
            return 0f;

        switch (type)
        {
            case DamageType.Physical:
                return config.physicalResistanceBonus;

            case DamageType.Fire:
                return config.fireResistanceBonus;

            case DamageType.Earth:
                return config.earthResistanceBonus;

            case DamageType.Wind:
                return config.windResistanceBonus;

            case DamageType.Lightning:
                return config.lightningResistanceBonus;

            case DamageType.Ice:
                return config.iceResistanceBonus;

            default:
                return 0f;
        }
    }

    private void SetResistanceBonus(EnemyAdaptationRuntimeConfig config, DamageType type, float value)
    {
        if (config == null)
            return;

        value = Mathf.Max(0f, value);

        switch (type)
        {
            case DamageType.Physical:
                config.physicalResistanceBonus = value;
                break;

            case DamageType.Fire:
                config.fireResistanceBonus = value;
                break;

            case DamageType.Earth:
                config.earthResistanceBonus = value;
                break;

            case DamageType.Wind:
                config.windResistanceBonus = value;
                break;

            case DamageType.Lightning:
                config.lightningResistanceBonus = value;
                break;

            case DamageType.Ice:
                config.iceResistanceBonus = value;
                break;
        }
    }

    private void LogConfig(EnemyAdaptationRuntimeConfig config)
    {
        if (config == null)
            return;

        GameLog.Info(
            "=== Neural Enemy Adaptation Config ===\n" +
            $"Enabled: {config.enabled}\n" +
            $"Source Completed Level: {config.sourceCompletedLevel}\n" +
            $"Target Level: {config.targetLevel}\n" +
            $"Medium Damage Type: {config.mediumAttackDamageType}\n" +
            $"Heavy Damage Type: {config.heavyAttackDamageType}\n" +
            $"STR: +{config.strengthBonus}, CON: +{config.constitutionBonus}, DEX: +{config.dexterityBonus}, INT: +{config.intelligenceBonus}\n" +
            $"HP: +{config.maxHpBonus}, Armor: +{config.armorBonus}\n" +
            $"Resistances | Physical: +{config.physicalResistanceBonus:0.#}%, Fire: +{config.fireResistanceBonus:0.#}%, Earth: +{config.earthResistanceBonus:0.#}%, " +
            $"Wind: +{config.windResistanceBonus:0.#}%, Lightning: +{config.lightningResistanceBonus:0.#}%, Ice: +{config.iceResistanceBonus:0.#}%\n" +
            $"Effects | Medium Slow: {config.mediumSlowChance:0.##}, Medium DOT: {config.mediumDotChance:0.##}, Medium Knock: {config.mediumKnockChance:0.##}\n" +
            $"Effects | Heavy Slow: {config.heavySlowChance:0.##}, Heavy DOT: {config.heavyDotChance:0.##}, Heavy Knock: {config.heavyKnockChance:0.##}\n" +
            config.BuildDamageWeightDebugText()
        );
    }

    private float SafeRatio(float value, float total)
    {
        if (total <= 0f)
            return 0f;

        return Mathf.Clamp01(value / total);
    }

    private float Clamp01(float value)
    {
        return Mathf.Clamp01(value);
    }

    private void OnDisable()
    {
        DisposeWorker();
    }

    private void OnDestroy()
    {
        DisposeWorker();
    }

    private void DisposeWorker()
    {
        if (worker != null)
        {
            worker.Dispose();
            worker = null;
        }

        runtimeModel = null;
        isInitialized = false;
    }
}