using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string firstCombatSceneName = "Level01Scene";
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject chooseClassPanel;

    public void StartNewGame()
    {
        OpenChooseClass();
    }

    public void OpenChooseClass()
    {
        EnsurePersistentSessionExists();

        SetMainMenuVisible(false);
        SetSettingsVisible(false);
        SetChooseClassVisible(true);
    }

    public void CloseChooseClass()
    {
        SetChooseClassVisible(false);
        SetSettingsVisible(false);
        SetMainMenuVisible(true);
    }

    public void StartAsMelee()
    {
        BeginNewRunWithClass(CharacterClass.Melee);
    }

    public void StartAsRanger()
    {
        BeginNewRunWithClass(CharacterClass.Ranger);
    }

    public void StartAsMage()
    {
        BeginNewRunWithClass(CharacterClass.Mage);
    }

    public void OpenLobbyDebugWithLevelUp()
    {
        EnsurePersistentSessionExists();

        if (GameSession.Instance != null)
        {
            GameSession.Instance.BeginNewRun(1);
            GameSession.Instance.MarkCombatLevelCompleted(1);
        }

        if (RunLevelFlow.Instance != null)
        {
            RunLevelFlow.Instance.StartNewRun(1);
            RunLevelFlow.Instance.EnterLobbyAfterCombat(1);
        }

        SceneManager.LoadScene(lobbySceneName);
    }

    public void LoadSavePlaceholder()
    {
        EnsurePersistentSessionExists();

        Debug.Log("Load Save este placeholder momentan.");
        SceneManager.LoadScene(lobbySceneName);
    }

    public void OpenSettings()
    {
        SetMainMenuVisible(false);
        SetChooseClassVisible(false);
        SetSettingsVisible(true);
    }

    public void CloseSettings()
    {
        SetSettingsVisible(false);
        SetChooseClassVisible(false);
        SetMainMenuVisible(true);
    }

    public void ToggleSettings()
    {
        bool shouldOpen = settingsPanel == null || !settingsPanel.activeSelf;

        if (shouldOpen)
            OpenSettings();
        else
            CloseSettings();
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    private void BeginNewRunWithClass(CharacterClass classType)
    {
        EnsurePersistentSessionExists();

        if (GameSession.Instance != null)
        {
            GameSession.Instance.BeginNewRun(1);
            GameSession.Instance.SetSelectedPlayerClass(classType);
        }

        if (RunLevelFlow.Instance != null)
            RunLevelFlow.Instance.StartNewRun(1);

        SceneManager.LoadScene(firstCombatSceneName);
    }

    private void EnsurePersistentSessionExists()
    {
        if (GameSession.Instance != null)
            return;

        GameSession existing = FindFirstObjectByType<GameSession>();
        if (existing != null)
            return;

        GameObject go = new GameObject("GameSession");
        go.AddComponent<GameSession>();
        go.AddComponent<RunLevelFlow>();
    }

    private void SetMainMenuVisible(bool visible)
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(visible);
    }

    private void SetSettingsVisible(bool visible)
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(visible);
    }

    private void SetChooseClassVisible(bool visible)
    {
        if (chooseClassPanel != null)
            chooseClassPanel.SetActive(visible);
    }
}