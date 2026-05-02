using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyContinueButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text buttonText;

    [Header("Labels")]
    [SerializeField] private string continueLabel = "Continue";
    [SerializeField] private string finishedLabel = "Finished";

    private void Awake()
    {
        if (continueButton == null)
            continueButton = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
    }

    private void OnEnable()
    {
        RefreshVisualState();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
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

        RunLevelFlow.Instance.LoadNextCombatFromLobby();
    }
}