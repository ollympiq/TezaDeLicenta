using TMPro;
using UnityEngine;

public class APTextUI : MonoBehaviour
{
    [SerializeField] private PlayerAP playerAP;
    [SerializeField] private TextMeshProUGUI apText;

    private void Start()
    {
        if (playerAP == null || apText == null)
            return;

        UpdateAPText(playerAP.CurrentAP, playerAP.MaxAP);
        playerAP.OnAPChanged += UpdateAPText;
    }

    private void OnDestroy()
    {
        if (playerAP != null)
            playerAP.OnAPChanged -= UpdateAPText;
    }

    private void UpdateAPText(int current, int max)
    {
        apText.text = $"AP: {current}/{max}";
    }
}