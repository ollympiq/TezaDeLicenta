using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TraderShopUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("References")]
    [SerializeField] private PlayerWallet playerWallet;

    [Header("Behavior")]
    [SerializeField] private bool closeOnEscape = true;

    private TraderInteractable currentTrader;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public TraderInteractable CurrentTrader => currentTrader;

    private void Awake()
    {
        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();

        Close();
    }

    private void OnEnable()
    {
        if (playerWallet != null)
            playerWallet.OnGoldChanged += RefreshGoldDisplay;
    }

    private void OnDisable()
    {
        if (playerWallet != null)
            playerWallet.OnGoldChanged -= RefreshGoldDisplay;
    }

    private void Update()
    {
        if (!IsOpen || !closeOnEscape || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void OpenForTrader(TraderInteractable trader)
    {
        currentTrader = trader;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshTitle();
        RefreshGoldDisplay();
    }

    public void Close()
    {
        currentTrader = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void RefreshGoldDisplay()
    {
        if (goldText == null)
            return;

        int gold = playerWallet != null ? playerWallet.CurrentGold : 0;
        goldText.text = $"Gold: {gold}";
    }

    private void RefreshTitle()
    {
        if (titleText == null)
            return;

        string traderName = currentTrader != null ? currentTrader.TraderDisplayName : "Trader";
        titleText.text = $"{traderName} Shop";
    }
}