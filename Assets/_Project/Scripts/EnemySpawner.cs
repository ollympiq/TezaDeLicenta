using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyResistanceDistributionMode
{
    None,
    DominantOnly,
    SecondaryOnly,
    DominantAndSecondary
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Runtime Adaptation")]
    [SerializeField] private bool applyRuntimeAdaptation = true;
    [SerializeField] private bool preferGameSessionAdaptation = true;
    [SerializeField] private EnemyAdaptationEffectLibrary adaptationEffectLibrary;
    [SerializeField] private bool debugAdaptationLogs = true;
    [SerializeField] private bool sendAdaptationLogsToGameLog = true;

    [Header("Damage Type Distribution")]
    [SerializeField] private bool rollDamageTypesPerEnemy = true;
    [SerializeField] private bool avoidSameMediumAndHeavyDamageTypePerEnemy = true;

    [Header("Adaptation Distribution")]
    [SerializeField, Range(0f, 1f)] private float normalEnemyAdaptationChance = 0.65f;
    [SerializeField, Range(0f, 1f)] private float miniBossAdaptationChance = 1f;
    [SerializeField, Range(0f, 1f)] private float bossAdaptationChance = 1f;

    [SerializeField] private Vector2 normalEnemyIntensityRange = new Vector2(0.45f, 0.85f);
    [SerializeField] private Vector2 miniBossIntensityRange = new Vector2(0.85f, 1.10f);
    [SerializeField] private Vector2 bossIntensityRange = new Vector2(1.00f, 1.25f);

    [SerializeField] private bool keepFirstNormalEnemyNeutral = true;
    [SerializeField] private bool forceAtLeastOneNormalAdapted = true;

    [Header("Resistance Distribution")]
    [SerializeField, Range(0f, 1f)] private float normalBothResistanceChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float normalDominantOnlyChance = 0.45f;
    [SerializeField, Range(0f, 1f)] private float normalSecondaryOnlyChance = 0.20f;

    [SerializeField, Range(0f, 1f)] private float miniBossBothResistanceChance = 0.70f;
    [SerializeField, Range(0f, 1f)] private float miniBossDominantOnlyChance = 0.20f;
    [SerializeField, Range(0f, 1f)] private float miniBossSecondaryOnlyChance = 0.10f;

    [SerializeField, Range(0f, 1f)] private float bossBothResistanceChance = 0.85f;
    [SerializeField, Range(0f, 1f)] private float bossDominantOnlyChance = 0.10f;
    [SerializeField, Range(0f, 1f)] private float bossSecondaryOnlyChance = 0.05f;

    [Header("Pools")]
    [SerializeField] private List<GameObject> normalEnemies = new List<GameObject>();
    [SerializeField] private List<GameObject> miniBossEnemies = new List<GameObject>();
    [SerializeField] private List<GameObject> bossEnemies = new List<GameObject>();

    [Header("Level Rules")]
    [SerializeField] private int normalsPerRegularLevel = 3;
    [SerializeField] private int miniBossesPerRegularLevel = 1;
    [SerializeField] private int normalsPerBossLevel = 3;
    [SerializeField] private int miniBossesPerBossLevel = 1;
    [SerializeField] private int bossesPerBossLevel = 1;
    [SerializeField] private List<int> bossLevels = new List<int> { 5, 10 };

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider spawnVolume;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private float minDistanceBetweenEnemies = 2.75f;
    [SerializeField] private float minDistanceFromPlayer = 5f;
    [SerializeField] private int maxAttemptsPerEnemy = 40;

    [Header("References")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private CharacterStats playerStats;
    [SerializeField] private CurrentLevelContext currentLevelContext;

    [Header("Behavior")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool clearOldSpawnedChildrenFirst = true;
    [SerializeField] private bool renameSpawnedEnemies = true;
    [SerializeField] private bool refreshTurnManagerAfterSpawn = true;
    [SerializeField] private bool debugLogs = true;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private readonly List<Vector3> usedPositions = new List<Vector3>();
    private readonly List<EnemyTurnController> spawnedEnemyTurns = new List<EnemyTurnController>();

    private bool hasSpawnedThisScene;
    private int normalAdaptedThisSpawn;
    private Coroutine delayedStartRoutine;

    private void Reset()
    {
        spawnVolume = GetComponent<BoxCollider>();

        if (spawnVolume != null)
            spawnVolume.isTrigger = true;
    }

    private void OnValidate()
    {
        if (spawnVolume == null)
            spawnVolume = GetComponent<BoxCollider>();

        if (spawnVolume != null)
            spawnVolume.isTrigger = true;
    }

    private void Awake()
    {
        if (spawnVolume == null)
            spawnVolume = GetComponent<BoxCollider>();

        if (spawnParent == null)
            spawnParent = transform;

        if (currentLevelContext == null)
            currentLevelContext = FindFirstObjectByType<CurrentLevelContext>();

        ResolvePlayerStats();
    }

    private void Start()
    {
        if (spawnOnStart)
            delayedStartRoutine = StartCoroutine(SpawnAfterOneFrame());
    }

    private IEnumerator SpawnAfterOneFrame()
    {
        yield return null;

        delayedStartRoutine = null;
        SpawnForCurrentLevel();
    }

    [ContextMenu("Spawn For Current Level")]
    public void SpawnForCurrentLevel()
    {
        if (hasSpawnedThisScene)
        {
            if (debugLogs)
                Debug.Log("EnemySpawner: spawn ignorat, a fost deja facut pentru scena curenta.");

            return;
        }

        if (spawnVolume == null)
        {
            Debug.LogWarning("EnemySpawner: lipseste BoxCollider-ul de spawn.");
            return;
        }

        ResolvePlayerStats();

        if (clearOldSpawnedChildrenFirst)
            ClearSpawnedEnemies();

        usedPositions.Clear();
        spawnedEnemies.Clear();
        spawnedEnemyTurns.Clear();
        normalAdaptedThisSpawn = 0;

        int currentLevel = ResolveCurrentLevel();
        bool isBossLevel = bossLevels != null && bossLevels.Contains(currentLevel);

        int normalCount = isBossLevel ? normalsPerBossLevel : normalsPerRegularLevel;
        int miniBossCount = isBossLevel ? miniBossesPerBossLevel : miniBossesPerRegularLevel;
        int bossCount = isBossLevel ? bossesPerBossLevel : 0;

        if (debugLogs)
        {
            Debug.Log(
                $"EnemySpawner | CurrentLevel={currentLevel} | IsBossLevel={isBossLevel} | " +
                $"Normals={normalCount} | MiniBosses={miniBossCount} | Bosses={bossCount}"
            );
        }

        SpawnCategory(normalEnemies, normalCount, "Normal");
        SpawnCategory(miniBossEnemies, miniBossCount, "MiniBoss");
        SpawnCategory(bossEnemies, bossCount, "Boss");

        hasSpawnedThisScene = true;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.ResetCombatState();

            if (refreshTurnManagerAfterSpawn)
                TurnManager.Instance.SetEnemyTurns(spawnedEnemyTurns);
            else
                TurnManager.Instance.RefreshEnemyList();

            TurnManager.Instance.StartCombatOnce();
        }
    }

    [ContextMenu("Clear Spawned Enemies")]
    public void ClearSpawnedEnemies()
    {
        hasSpawnedThisScene = false;
        normalAdaptedThisSpawn = 0;
        spawnedEnemies.Clear();
        spawnedEnemyTurns.Clear();
        usedPositions.Clear();

        if (TurnManager.Instance != null)
            TurnManager.Instance.ResetCombatState();

        Transform parent = spawnParent != null ? spawnParent : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void SpawnCategory(List<GameObject> pool, int count, string categoryLabel)
    {
        if (count <= 0)
            return;

        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner: pool gol pentru categoria {categoryLabel}.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = pool[UnityEngine.Random.Range(0, pool.Count)];

            if (prefab == null)
                continue;

            if (!TryFindSpawnPosition(out Vector3 navSpawnPos))
            {
                Debug.LogWarning($"EnemySpawner: nu am gasit loc valid pentru {categoryLabel} #{i + 1}.");
                continue;
            }

            Vector3 spawnPos = GetAdjustedSpawnPosition(navSpawnPos, prefab);
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            Transform parent = spawnParent != null ? spawnParent : null;
            GameObject instance = Instantiate(prefab, spawnPos, rotation);

            if (parent != null)
                instance.transform.SetParent(parent, true);

            if (renameSpawnedEnemies)
                instance.name = $"{categoryLabel}_{i + 1}_{prefab.name}";

            ConfigureSpawnedEnemy(instance, categoryLabel, i, count);

            EnemyTurnController enemyTurn = instance.GetComponent<EnemyTurnController>();
            if (enemyTurn != null)
                spawnedEnemyTurns.Add(enemyTurn);

            spawnedEnemies.Add(instance);
            usedPositions.Add(instance.transform.position);
        }
    }

    private void ConfigureSpawnedEnemy(GameObject instance, string categoryLabel, int categoryIndex, int categoryCount)
    {
        if (instance == null)
            return;

        EnemyTurnController enemyTurn = instance.GetComponent<EnemyTurnController>();

        if (enemyTurn != null && playerStats != null)
            enemyTurn.SetTarget(playerStats);

        EnemyLevelScaler scaler = instance.GetComponent<EnemyLevelScaler>();
        if (scaler != null)
            scaler.ApplyScaling();

        ApplyRuntimeAdaptationToSpawnedEnemy(instance, categoryLabel, categoryIndex, categoryCount);

        TurnAgentLock turnLock = instance.GetComponent<TurnAgentLock>();
        if (turnLock != null)
            turnLock.SnapLockedTransformToNavMesh();

        CharacterHealth health = instance.GetComponent<CharacterHealth>();
        if (health != null && health.IsDead)
            health.ResetToFull();
    }

    private void ApplyRuntimeAdaptationToSpawnedEnemy(
        GameObject instance,
        string categoryLabel,
        int categoryIndex,
        int categoryCount)
    {
        if (instance == null)
            return;

        if (!applyRuntimeAdaptation || !preferGameSessionAdaptation)
            return;

        if (GameSession.Instance == null)
        {
            LogAdaptation("EnemySpawner: GameSession lipseste, adaptarea runtime nu se aplica.");
            return;
        }

        EnemyAdaptationRuntimeConfig runtimeConfig = GameSession.Instance.GetNextEnemyAdaptationConfig();

        if (runtimeConfig == null || !runtimeConfig.enabled)
        {
            LogAdaptation($"EnemySpawner: nu exista config runtime activ pentru {instance.name}.");
            return;
        }

        int currentLevel = ResolveCurrentLevel();

        if (runtimeConfig.targetLevel <= 0)
        {
            LogAdaptation(
                $"EnemySpawner: config runtime ignorat pentru {instance.name}. " +
                $"TargetLevel invalid: {runtimeConfig.targetLevel}."
            );
            return;
        }

        if (runtimeConfig.targetLevel != currentLevel)
        {
            LogAdaptation(
                $"EnemySpawner: adaptare ignorata pentru {instance.name}. " +
                $"CurrentLevel={currentLevel}, TargetLevel={runtimeConfig.targetLevel}, " +
                $"SourceCompletedLevel={runtimeConfig.sourceCompletedLevel}."
            );
            return;
        }

        if (!ShouldApplyAdaptationToEnemy(categoryLabel, categoryIndex, categoryCount))
        {
            LogAdaptation(
                $"EnemySpawner: {instance.name} ramane neutru. " +
                $"Category={categoryLabel}, Index={categoryIndex + 1}/{categoryCount}."
            );
            return;
        }

        float intensity = RollAdaptationIntensity(categoryLabel);
        EnemyAdaptationRuntimeConfig scaledConfig = runtimeConfig.CreateScaledCopy(intensity);

        EnemyResistanceDistributionMode resistanceMode = ApplyResistanceDistribution(scaledConfig, categoryLabel);
        ApplyDamageTypeDistribution(scaledConfig);

        EnemyAdaptationApplyReport report = EnemyAdaptationApplier.Apply(
            instance,
            scaledConfig,
            adaptationEffectLibrary
        );

        if (IsNormalCategory(categoryLabel))
            normalAdaptedThisSpawn++;

        LogAdaptation(
            $"EnemySpawner: adaptare runtime aplicata pe {instance.name}. " +
            $"Category={categoryLabel}, Intensity={intensity:0.00}, ResistanceMode={resistanceMode}, " +
            $"Resistances=[{scaledConfig.BuildResistanceDebugText()}], " +
            $"MediumDamage={scaledConfig.mediumAttackDamageType}, HeavyDamage={scaledConfig.heavyAttackDamageType}, " +
            $"MediumEffect={report.MediumEffectText}, HeavyEffect={report.HeavyEffectText}, " +
            $"{scaledConfig.BuildDamageWeightDebugText()}, " +
            $"CurrentLevel={currentLevel}, SourceCompletedLevel={runtimeConfig.sourceCompletedLevel}, " +
            $"TargetLevel={runtimeConfig.targetLevel}."
        );
    }

    private void ApplyDamageTypeDistribution(EnemyAdaptationRuntimeConfig config)
    {
        if (config == null || !rollDamageTypesPerEnemy)
            return;

        if (config.HasMediumDamageWeights())
        {
            config.mediumAttackDamageType = config.RollMediumDamageType(config.mediumAttackDamageType);
            config.overrideMediumAttackDamageType = true;
        }

        if (config.HasHeavyDamageWeights())
        {
            DamageType discouragedType = avoidSameMediumAndHeavyDamageTypePerEnemy
                ? config.mediumAttackDamageType
                : DamageType.Physical;

            float discouragedMultiplier = avoidSameMediumAndHeavyDamageTypePerEnemy ? 0.25f : 1f;

            config.heavyAttackDamageType = config.RollHeavyDamageType(
                config.heavyAttackDamageType,
                discouragedType,
                discouragedMultiplier
            );

            config.overrideHeavyAttackDamageType = true;
        }
    }

    private bool ShouldApplyAdaptationToEnemy(string categoryLabel, int categoryIndex, int categoryCount)
    {
        if (IsNormalCategory(categoryLabel))
        {
            if (keepFirstNormalEnemyNeutral && categoryCount > 1 && categoryIndex == 0)
                return false;

            bool isLastNormal = categoryIndex == categoryCount - 1;
            if (forceAtLeastOneNormalAdapted && isLastNormal && normalAdaptedThisSpawn == 0)
                return true;

            return UnityEngine.Random.value <= normalEnemyAdaptationChance;
        }

        if (IsMiniBossCategory(categoryLabel))
            return UnityEngine.Random.value <= miniBossAdaptationChance;

        if (IsBossCategory(categoryLabel))
            return UnityEngine.Random.value <= bossAdaptationChance;

        return UnityEngine.Random.value <= normalEnemyAdaptationChance;
    }

    private float RollAdaptationIntensity(string categoryLabel)
    {
        Vector2 range = normalEnemyIntensityRange;

        if (IsMiniBossCategory(categoryLabel))
            range = miniBossIntensityRange;
        else if (IsBossCategory(categoryLabel))
            range = bossIntensityRange;

        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);

        return UnityEngine.Random.Range(min, max);
    }

    private EnemyResistanceDistributionMode ApplyResistanceDistribution(
        EnemyAdaptationRuntimeConfig config,
        string categoryLabel)
    {
        if (config == null)
            return EnemyResistanceDistributionMode.None;

        FindTopTwoResistanceTypes(
            config,
            out DamageType dominantType,
            out float dominantValue,
            out DamageType secondaryType,
            out float secondaryValue
        );

        if (dominantValue <= 0.001f)
        {
            config.ClearAllResistanceBonuses();
            return EnemyResistanceDistributionMode.None;
        }

        EnemyResistanceDistributionMode mode = RollResistanceDistributionMode(
            categoryLabel,
            secondaryValue > 0.001f
        );

        config.ClearAllResistanceBonuses();

        switch (mode)
        {
            case EnemyResistanceDistributionMode.DominantOnly:
                config.SetResistanceBonus(dominantType, dominantValue);
                break;

            case EnemyResistanceDistributionMode.SecondaryOnly:
                if (secondaryValue > 0.001f)
                    config.SetResistanceBonus(secondaryType, secondaryValue);
                else
                    mode = EnemyResistanceDistributionMode.None;
                break;

            case EnemyResistanceDistributionMode.DominantAndSecondary:
                config.SetResistanceBonus(dominantType, dominantValue);

                if (secondaryValue > 0.001f)
                    config.SetResistanceBonus(secondaryType, secondaryValue);
                break;

            case EnemyResistanceDistributionMode.None:
            default:
                break;
        }

        config.Clamp();
        return mode;
    }

    private EnemyResistanceDistributionMode RollResistanceDistributionMode(string categoryLabel, bool hasSecondaryResistance)
    {
        float bothChance;
        float dominantChance;
        float secondaryChance;

        if (IsBossCategory(categoryLabel))
        {
            bothChance = bossBothResistanceChance;
            dominantChance = bossDominantOnlyChance;
            secondaryChance = bossSecondaryOnlyChance;
        }
        else if (IsMiniBossCategory(categoryLabel))
        {
            bothChance = miniBossBothResistanceChance;
            dominantChance = miniBossDominantOnlyChance;
            secondaryChance = miniBossSecondaryOnlyChance;
        }
        else
        {
            bothChance = normalBothResistanceChance;
            dominantChance = normalDominantOnlyChance;
            secondaryChance = normalSecondaryOnlyChance;
        }

        if (!hasSecondaryResistance)
        {
            bothChance = 0f;
            secondaryChance = 0f;
            dominantChance = Mathf.Max(dominantChance, 0.75f);
        }

        float roll = UnityEngine.Random.value;

        if (roll < bothChance)
            return EnemyResistanceDistributionMode.DominantAndSecondary;

        roll -= bothChance;

        if (roll < dominantChance)
            return EnemyResistanceDistributionMode.DominantOnly;

        roll -= dominantChance;

        if (roll < secondaryChance)
            return EnemyResistanceDistributionMode.SecondaryOnly;

        return EnemyResistanceDistributionMode.None;
    }

    private void FindTopTwoResistanceTypes(
        EnemyAdaptationRuntimeConfig config,
        out DamageType dominantType,
        out float dominantValue,
        out DamageType secondaryType,
        out float secondaryValue)
    {
        dominantType = DamageType.Physical;
        dominantValue = -1f;

        secondaryType = DamageType.Physical;
        secondaryValue = -1f;

        CheckResistanceCandidate(config, DamageType.Physical, ref dominantType, ref dominantValue, ref secondaryType, ref secondaryValue);
        CheckResistanceCandidate(config, DamageType.Fire, ref dominantType, ref dominantValue, ref secondaryType, ref secondaryValue);
        CheckResistanceCandidate(config, DamageType.Earth, ref dominantType, ref dominantValue, ref secondaryType, ref secondaryValue);
        CheckResistanceCandidate(config, DamageType.Wind, ref dominantType, ref dominantValue, ref secondaryType, ref secondaryValue);
        CheckResistanceCandidate(config, DamageType.Lightning, ref dominantType, ref dominantValue, ref secondaryType, ref secondaryValue);
        CheckResistanceCandidate(config, DamageType.Ice, ref dominantType, ref dominantValue, ref secondaryType, ref secondaryValue);

        if (dominantValue < 0f)
            dominantValue = 0f;

        if (secondaryValue < 0f)
            secondaryValue = 0f;
    }

    private void CheckResistanceCandidate(
        EnemyAdaptationRuntimeConfig config,
        DamageType damageType,
        ref DamageType dominantType,
        ref float dominantValue,
        ref DamageType secondaryType,
        ref float secondaryValue)
    {
        float value = config.GetResistanceBonus(damageType);

        if (value > dominantValue)
        {
            secondaryType = dominantType;
            secondaryValue = dominantValue;

            dominantType = damageType;
            dominantValue = value;
            return;
        }

        if (value > secondaryValue)
        {
            secondaryType = damageType;
            secondaryValue = value;
        }
    }

    private bool IsNormalCategory(string categoryLabel)
    {
        return string.Equals(categoryLabel, "Normal", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsMiniBossCategory(string categoryLabel)
    {
        return string.Equals(categoryLabel, "MiniBoss", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsBossCategory(string categoryLabel)
    {
        return string.Equals(categoryLabel, "Boss", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryFindSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;
        Bounds bounds = spawnVolume.bounds;

        for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
        {
            Vector3 candidate = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
                continue;

            Vector3 sampled = navHit.position;

            if (!IsInsideSpawnXZ(sampled, bounds))
                continue;

            if (playerStats != null)
            {
                float distToPlayer = GetPlanarDistance(sampled, playerStats.transform.position);

                if (distToPlayer < minDistanceFromPlayer)
                    continue;
            }

            if (IsTooCloseToExisting(sampled))
                continue;

            position = sampled;
            return true;
        }

        return false;
    }

    private Vector3 GetAdjustedSpawnPosition(Vector3 navMeshPosition, GameObject prefabForOffsetCheck)
    {
        Vector3 result = navMeshPosition;

        if (prefabForOffsetCheck == null)
            return result;

        NavMeshAgent prefabAgent = prefabForOffsetCheck.GetComponent<NavMeshAgent>();

        if (prefabAgent != null)
            result.y += prefabAgent.baseOffset;

        return result;
    }

    private bool IsInsideSpawnXZ(Vector3 worldPos, Bounds bounds)
    {
        return worldPos.x >= bounds.min.x &&
               worldPos.x <= bounds.max.x &&
               worldPos.z >= bounds.min.z &&
               worldPos.z <= bounds.max.z;
    }

    private bool IsTooCloseToExisting(Vector3 candidate)
    {
        for (int i = 0; i < usedPositions.Count; i++)
        {
            float distance = GetPlanarDistance(candidate, usedPositions[i]);

            if (distance < minDistanceBetweenEnemies)
                return true;
        }

        return false;
    }

    private float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private int ResolveCurrentLevel()
    {
        if (currentLevelContext != null)
            return currentLevelContext.CurrentLevel;

        if (CurrentLevelContext.Instance != null)
            return CurrentLevelContext.Instance.CurrentLevel;

        return 1;
    }

    private void ResolvePlayerStats()
    {
        if (playerStats != null)
            return;

        PlayerTurnController playerTurn = FindFirstObjectByType<PlayerTurnController>();

        if (playerTurn != null)
        {
            playerStats = playerTurn.GetComponent<CharacterStats>();

            if (playerStats != null)
                return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            playerStats = playerObject.GetComponent<CharacterStats>();

        if (playerStats == null)
            playerStats = FindFirstObjectByType<CharacterStats>();
    }

    private void LogAdaptation(string message)
    {
        if (debugAdaptationLogs)
            Debug.Log(message);

        if (sendAdaptationLogsToGameLog)
            GameLog.Info(message);
    }

    private void OnDisable()
    {
        if (delayedStartRoutine != null)
        {
            StopCoroutine(delayedStartRoutine);
            delayedStartRoutine = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = spawnVolume != null ? spawnVolume : GetComponent<BoxCollider>();

        if (box == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0f, 1f, 1f, 0.18f);
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = new Color(0f, 1f, 1f, 0.85f);
        Gizmos.DrawWireCube(box.center, box.size);

        Gizmos.matrix = previousMatrix;
    }
}
