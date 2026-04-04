using UnityEngine;
using UnityEngine.UI;

public class TurnOrderSlotUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image portraitImage;

    [Header("Colors")]
    [SerializeField] private Color currentColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);

    public void SetData(TurnManager.TurnActorPortraitData data, bool isCurrent)
    {
        if (backgroundImage != null)
            backgroundImage.color = isCurrent ? currentColor : normalColor;

        if (portraitImage != null)
        {
            portraitImage.sprite = data.Portrait;
            portraitImage.enabled = data.IsValid && data.Portrait != null;
        }
    }

    public void Clear()
    {
        if (backgroundImage != null)
            backgroundImage.color = emptyColor;

        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }
    }
}