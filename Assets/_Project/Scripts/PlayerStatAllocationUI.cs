using System.Collections;
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

    private PlayerProgression subscribedProgression;
    private CharacterStats subscribedStats;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        ResolveReferences();
    }

    private IEnumerator Start()
    {
        // Asteptam un frame, pentru ca playerul poate fi spawnat dinamic
        // si PlayerSceneReferenceBinder poate seta referintele dupa Awake/OnEnable.
        yield return null;

        ResolveReferences();
        SubscribeToEvents();
        RefreshNow();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEvents();
        RefreshNow();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    public void OpenPanel()
    {
        ResolveReferences();

        if (ShouldAutoClose())
        {
            ClosePanel();
            return;
        }

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
        ResolveReferences();

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

        if (ShouldAutoClose())
            ClosePanel();
    }

    private void TrySpend(PlayerStatType statType)
    {
        ResolveReferences();

        if (progression == null)
            return;

        bool spent = progression.SpendPoint(statType, 1);

        if (!spent)
        {
            GameLog.Warning("Nu mai ai puncte disponibile.");
            RefreshNow();
            return;
        }

        RefreshNow();

        if (ShouldAutoClose())
            ClosePanel();
    }

    private bool ShouldAutoClose()
    {
        return autoCloseWhenNoPointsRemain &&
               progression != null &&
               progression.UnspentStatPoints <= 0;
    }

    private void ResolveReferences()
    {
        if (progression == null)
            progression = PlayerRuntimeRegistry.Get<PlayerProgression>();

        if (progression == null)
            progression = FindFirstObjectByType<PlayerProgression>();

        if (stats == null)
            stats = PlayerRuntimeRegistry.Get<CharacterStats>();

        if (stats == null)
            stats = FindFirstObjectByType<CharacterStats>();
    }

    private void SubscribeToEvents()
    {
        if (subscribedProgression != progression)
        {
            if (subscribedProgression != null)
                subscribedProgression.OnProgressionChanged -= RefreshNow;

            subscribedProgression = progression;

            if (subscribedProgression != null)
                subscribedProgression.OnProgressionChanged += RefreshNow;
        }

        if (subscribedStats != stats)
        {
            if (subscribedStats != null)
                subscribedStats.OnStatsChanged -= RefreshNow;

            subscribedStats = stats;

            if (subscribedStats != null)
                subscribedStats.OnStatsChanged += RefreshNow;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (subscribedProgression != null)
            subscribedProgression.OnProgressionChanged -= RefreshNow;

        if (subscribedStats != null)
            subscribedStats.OnStatsChanged -= RefreshNow;

        subscribedProgression = null;
        subscribedStats = null;
    }
}