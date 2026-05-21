using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatExitToLobby : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("References")]
    [SerializeField] private CurrentLevelContext currentLevelContext;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private CharacterInventory characterInventory;
    [SerializeField] private CharacterEquipment characterEquipment;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;

    [Header("Button State")]
    [SerializeField] private Button lobbyButton;
    [SerializeField] private CanvasGroup buttonCanvasGroup;
    [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float unlockedAlpha = 1f;

    private TurnManager subscribedTurnManager;

    private void Awake()
    {
        if (lobbyButton == null)
            lobbyButton = GetComponent<Button>();

        if (buttonCanvasGroup == null)
            buttonCanvasGroup = GetComponent<CanvasGroup>();

        if (buttonCanvasGroup == null)
            buttonCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetButtonUnlocked(false);
    }

    private void OnEnable()
    {
        StartCoroutine(BindTurnManagerDelayed());
    }

    private void OnDisable()
    {
        UnsubscribeTurnManager();
    }

    private IEnumerator BindTurnManagerDelayed()
    {
        yield return null;

        SubscribeTurnManager();
        RefreshButtonState();
    }

    public void GoToLobby()
    {
        if (!CanGoToLobby())
        {
            GameLog.Warning("Trebuie sa elimini toti inamicii inainte de a merge in lobby.");
            RefreshButtonState();
            return;
        }

        ResolveReferences();

        int completedLevel = currentLevelContext != null ? currentLevelContext.CurrentLevel : 1;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.SaveFromPlayer(
                playerProgression,
                playerWallet,
                characterInventory,
                characterEquipment,
                playerSkillLoadout);

            GameSession.Instance.MarkCombatLevelCompleted(completedLevel);
        }

        if (RunLevelFlow.Instance != null)
        {
            RunLevelFlow.Instance.LoadLobbyAfterCombat(completedLevel);
            return;
        }

        SceneManager.LoadScene(lobbySceneName);
    }

    private void SubscribeTurnManager()
    {
        if (subscribedTurnManager == TurnManager.Instance)
            return;

        UnsubscribeTurnManager();

        subscribedTurnManager = TurnManager.Instance;

        if (subscribedTurnManager != null)
            subscribedTurnManager.OnTurnStateChanged += RefreshButtonState;
    }

    private void UnsubscribeTurnManager()
    {
        if (subscribedTurnManager != null)
            subscribedTurnManager.OnTurnStateChanged -= RefreshButtonState;

        subscribedTurnManager = null;
    }

    private void RefreshButtonState()
    {
        SubscribeTurnManager();
        SetButtonUnlocked(CanGoToLobby());
    }

    private bool CanGoToLobby()
    {
        if (TurnManager.Instance != null)
            return TurnManager.Instance.CanExitToLobby;

        return !HasAliveEnemiesFallback();
    }

    private bool HasAliveEnemiesFallback()
    {
        EnemyTurnController[] enemies = FindObjectsByType<EnemyTurnController>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            CharacterHealth health = enemies[i].GetComponent<CharacterHealth>();

            if (health != null && !health.IsDead)
                return true;
        }

        return false;
    }

    private void SetButtonUnlocked(bool unlocked)
    {
        if (lobbyButton != null)
            lobbyButton.interactable = unlocked;

        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = unlocked ? unlockedAlpha : lockedAlpha;
            buttonCanvasGroup.interactable = unlocked;
            buttonCanvasGroup.blocksRaycasts = true;
        }
    }

    private void ResolveReferences()
    {
        if (currentLevelContext == null)
            currentLevelContext = FindFirstObjectByType<CurrentLevelContext>();

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
    }
}