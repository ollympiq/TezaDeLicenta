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
    [SerializeField] private int pendingLobbyLevel = 0;

    public int CurrentCombatLevel => Mathf.Clamp(currentCombatLevel, 1, maxCombatLevel);
    public int PendingLobbyLevel => Mathf.Clamp(pendingLobbyLevel > 0 ? pendingLobbyLevel : currentCombatLevel, 1, maxCombatLevel);
    public int MaxCombatLevel => maxCombatLevel;

    public bool IsLastCombatCleared => PendingLobbyLevel >= maxCombatLevel;
    public bool CanContinueFromLobby => PendingLobbyLevel < maxCombatLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentCombatLevel = Mathf.Clamp(currentCombatLevel, 1, maxCombatLevel);
        pendingLobbyLevel = Mathf.Clamp(pendingLobbyLevel, 0, maxCombatLevel);
    }

    public void StartNewRun(int startLevel = 1)
    {
        currentCombatLevel = Mathf.Clamp(startLevel, 1, maxCombatLevel);
        pendingLobbyLevel = 0;
    }

    public void EnterLobbyAfterCombat(int clearedCombatLevel)
    {
        int safeLevel = Mathf.Clamp(clearedCombatLevel, 1, maxCombatLevel);
        currentCombatLevel = safeLevel;
        pendingLobbyLevel = safeLevel;
    }

    public int AdvanceFromLobbyToNextCombat()
    {
        int baseLevel = PendingLobbyLevel;
        currentCombatLevel = Mathf.Clamp(baseLevel + 1, 1, maxCombatLevel);
        pendingLobbyLevel = 0;
        return currentCombatLevel;
    }

    public void LoadLobbyAfterCombat(int clearedCombatLevel)
    {
        EnterLobbyAfterCombat(clearedCombatLevel);
        SceneManager.LoadScene(lobbySceneName);
    }

    public void LoadNextCombatFromLobby()
    {
        if (!CanContinueFromLobby)
        {
            Debug.Log("RunLevelFlow: ultimul nivel a fost deja terminat.");
            return;
        }

        AdvanceFromLobbyToNextCombat();
        SceneManager.LoadScene(combatSceneName);
    }
}