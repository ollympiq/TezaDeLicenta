using UnityEngine;
using System.IO;

public class CombatTelemetryTracker : MonoBehaviour
{
    public static CombatTelemetryTracker Instance { get; private set; }

    [Header("Adaptation Generation")]
    [SerializeField] private bool generateAdaptationOnFinalize = true;
    [SerializeField] private RuleBasedAdaptationGenerator adaptationGenerator;
    [SerializeField] private bool saveAdaptationToGameSession = true;
    [SerializeField] private bool logGeneratedAdaptation = true;

    [Header("Telemetry Export")]
    [SerializeField] private bool saveTelemetryToJsonLines = true;
    [SerializeField] private string telemetryFileName = "combat_telemetry.jsonl";

    [Header("Settings")]
    [SerializeField] private bool autoStartTracking = true;
    [SerializeField] private float targetClearTimeSeconds = 120f;
    [SerializeField] private float distanceSampleInterval = 0.5f;

    [Header("Finalize Protection")]
    [SerializeField] private float minTrackingTimeBeforeFinalize = 2f;
    [SerializeField] private bool requireAliveEnemySeenBeforeFinalize = true;
    [SerializeField] private bool requireCombatSeenBeforeFinalize = true;

    [Header("Debug")]
    [SerializeField] private bool logFinalTelemetry = true;

    private CombatTelemetryData currentData;

    private float startTime;
    private float sceneTrackingStartTime;
    private float lastDistanceSampleTime;
    private float distanceSum;
    private int distanceSampleCount;

    private bool isTracking;
    private bool finalized;
    private bool timerStarted;
    private bool hasSeenAliveEnemy;
    private bool hasSeenCombatActive;

    private TurnManager subscribedTurnManager;

    public CombatTelemetryData CurrentData => currentData;
    public bool IsTracking => isTracking;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        TrySubscribeTurnManager();

        if (autoStartTracking)
            StartTracking();
    }

    private void OnDestroy()
    {
        UnsubscribeTurnManager();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        TrySubscribeTurnManager();

        if (!isTracking || finalized)
            return;

        ObserveCombatState();

        if (Time.time - lastDistanceSampleTime >= distanceSampleInterval)
        {
            lastDistanceSampleTime = Time.time;
            SampleAverageDistanceToEnemies();
        }

        if (CanFinalizeByTurnManager())
            FinalizeTracking();
    }

    public void StartTracking()
    {
        currentData = new CombatTelemetryData();

        if (CurrentLevelContext.Instance != null)
            currentData.completedLevel = CurrentLevelContext.Instance.CurrentLevel;
        else
            currentData.completedLevel = 1;

        currentData.targetClearTimeSeconds = targetClearTimeSeconds;

        sceneTrackingStartTime = Time.time;
        startTime = Time.time;
        lastDistanceSampleTime = Time.time;

        distanceSum = 0f;
        distanceSampleCount = 0;

        isTracking = true;
        finalized = false;
        timerStarted = false;
        hasSeenAliveEnemy = false;
        hasSeenCombatActive = false;

        GameLog.Info("CombatTelemetryTracker: telemetria luptei a fost pornita.");
    }

    public CombatTelemetryData FinalizeTracking()
    {
        if (currentData == null)
            StartTracking();

        if (finalized)
            return currentData;

        finalized = true;
        isTracking = false;

        float usedStartTime = timerStarted ? startTime : sceneTrackingStartTime;
        currentData.clearTimeSeconds = Mathf.Max(0f, Time.time - usedStartTime);

        if (distanceSampleCount > 0)
            currentData.averageDistanceToEnemies = distanceSum / distanceSampleCount;

        CaptureFinalPlayerState();

        if (logFinalTelemetry)
            LogTelemetry(currentData);

        if (saveTelemetryToJsonLines)
            SaveTelemetryJsonLine(currentData);

        GenerateAndStoreAdaptation(currentData);

        return currentData;
    }

    private void SaveTelemetryJsonLine(CombatTelemetryData data)
    {
        if (data == null)
            return;

        try
        {
            string path = Path.Combine(Application.persistentDataPath, telemetryFileName);
            string json = JsonUtility.ToJson(data);

            File.AppendAllText(path, json + "\n");

            GameLog.Info($"CombatTelemetryTracker: telemetria a fost salvata in JSONL: {path}");
        }
        catch (System.Exception ex)
        {
            GameLog.Warning($"CombatTelemetryTracker: nu a putut salva telemetria JSONL. {ex.Message}");
        }
    }

    private void GenerateAndStoreAdaptation(CombatTelemetryData data)
    {
        if (!generateAdaptationOnFinalize)
            return;

        if (data == null)
            return;

        if (adaptationGenerator == null)
        {
            GameLog.Warning("CombatTelemetryTracker: lipseste RuleBasedAdaptationGenerator.");
            return;
        }

        EnemyAdaptationRuntimeConfig config = adaptationGenerator.Generate(data);

        if (config == null)
        {
            GameLog.Warning("CombatTelemetryTracker: generatorul nu a produs configuratie de adaptare.");
            return;
        }

        config.Clamp();

        if (saveAdaptationToGameSession)
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.SetNextEnemyAdaptationConfig(config);
            }
            else
            {
                GameLog.Warning("CombatTelemetryTracker: GameSession lipseste. Configuratia de adaptare nu a fost salvata.");
            }
        }

        if (logGeneratedAdaptation)
            LogGeneratedAdaptation(config);
    }

    private void LogGeneratedAdaptation(EnemyAdaptationRuntimeConfig config)
    {
        if (config == null)
            return;

        GameLog.Info(
            "=== Generated Enemy Adaptation Config ===\n" +
            $"Enabled: {config.enabled}\n" +
            $"Medium Damage Type: {(config.overrideMediumAttackDamageType ? config.mediumAttackDamageType.ToString() : "Default")}\n" +
            $"Heavy Damage Type: {(config.overrideHeavyAttackDamageType ? config.heavyAttackDamageType.ToString() : "Default")}\n" +
            $"STR: +{config.strengthBonus}, CON: +{config.constitutionBonus}, DEX: +{config.dexterityBonus}, INT: +{config.intelligenceBonus}\n" +
            $"HP: +{config.maxHpBonus}, Armor: +{config.armorBonus}\n" +
            $"Resistances | Physical: +{config.physicalResistanceBonus:0.#}%, Fire: +{config.fireResistanceBonus:0.#}%, Earth: +{config.earthResistanceBonus:0.#}%, " +
            $"Wind: +{config.windResistanceBonus:0.#}%, Lightning: +{config.lightningResistanceBonus:0.#}%, Ice: +{config.iceResistanceBonus:0.#}%\n" +
            $"Effects | Medium Slow: {config.mediumSlowChance:0.##}, Medium DOT: {config.mediumDotChance:0.##}, Medium Knock: {config.mediumKnockChance:0.##}\n" +
            $"Effects | Heavy Slow: {config.heavySlowChance:0.##}, Heavy DOT: {config.heavyDotChance:0.##}, Heavy Knock: {config.heavyKnockChance:0.##}\n" +
            $"Spawn Weights | Normal: {config.normalEnemyWeight:0.##}, MiniBoss: {config.miniBossWeight:0.##}, Boss: {config.bossWeight:0.##}"
        );
    }

    public void RecordPlayerDamageDealt(DamageType damageType, int amount)
    {
        if (!CanRecord() || amount <= 0)
            return;

        EnsureTimerStarted();

        switch (damageType)
        {
            case DamageType.Physical:
                currentData.physicalDamageDealt += amount;
                break;

            case DamageType.Fire:
                currentData.fireDamageDealt += amount;
                break;

            case DamageType.Earth:
                currentData.earthDamageDealt += amount;
                break;

            case DamageType.Wind:
                currentData.windDamageDealt += amount;
                break;

            case DamageType.Lightning:
                currentData.lightningDamageDealt += amount;
                break;

            case DamageType.Ice:
                currentData.iceDamageDealt += amount;
                break;
        }
    }

    public void RecordPlayerDamageTaken(int amount)
    {
        if (!CanRecord() || amount <= 0)
            return;

        EnsureTimerStarted();
        currentData.damageTaken += amount;
    }

    public void RecordPotionUsed()
    {
        if (!CanRecord())
            return;

        EnsureTimerStarted();
        currentData.potionsUsed++;
    }

    public void RecordSkillUsed()
    {
        if (!CanRecord())
            return;

        EnsureTimerStarted();
        currentData.skillsUsed++;
    }

    public void RecordBasicAttackUsed()
    {
        if (!CanRecord())
            return;

        EnsureTimerStarted();
        currentData.basicAttacksUsed++;
    }

    public void RecordMovementAction()
    {
        if (!CanRecord())
            return;

        EnsureTimerStarted();
        currentData.movementActions++;
    }

    public void RecordEffectApplied(SkillEffectType effectType)
    {
        if (!CanRecord())
            return;

        EnsureTimerStarted();

        switch (effectType)
        {
            case SkillEffectType.DamageOverTime:
                currentData.dotEffectsApplied++;
                break;

            case SkillEffectType.SlowMovement:
                currentData.slowEffectsApplied++;
                break;

            case SkillEffectType.SkipTurn:
                currentData.knockEffectsApplied++;
                break;
        }
    }

    private bool CanRecord()
    {
        return isTracking && !finalized && currentData != null;
    }

    private void TrySubscribeTurnManager()
    {
        if (subscribedTurnManager == TurnManager.Instance)
            return;

        UnsubscribeTurnManager();

        subscribedTurnManager = TurnManager.Instance;

        if (subscribedTurnManager != null)
            subscribedTurnManager.OnTurnStateChanged += HandleTurnStateChanged;
    }

    private void UnsubscribeTurnManager()
    {
        if (subscribedTurnManager != null)
            subscribedTurnManager.OnTurnStateChanged -= HandleTurnStateChanged;

        subscribedTurnManager = null;
    }

    private void HandleTurnStateChanged()
    {
        if (!isTracking || finalized)
            return;

        ObserveCombatState();

        if (CanFinalizeByTurnManager())
            FinalizeTracking();
    }

    private bool CanFinalizeByTurnManager()
    {
        if (!isTracking || finalized)
            return false;

        if (TurnManager.Instance == null)
            return false;

        if (!TurnManager.Instance.CanExitToLobby)
            return false;

        float elapsedSinceSceneTracking = Time.time - sceneTrackingStartTime;
        if (elapsedSinceSceneTracking < minTrackingTimeBeforeFinalize)
            return false;

        if (requireAliveEnemySeenBeforeFinalize && !hasSeenAliveEnemy)
            return false;

        if (requireCombatSeenBeforeFinalize && !hasSeenCombatActive)
            return false;

        return true;
    }

    private void ObserveCombatState()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.IsCombatActive)
        {
            hasSeenCombatActive = true;

            if (!timerStarted && HasAliveEnemies())
                StartCombatTimer();
        }

        if (HasAliveEnemies())
        {
            hasSeenAliveEnemy = true;

            if (!timerStarted && (TurnManager.Instance == null || TurnManager.Instance.IsCombatActive))
                StartCombatTimer();
        }
    }

    private void StartCombatTimer()
    {
        timerStarted = true;
        startTime = Time.time;
        lastDistanceSampleTime = Time.time;

        GameLog.Info("CombatTelemetryTracker: timerul luptei a inceput.");
    }

    private void EnsureTimerStarted()
    {
        if (timerStarted)
            return;

        timerStarted = true;
        startTime = Time.time;
        lastDistanceSampleTime = Time.time;
    }

    private bool HasAliveEnemies()
    {
        EnemyTurnController[] enemies = FindObjectsByType<EnemyTurnController>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            CharacterHealth health = enemies[i].GetComponent<CharacterHealth>();
            if (health != null && !health.IsDead)
                return true;
        }

        return false;
    }

    private void CaptureFinalPlayerState()
    {
        CharacterHealth playerHealth = FindPlayerHealth();

        if (playerHealth == null || playerHealth.MaxHP <= 0)
        {
            currentData.playerHpPercentAtEnd = 0f;
            return;
        }

        currentData.playerHpPercentAtEnd = Mathf.Clamp01(
            playerHealth.CurrentHP / (float)playerHealth.MaxHP
        );
    }

    private CharacterHealth FindPlayerHealth()
    {
        GameObject playerRoot = null;

        if (PlayerRuntimeRegistry.ResolvePlayerRoot() != null)
            playerRoot = PlayerRuntimeRegistry.ResolvePlayerRoot();

        if (playerRoot == null)
        {
            PlayerTurnController playerTurn = FindFirstObjectByType<PlayerTurnController>();
            if (playerTurn != null)
                playerRoot = playerTurn.gameObject;
        }

        if (playerRoot == null)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
                playerRoot = taggedPlayer;
        }

        return playerRoot != null ? playerRoot.GetComponent<CharacterHealth>() : null;
    }

    private void SampleAverageDistanceToEnemies()
    {
        GameObject playerRoot = null;

        if (PlayerRuntimeRegistry.ResolvePlayerRoot() != null)
            playerRoot = PlayerRuntimeRegistry.ResolvePlayerRoot();

        if (playerRoot == null)
        {
            PlayerTurnController playerTurn = FindFirstObjectByType<PlayerTurnController>();
            if (playerTurn != null)
                playerRoot = playerTurn.gameObject;
        }

        if (playerRoot == null)
            return;

        EnemyTurnController[] enemies = FindObjectsByType<EnemyTurnController>(FindObjectsSortMode.None);

        float totalDistance = 0f;
        int aliveEnemies = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            CharacterHealth enemyHealth = enemies[i].GetComponent<CharacterHealth>();
            if (enemyHealth == null || enemyHealth.IsDead)
                continue;

            Vector3 a = playerRoot.transform.position;
            Vector3 b = enemies[i].transform.position;

            a.y = 0f;
            b.y = 0f;

            totalDistance += Vector3.Distance(a, b);
            aliveEnemies++;
        }

        if (aliveEnemies <= 0)
            return;

        hasSeenAliveEnemy = true;

        distanceSum += totalDistance / aliveEnemies;
        distanceSampleCount++;
    }

    private void LogTelemetry(CombatTelemetryData data)
    {
        if (data == null)
            return;

        GameLog.Info(
            "=== Combat Telemetry Final ===\n" +
            $"Level: {data.completedLevel}\n" +
            $"Clear Time: {data.clearTimeSeconds:0.0}s / Target: {data.targetClearTimeSeconds:0.0}s\n" +
            $"Total Damage: {data.TotalDamageDealt}\n" +
            $"Physical: {data.physicalDamageDealt}, Fire: {data.fireDamageDealt}, Earth: {data.earthDamageDealt}, " +
            $"Wind: {data.windDamageDealt}, Lightning: {data.lightningDamageDealt}, Ice: {data.iceDamageDealt}\n" +
            $"HP End: {data.playerHpPercentAtEnd * 100f:0.0}%\n" +
            $"Damage Taken: {data.damageTaken}\n" +
            $"Potions: {data.potionsUsed}, Skills: {data.skillsUsed}, Basic Attacks: {data.basicAttacksUsed}, Moves: {data.movementActions}\n" +
            $"Avg Distance: {data.averageDistanceToEnemies:0.0}\n" +
            $"DOT: {data.dotEffectsApplied}, Slow: {data.slowEffectsApplied}, Knock: {data.knockEffectsApplied}"
        );
    }
}