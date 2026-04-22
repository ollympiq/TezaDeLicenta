using TMPro;
using UnityEngine;

public class PlayerGoldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private TextMeshProUGUI goldText;

    private void Awake()
    {
        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();
    }

    private void OnEnable()
    {
        if (playerWallet != null)
            playerWallet.OnGoldChanged += RefreshNow;

        RefreshNow();
    }

    private void OnDisable()
    {
        if (playerWallet != null)
            playerWallet.OnGoldChanged -= RefreshNow;
    }

    public void RefreshNow()
    {
        int currentGold = playerWallet != null ? playerWallet.CurrentGold : 0;

        if (goldText != null)
            goldText.text = currentGold.ToString();
    }
}