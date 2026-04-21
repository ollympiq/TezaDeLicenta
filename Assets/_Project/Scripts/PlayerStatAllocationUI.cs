using TMPro;
using UnityEngine;

public class PlayerStatAllocationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProgression progression;
    [SerializeField] private CharacterStats stats;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool autoCloseWhenNoPointsRemain = true;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI constitutionText;
    [SerializeField] private TextMeshProUGUI dexterityText;
    [SerializeField] private TextMeshProUGUI intelligenceText;

    private void Awake()
    {
        if (progression == null)
            progression = FindFirstObjectByType<PlayerProgression>();

        if (stats == null)
            stats = FindFirstObjectByType<CharacterStats>();

        if (panelRoot == null)
            panelRoot = gameObject;
    }

    private void OnEnable()
    {
        if (progression != null)
            progression.OnProgressionChanged += RefreshNow;

        if (stats != null)
            stats.OnStatsChanged += RefreshNow;

        RefreshNow();
    }

    private void OnDisable()
    {
        if (progression != null)
            progression.OnProgressionChanged -= RefreshNow;

        if (stats != null)
            stats.OnStatsChanged -= RefreshNow;
    }

    public void OpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshNow();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void AddStrength()
    {
        TrySpend(PlayerStatType.Strength);
    }

    public void AddConstitution()
    {
        TrySpend(PlayerStatType.Constitution);
    }

    public void AddDexterity()
    {
        TrySpend(PlayerStatType.Dexterity);
    }

    public void AddIntelligence()
    {
        TrySpend(PlayerStatType.Intelligence);
    }

    public void RefreshNow()
    {
        if (progression != null)
        {
            if (levelText != null)
                levelText.text = $"Level: {progression.CurrentLevel}";

            if (availablePointsText != null)
                availablePointsText.text = $"Points: {progression.UnspentStatPoints}";
        }

        if (stats != null)
        {
            if (strengthText != null)
                strengthText.text = stats.Strength.ToString();

            if (constitutionText != null)
                constitutionText.text = stats.Constitution.ToString();

            if (dexterityText != null)
                dexterityText.text = stats.Dexterity.ToString();

            if (intelligenceText != null)
                intelligenceText.text = stats.Intelligence.ToString();
        }
    }

    private void TrySpend(PlayerStatType statType)
    {
        if (progression == null)
            return;

        bool spent = progression.SpendPoint(statType, 1);
        if (!spent)
        {
            Debug.Log("Nu mai ai puncte disponibile.");
            return;
        }

        RefreshNow();

        if (autoCloseWhenNoPointsRemain && progression.UnspentStatPoints <= 0)
            ClosePanel();
    }
}