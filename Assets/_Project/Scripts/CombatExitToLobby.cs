using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatExitToLobby : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("References")]
    [SerializeField] private CurrentLevelContext currentLevelContext;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerWallet playerWallet;

    public void GoToLobby()
    {
        if (currentLevelContext == null)
            currentLevelContext = FindFirstObjectByType<CurrentLevelContext>();

        if (playerProgression == null)
            playerProgression = FindFirstObjectByType<PlayerProgression>();

        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();

        if (GameSession.Instance != null)
        {
            int completedLevel = currentLevelContext != null ? currentLevelContext.CurrentLevel : 1;

            GameSession.Instance.SaveFromPlayer(playerProgression, playerWallet);
            GameSession.Instance.MarkCombatLevelCompleted(completedLevel);
        }

        SceneManager.LoadScene(lobbySceneName);
    }
}