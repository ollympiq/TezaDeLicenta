using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbySceneController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string nextCombatSceneName = "Level01Scene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Panels")]
    [SerializeField] private GameObject statsPanelRoot;
    [SerializeField] private GameObject traderPanelRoot;

    public void OpenStatsPanel()
    {
        if (statsPanelRoot != null)
            statsPanelRoot.SetActive(true);
    }

    public void CloseStatsPanel()
    {
        if (statsPanelRoot != null)
            statsPanelRoot.SetActive(false);
    }

    public void OpenTraderPanel()
    {
        if (traderPanelRoot != null)
            traderPanelRoot.SetActive(true);
    }

    public void CloseTraderPanel()
    {
        if (traderPanelRoot != null)
            traderPanelRoot.SetActive(false);
    }

    public void GoToNextCombat()
    {
        SceneManager.LoadScene(nextCombatSceneName);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}