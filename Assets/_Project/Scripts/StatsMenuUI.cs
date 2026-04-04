using System.Text;
using TMPro;
using UnityEngine;

public class StatsMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStats targetStats;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Settings")]
    [SerializeField] private bool startOpened = false;

    private CharacterHealth targetHealth;
    private PlayerAP targetAP;

    private void Start()
    {
        if (targetStats == null)
            targetStats = FindFirstObjectByType<CharacterStats>();

        ResolveRuntimeReferences();
        SubscribeToEvents();

        if (panelRoot != null)
            panelRoot.SetActive(startOpened);

        Refresh();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    public void Refresh()
    {
        if (targetStats == null || statsText == null)
            return;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Class: {targetStats.Class}");
        sb.AppendLine($"Level: {targetStats.Level}");
        sb.AppendLine();

        if (targetHealth != null)
            sb.AppendLine($"HP: {targetHealth.CurrentHP}/{targetHealth.MaxHP}");
        else
            sb.AppendLine($"HP: {targetStats.MaxHP}/{targetStats.MaxHP}");

        if (targetAP != null)
            sb.AppendLine($"AP: {targetAP.CurrentAP}/{targetAP.MaxAP}");
        else
            sb.AppendLine($"AP: {targetStats.MaxAP}/{targetStats.MaxAP}");

        sb.AppendLine();

        sb.AppendLine($"Strength: {targetStats.Strength}");
        sb.AppendLine($"Constitution: {targetStats.Constitution}");
        sb.AppendLine($"Dexterity: {targetStats.Dexterity}");
        sb.AppendLine($"Intelligence: {targetStats.Intelligence}");
        sb.AppendLine();

        sb.AppendLine($"Max HP: {targetStats.MaxHP}");
        sb.AppendLine($"Max AP: {targetStats.MaxAP}");
        sb.AppendLine($"Physical Power: {targetStats.PhysicalPower}");
        sb.AppendLine($"Magic Power: {targetStats.MagicPower}");
        sb.AppendLine($"Crit Chance: {targetStats.CritChance:F1}%");
        sb.AppendLine($"Initiative: {targetStats.Initiative}");
        sb.AppendLine($"Accuracy: {targetStats.Accuracy:F1}%");
        sb.AppendLine($"Evasion: {targetStats.Evasion:F1}%");
        sb.AppendLine();

        sb.AppendLine($"Armor: {targetStats.Armor} ({targetStats.ArmorPhysicalReductionPercent:F1}% physical reduction)");
        sb.AppendLine($"Physical Resistance: {targetStats.PhysicalResistance:F1}%");
        sb.AppendLine($"Fire Resistance: {targetStats.FireResistance:F1}%");
        sb.AppendLine($"Earth Resistance: {targetStats.EarthResistance:F1}%");
        sb.AppendLine($"Wind Resistance: {targetStats.WindResistance:F1}%");
        sb.AppendLine($"Lightning Resistance: {targetStats.LightningResistance:F1}%");
        sb.AppendLine($"Ice Resistance: {targetStats.IceResistance:F1}%");

        statsText.text = sb.ToString();
    }

    public void SetTarget(CharacterStats newTarget)
    {
        UnsubscribeFromEvents();

        targetStats = newTarget;
        ResolveRuntimeReferences();
        SubscribeToEvents();
        Refresh();
    }

    private void ResolveRuntimeReferences()
    {
        if (targetStats == null)
        {
            targetHealth = null;
            targetAP = null;
            return;
        }

        targetHealth = targetStats.GetComponent<CharacterHealth>();
        targetAP = targetStats.GetComponent<PlayerAP>();
    }

    private void SubscribeToEvents()
    {
        if (targetStats != null)
            targetStats.OnStatsChanged += Refresh;

        if (targetHealth != null)
            targetHealth.OnHealthChanged += HandleHealthChanged;

        if (targetAP != null)
            targetAP.OnAPChanged += HandleAPChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (targetStats != null)
            targetStats.OnStatsChanged -= Refresh;

        if (targetHealth != null)
            targetHealth.OnHealthChanged -= HandleHealthChanged;

        if (targetAP != null)
            targetAP.OnAPChanged -= HandleAPChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        Refresh();
    }

    private void HandleAPChanged(int current, int max)
    {
        Refresh();
    }
}