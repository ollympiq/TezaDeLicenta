using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class RunLevelFlow : MonoBehaviour
{
    public static RunLevelFlow Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string combatSceneName = "Level01Scene";
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("Run State")]
    [SerializeField] private int maxCombatLevel = 10;
    [SerializeField] private int currentCombatLevel = 1;

    [Tooltip("In lobby, this is the next combat level that should be loaded. 0 means no pending combat level.")]
    [SerializeField] private int pendingLobbyLevel = 0;

    private bool isLoadingScene;

    public int CurrentCombatLevel => Mathf.Clamp(currentCombatLevel, 1, maxCombatLevel);
    public int RawPendingLobbyLevel => pendingLobbyLevel;
    public int MaxCombatLevel => maxCombatLevel;

    public int PendingLobbyLevel
    {
        get
        {
            if (pendingLobbyLevel <= 0)
                return CurrentCombatLevel;

            return Mathf.Clamp(pendingLobbyLevel, 1, maxCombatLevel);
        }
    }

    public bool HasPendingLobbyCombat
    {
        get
        {
            return pendingLobbyLevel >= 1 && pendingLobbyLevel <= maxCombatLevel;
        }
    }

    public bool CanContinueFromLobby
    {
        get
        {
            return !isLoadingScene && HasPendingLobbyCombat;
        }
    }

    public bool IsLastCombatCleared
    {
        get
        {
            return currentCombatLevel >= maxCombatLevel && !HasPendingLobbyCombat;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        maxCombatLevel = Mathf.Max(1, maxCombatLevel);
        currentCombatLevel = Mathf.Clamp(currentCombatLevel, 1, maxCombatLevel);
        pendingLobbyLevel = Mathf.Clamp(pendingLobbyLevel, 0, maxCombatLevel);

        DebugState("Awake");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoadingScene = false;
        DebugState("Scene loaded: " + scene.name);
    }

    public void StartNewRun(int startLevel = 1)
    {
        currentCombatLevel = Mathf.Clamp(startLevel, 1, maxCombatLevel);
        pendingLobbyLevel = 0;
        isLoadingScene = false;

        DebugState("StartNewRun");
    }

    public void ApplyLoadedRunState(int loadedCurrentCombatLevel, int loadedPendingLobbyLevel)
    {
        maxCombatLevel = Mathf.Max(1, maxCombatLevel);

        currentCombatLevel = Mathf.Clamp(loadedCurrentCombatLevel, 1, maxCombatLevel);
        pendingLobbyLevel = Mathf.Clamp(loadedPendingLobbyLevel, 0, maxCombatLevel);
        isLoadingScene = false;

        DebugState("ApplyLoadedRunState");
    }

    public void EnterLobbyAfterCombat(int clearedCombatLevel)
    {
        int safeLevel = Mathf.Clamp(clearedCombatLevel, 1, maxCombatLevel);

        currentCombatLevel = safeLevel;

        if (safeLevel >= maxCombatLevel)
            pendingLobbyLevel = 0;
        else
            pendingLobbyLevel = safeLevel + 1;

        DebugState("EnterLobbyAfterCombat");
    }

    public int AdvanceFromLobbyToNextCombat()
    {
        if (!HasPendingLobbyCombat)
        {
            DebugState("AdvanceFromLobbyToNextCombat blocked");
            return CurrentCombatLevel;
        }

        currentCombatLevel = Mathf.Clamp(pendingLobbyLevel, 1, maxCombatLevel);
        pendingLobbyLevel = 0;

        DebugState("AdvanceFromLobbyToNextCombat");
        return currentCombatLevel;
    }

    public void LoadLobbyAfterCombat(int clearedCombatLevel)
    {
        if (isLoadingScene)
            return;

        EnterLobbyAfterCombat(clearedCombatLevel);

        isLoadingScene = true;
        SceneManager.LoadScene(lobbySceneName);
    }

    public void LoadNextCombatFromLobby()
    {
        if (isLoadingScene)
            return;

        if (!HasPendingLobbyCombat)
        {
            DebugState("LoadNextCombatFromLobby blocked");
            return;
        }

        AdvanceFromLobbyToNextCombat();

        isLoadingScene = true;
        SceneManager.LoadScene(combatSceneName);
    }

    private void DebugState(string source)
    {
        Debug.Log(
            $"RunLevelFlow [{source}] | " +
            $"currentCombatLevel={currentCombatLevel}, " +
            $"pendingLobbyLevel={pendingLobbyLevel}, " +
            $"maxCombatLevel={maxCombatLevel}, " +
            $"hasPendingLobbyCombat={HasPendingLobbyCombat}, " +
            $"canContinue={CanContinueFromLobby}, " +
            $"isLoadingScene={isLoadingScene}"
        );
    }
}