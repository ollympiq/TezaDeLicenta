using UnityEngine;

public enum CurrentLevelSource
{
    Manual = 0,
    CombatLevelFromRunFlow = 1,
    LobbyLevelFromRunFlow = 2
}

[DefaultExecutionOrder(-900)]
public class CurrentLevelContext : MonoBehaviour
{
    public static CurrentLevelContext Instance { get; private set; }

    [SerializeField] private CurrentLevelSource source = CurrentLevelSource.Manual;
    [SerializeField] private int currentLevel = 1;

    public int CurrentLevel => Mathf.Max(1, currentLevel);

    private void Awake()
    {
        Instance = this;
        RefreshFromRunFlow();
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        RefreshFromRunFlow();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshFromRunFlow()
    {
        if (source == CurrentLevelSource.Manual || RunLevelFlow.Instance == null)
        {
            currentLevel = Mathf.Max(1, currentLevel);
            return;
        }

        switch (source)
        {
            case CurrentLevelSource.CombatLevelFromRunFlow:
                currentLevel = RunLevelFlow.Instance.CurrentCombatLevel;
                break;

            case CurrentLevelSource.LobbyLevelFromRunFlow:
                currentLevel = RunLevelFlow.Instance.PendingLobbyLevel;
                break;

            default:
                currentLevel = Mathf.Max(1, currentLevel);
                break;
        }

        currentLevel = Mathf.Max(1, currentLevel);
    }

    public void SetCurrentLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
    }
}