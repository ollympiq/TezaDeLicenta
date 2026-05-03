using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbySceneController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string nextCombatSceneName = "Level01Scene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Panels")]
    [SerializeField] private GameObject statsPanelRoot;
    [SerializeField] private GameObject traderPanelRoot;

    [Header("Player Save References")]
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private CharacterInventory characterInventory;
    [SerializeField] private CharacterEquipment characterEquipment;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private CharacterStats characterStats;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    public void ContinueToNextCombat()
    {
        ResolveReferences();
        SaveLobbyPlayerState();

        if (RunLevelFlow.Instance != null)
        {
            RunLevelFlow.Instance.LoadNextCombatFromLobby();
            return;
        }

        SceneManager.LoadScene(nextCombatSceneName);
    }

    public void ReturnToMainMenu()
    {
        ResolveReferences();
        SaveLobbyPlayerState();
        SceneManager.LoadScene(mainMenuSceneName);
    }

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

    public void ToggleStatsPanel()
    {
        if (statsPanelRoot != null)
            statsPanelRoot.SetActive(!statsPanelRoot.activeSelf);
    }

    public void CloseTraderPanel()
    {
        if (traderPanelRoot != null)
            traderPanelRoot.SetActive(false);
    }

    public void ToggleTraderPanel()
    {
        if (traderPanelRoot != null)
            traderPanelRoot.SetActive(!traderPanelRoot.activeSelf);
    }

    private void SaveLobbyPlayerState()
    {
        if (GameSession.Instance == null)
            return;

        if (characterStats != null && characterStats.Class != CharacterClass.Unassigned)
            GameSession.Instance.RememberAppliedPlayerClass(characterStats.Class);

        GameSession.Instance.SaveFromPlayer(
            playerProgression,
            playerWallet,
            characterInventory,
            characterEquipment,
            playerSkillLoadout);
    }

    private void ResolveReferences()
    {
        if (playerProgression == null)
            playerProgression = FindFirstObjectByType<PlayerProgression>();

        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();

        if (characterInventory == null)
            characterInventory = FindFirstObjectByType<CharacterInventory>();

        if (characterEquipment == null)
            characterEquipment = FindFirstObjectByType<CharacterEquipment>();

        if (playerSkillLoadout == null)
            playerSkillLoadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (characterStats == null)
            characterStats = FindFirstObjectByType<CharacterStats>();
    }
}