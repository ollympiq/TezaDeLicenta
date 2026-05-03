using UnityEngine;

public class LobbyEntryHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private CharacterInventory characterInventory;
    [SerializeField] private CharacterEquipment characterEquipment;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private CharacterStats characterStats;
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

        if (characterInventory == null)
            characterInventory = FindFirstObjectByType<CharacterInventory>();

        if (characterEquipment == null)
            characterEquipment = FindFirstObjectByType<CharacterEquipment>();

        if (playerSkillLoadout == null)
            playerSkillLoadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (characterStats == null)
            characterStats = FindFirstObjectByType<CharacterStats>();

        if (statAllocationUI == null)
            statAllocationUI = FindFirstObjectByType<PlayerStatAllocationUI>(FindObjectsInactive.Include);

        if (GameSession.Instance != null)
        {
            GameSession.Instance.LoadIntoPlayer(
                playerProgression,
                playerWallet,
                characterInventory,
                characterEquipment,
                playerSkillLoadout);

            GameSession.Instance.ApplySavedClassTo(characterStats);

            if (GameSession.Instance.PendingLobbyLevelUp && playerProgression != null)
            {
                playerProgression.LevelUpOnce();
                GameSession.Instance.ConsumePendingLobbyLevelUp();

                GameSession.Instance.SaveFromPlayer(
                    playerProgression,
                    playerWallet,
                    characterInventory,
                    characterEquipment,
                    playerSkillLoadout);

                GameSession.Instance.ApplySavedClassTo(characterStats);
            }
        }

        if (autoOpenStatsPanel && statsPanelRoot != null)
            statsPanelRoot.SetActive(true);

        if (statAllocationUI != null)
            statAllocationUI.RefreshNow();
    }
}