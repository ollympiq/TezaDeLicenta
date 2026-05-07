using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCodexToggleButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private SkillCodexUI codexUI;

    [Header("Text")]
    [SerializeField] private string label = "Skills";

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);

        if (codexUI == null)
            codexUI = FindFirstObjectByType<SkillCodexUI>(FindObjectsInactive.Include);

        if (buttonText != null)
            buttonText.text = label;

        if (button != null)
        {
            button.onClick.RemoveListener(OnPressed);
            button.onClick.AddListener(OnPressed);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnPressed);
    }

    private void OnPressed()
    {
        if (codexUI == null)
        {
            codexUI = FindFirstObjectByType<SkillCodexUI>(FindObjectsInactive.Include);
        }

        if (codexUI != null)
            codexUI.Toggle();
    }
}