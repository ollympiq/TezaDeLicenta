using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Run State")]
    [SerializeField] private int currentCombatLevel = 1;
    [SerializeField] private bool pendingLobbyLevelUp = false;

    [Header("Player Persistent Data")]
    [SerializeField] private int savedPlayerLevel = 1;
    [SerializeField] private int savedUnspentStatPoints = 0;
    [SerializeField] private int savedGold = 0;

    public int CurrentCombatLevel => Mathf.Max(1, currentCombatLevel);
    public bool PendingLobbyLevelUp => pendingLobbyLevelUp;

    public int SavedPlayerLevel => Mathf.Max(1, savedPlayerLevel);
    public int SavedUnspentStatPoints => Mathf.Max(0, savedUnspentStatPoints);
    public int SavedGold => Mathf.Max(0, savedGold);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRunData()
    {
        currentCombatLevel = 1;
        pendingLobbyLevelUp = false;

        savedPlayerLevel = 1;
        savedUnspentStatPoints = 0;
        savedGold = 0;
    }

    public void BeginNewRun(int startingCombatLevel = 1)
    {
        ResetRunData();
        currentCombatLevel = Mathf.Max(1, startingCombatLevel);
    }

    public void MarkCombatLevelCompleted(int completedLevel)
    {
        currentCombatLevel = Mathf.Max(currentCombatLevel, completedLevel);
        pendingLobbyLevelUp = true;
    }

    public void ConsumePendingLobbyLevelUp()
    {
        pendingLobbyLevelUp = false;
    }

    public void SaveFromPlayer(PlayerProgression progression, PlayerWallet wallet)
    {
        if (progression != null)
        {
            savedPlayerLevel = progression.CurrentLevel;
            savedUnspentStatPoints = progression.UnspentStatPoints;
        }

        if (wallet != null)
            savedGold = wallet.CurrentGold;
    }

    public void LoadIntoPlayer(PlayerProgression progression, PlayerWallet wallet)
    {
        if (progression != null)
            progression.SetProgressionState(savedPlayerLevel, savedUnspentStatPoints);

        if (wallet != null)
            wallet.SetGold(savedGold);
    }

    public void SetCombatLevel(int level)
    {
        currentCombatLevel = Mathf.Max(1, level);
    }
}