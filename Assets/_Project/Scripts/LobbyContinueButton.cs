using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyContinueButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private LobbySceneController lobbySceneController;

    [Header("Labels")]
    [SerializeField] private string continueLabel = "Continue";
    [SerializeField] private string finishedLabel = "Finished";

    private Coroutine refreshRoutine;

    private void Awake()
    {
        if (continueButton == null)
            continueButton = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);

        ResolveReferences();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
            continueButton.onClick.AddListener(OnContinuePressed);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshVisualState();

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(RefreshAfterSceneLoad());
    }

    private void Start()
    {
        ResolveReferences();
        RefreshVisualState();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        yield return null;
        RefreshVisualState();

        yield return null;
        RefreshVisualState();

        refreshRoutine = null;
    }

    public void RefreshVisualState()
    {
        bool canContinue = RunLevelFlow.Instance != null && RunLevelFlow.Instance.CanContinueFromLobby;

        if (continueButton != null)
            continueButton.interactable = canContinue;

        if (buttonText != null)
            buttonText.text = canContinue ? continueLabel : finishedLabel;
    }

    public void OnContinuePressed()
    {
        ResolveReferences();

        if (RunLevelFlow.Instance == null)
        {
            Debug.LogWarning("LobbyContinueButton: RunLevelFlow lipseste.");
            return;
        }

        if (!RunLevelFlow.Instance.CanContinueFromLobby)
        {
            RefreshVisualState();
            return;
        }

        if (lobbySceneController != null)
        {
            lobbySceneController.ContinueToNextCombat();
            return;
        }

        Debug.LogWarning("LobbyContinueButton: LobbySceneController lipseste. Se trece in combat fara salvare.");
        RunLevelFlow.Instance.LoadNextCombatFromLobby();
    }

    private void ResolveReferences()
    {
        if (lobbySceneController == null)
            lobbySceneController = FindFirstObjectByType<LobbySceneController>();
    }
}