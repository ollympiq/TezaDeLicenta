using UnityEngine;

public class CombatEntryHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private CharacterInventory characterInventory;
    [SerializeField] private CharacterEquipment characterEquipment;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private CharacterHealth characterHealth;

    private void Start()
    {
        ResolveReferences();

        if (GameSession.Instance == null)
            return;

        if (!GameSession.Instance.HasRestorablePlayerState)
            return;

        GameSession.Instance.LoadIntoPlayer(
            playerProgression,
            playerWallet,
            characterInventory,
            characterEquipment,
            playerSkillLoadout);

        GameSession.Instance.ApplySavedClassTo(characterStats);
        GameSession.Instance.ApplySavedPrimaryAttributesTo(characterStats);

        if (characterHealth != null)
            characterHealth.ResetToFull();
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

        if (characterHealth == null)
            characterHealth = FindFirstObjectByType<CharacterHealth>();
    }
}