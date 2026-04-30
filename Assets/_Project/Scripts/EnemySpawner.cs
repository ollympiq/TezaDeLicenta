using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
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
            SpawnForCurrentLevel();
    }

    [ContextMenu("Spawn For Current Level")]
    public void SpawnForCurrentLevel()
    {
        if (spawnVolume == null)
        {
            Debug.LogWarning("EnemySpawner: lipseste BoxCollider-ul de spawn.");
            return;
        }

        ResolvePlayerStats();

        if (clearOldSpawnedChildrenFirst)
            ClearSpawnedEnemies();

        usedPositions.Clear();

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

        if (refreshTurnManagerAfterSpawn && TurnManager.Instance != null)
            TurnManager.Instance.RefreshEnemyList();
    }

    [ContextMenu("Clear Spawned Enemies")]
    public void ClearSpawnedEnemies()
    {
        spawnedEnemies.Clear();
        usedPositions.Clear();

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

            ConfigureSpawnedEnemy(instance);

            spawnedEnemies.Add(instance);
            usedPositions.Add(instance.transform.position);
        }
    }

    private void ConfigureSpawnedEnemy(GameObject instance)
    {
        if (instance == null)
            return;

        EnemyTurnController enemyTurn = instance.GetComponent<EnemyTurnController>();
        if (enemyTurn != null && playerStats != null)
            enemyTurn.SetTarget(playerStats);

        EnemyLevelScaler scaler = instance.GetComponent<EnemyLevelScaler>();
        if (scaler != null)
            scaler.ApplyScaling();

        TurnAgentLock turnLock = instance.GetComponent<TurnAgentLock>();
        if (turnLock != null)
            turnLock.SnapLockedTransformToNavMesh();

        CharacterHealth health = instance.GetComponent<CharacterHealth>();
        if (health != null && health.IsDead)
            health.ResetToFull();
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