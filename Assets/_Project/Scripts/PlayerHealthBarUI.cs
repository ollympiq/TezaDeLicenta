using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterHealth targetHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    private void Start()
    {
        if (targetHealth == null)
            targetHealth = FindFirstObjectByType<CharacterHealth>();

        if (targetHealth != null)
            targetHealth.OnHealthChanged += HandleHealthChanged;

        Refresh();
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (targetHealth == null)
            return;

        int current = targetHealth.CurrentHP;
        int max = targetHealth.MaxHP;

        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
            hpText.text = $"{current} / {max}";
    }
}