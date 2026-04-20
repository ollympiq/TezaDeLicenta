using UnityEngine;

public class LobbyEntryHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private GameObject statsPanelRoot;
    [SerializeField] private PlayerStatAllocationUI statAllocationUI;

    [Header("Behavior")]
    [SerializeField] private bool autoOpenStatsPanel = true;

    private void Start()
    {
        if (playerProgression == null)
            playerProgression = FindFirstObjectByType<PlayerProgression>();

        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();

        if (statAllocationUI == null)
            statAllocationUI = FindFirstObjectByType<PlayerStatAllocationUI>(FindObjectsInactive.Include);

        if (GameSession.Instance != null)
        {
            GameSession.Instance.LoadIntoPlayer(playerProgression, playerWallet);

            if (GameSession.Instance.PendingLobbyLevelUp && playerProgression != null)
            {
                playerProgression.LevelUpOnce();
                GameSession.Instance.ConsumePendingLobbyLevelUp();
                GameSession.Instance.SaveFromPlayer(playerProgression, playerWallet);
            }
        }

        if (autoOpenStatsPanel && statsPanelRoot != null)
            statsPanelRoot.SetActive(true);

        if (statAllocationUI != null)
            statAllocationUI.RefreshNow();
    }
}