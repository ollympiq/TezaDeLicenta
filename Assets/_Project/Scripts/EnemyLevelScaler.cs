using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class EnemyLevelScaler : MonoBehaviour
{
    [Header("Tier Source")]
    [SerializeField] private bool readTierFromLootContainer = true;
    [SerializeField] private EnemyLootTier fallbackTier = EnemyLootTier.Normal;

    [Header("Application")]
    [SerializeField] private bool autoApplyOnStart = true;
    [SerializeField] private bool refillHealthAfterScaling = true;

    [Header("Per Level Bonuses")]
    [SerializeField] private int strengthPerLevel = 1;
    [SerializeField] private int constitutionPerLevel = 1;
    [SerializeField] private int dexterityPerLevel = 1;
    [SerializeField] private int intelligencePerLevel = 1;
    [SerializeField] private int maxHpPerLevel = 8;
    [SerializeField] private int armorPerLevel = 1;
    [SerializeField] private float allResistancePerLevel = 0.5f;
    [SerializeField] private float extraPhysicalResistancePerLevel = 0.25f;

    [Header("Tier Multipliers")]
    [SerializeField] private float normalMultiplier = 1f;
    [SerializeField] private float miniBossMultiplier = 1.5f;
    [SerializeField] private float bossMultiplier = 2f;

    private CharacterStats stats;
    private CharacterHealth health;
    private EnemyLootContainer lootContainer;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<CharacterHealth>();
        lootContainer = GetComponent<EnemyLootContainer>();
    }

    private void Start()
    {
        if (autoApplyOnStart)
            ApplyScaling();
    }

    [ContextMenu("Apply Current Level Scaling")]
    public void ApplyScaling()
    {
        if (stats == null)
            return;

        int sceneLevel = ResolveSceneLevel();
        int levelSteps = Mathf.Max(0, sceneLevel - stats.BaseLevel);
        EnemyLootTier tier = ResolveTier();
        float multiplier = GetTierMultiplier(tier);

        stats.SetRuntimeLevelOffset(levelSteps, false);

        stats.SetRuntimePrimaryAttributeBonuses(
            Mathf.RoundToInt(levelSteps * strengthPerLevel * multiplier),
            Mathf.RoundToInt(levelSteps * constitutionPerLevel * multiplier),
            Mathf.RoundToInt(levelSteps * dexterityPerLevel * multiplier),
            Mathf.RoundToInt(levelSteps * intelligencePerLevel * multiplier),
            false);

        stats.SetRuntimeBaseValueBonuses(
            Mathf.RoundToInt(levelSteps * maxHpPerLevel * multiplier),
            Mathf.RoundToInt(levelSteps * armorPerLevel * multiplier),
            false);

        float allRes = levelSteps * allResistancePerLevel * multiplier;
        float extraPhysRes = levelSteps * extraPhysicalResistancePerLevel * multiplier;

        stats.SetRuntimeResistanceBonuses(
            allRes + extraPhysRes,
            allRes,
            allRes,
            allRes,
            allRes,
            allRes,
            false);

        stats.NotifyStatsChanged();

        if (refillHealthAfterScaling && health != null)
            health.ResetToFull();
    }

    [ContextMenu("Clear Runtime Scaling")]
    public void ClearScaling()
    {
        if (stats == null)
            return;

        stats.ClearRuntimeScaling();
        if (refillHealthAfterScaling && health != null)
            health.ResetToFull();
    }

    private int ResolveSceneLevel()
    {
        if (CurrentLevelContext.Instance != null)
            return CurrentLevelContext.Instance.CurrentLevel;

        return 1;
    }

    private EnemyLootTier ResolveTier()
    {
        if (readTierFromLootContainer && lootContainer != null)
            return lootContainer.LootTier;

        return fallbackTier;
    }

    private float GetTierMultiplier(EnemyLootTier tier)
    {
        switch (tier)
        {
            case EnemyLootTier.MiniBoss:
                return miniBossMultiplier;

            case EnemyLootTier.Boss:
                return bossMultiplier;

            default:
                return normalMultiplier;
        }
    }
}