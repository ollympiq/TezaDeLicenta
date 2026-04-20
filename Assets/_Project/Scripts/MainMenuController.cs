using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string firstCombatSceneName = "Level01Scene";
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    public void StartNewGame()
    {
        EnsureGameSessionExists();

        GameSession.Instance.BeginNewRun(1);
        SceneManager.LoadScene(firstCombatSceneName);
    }

    public void OpenLobbyDebugWithLevelUp()
    {
        EnsureGameSessionExists();

        GameSession.Instance.BeginNewRun(1);
        GameSession.Instance.MarkCombatLevelCompleted(1);

        SceneManager.LoadScene(lobbySceneName);
    }

    public void LoadSavePlaceholder()
    {
        EnsureGameSessionExists();

        Debug.Log("Load Save este doar placeholder momentan. Il poti lega mai tarziu la sistemul real de save.");
        SceneManager.LoadScene(lobbySceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    private void EnsureGameSessionExists()
    {
        if (GameSession.Instance != null)
            return;

        GameObject go = new GameObject("GameSession");
        go.AddComponent<GameSession>();
    }
}