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

        sb.AppendLine(UIRichTextColors.DualLine("Class", targetStats.Class.ToString(), UIRichTextColors.White, UIRichTextColors.ClassColor(targetStats.Class)));
        sb.AppendLine(UIRichTextColors.Line("Level", targetStats.Level.ToString(), UIRichTextColors.Level));
        sb.AppendLine();

        if (targetHealth != null)
            sb.AppendLine(UIRichTextColors.Line("HP", $"{targetHealth.CurrentHP}/{targetHealth.MaxHP}", UIRichTextColors.HP));
        else
            sb.AppendLine(UIRichTextColors.Line("HP", $"{targetStats.MaxHP}/{targetStats.MaxHP}", UIRichTextColors.HP));

        if (targetAP != null)
            sb.AppendLine(UIRichTextColors.Line("AP", $"{targetAP.CurrentAP}/{targetAP.MaxAP}", UIRichTextColors.AP));
        else
            sb.AppendLine(UIRichTextColors.Line("AP", $"{targetStats.MaxAP}/{targetStats.MaxAP}", UIRichTextColors.AP));

        sb.AppendLine();

        sb.AppendLine(UIRichTextColors.Line("Strength", $"{targetStats.Strength}", UIRichTextColors.Strength));
        sb.AppendLine(UIRichTextColors.Line("Constitution", $"{targetStats.Constitution}", UIRichTextColors.Constitution));
        sb.AppendLine(UIRichTextColors.Line("Dexterity", $"{targetStats.Dexterity}", UIRichTextColors.Dexterity));
        sb.AppendLine(UIRichTextColors.Line("Intelligence", $"{targetStats.Intelligence}", UIRichTextColors.Intelligence));
        sb.AppendLine();

        sb.AppendLine(UIRichTextColors.Line("Max HP", $"{targetStats.MaxHP}", UIRichTextColors.HP));
        sb.AppendLine(UIRichTextColors.Line("Max AP", $"{targetStats.MaxAP}", UIRichTextColors.AP));
        sb.AppendLine(UIRichTextColors.Line("Physical Power", $"{targetStats.PhysicalPower}", UIRichTextColors.PhysicalPower));
        sb.AppendLine(UIRichTextColors.Line("Magic Power", $"{targetStats.MagicPower}", UIRichTextColors.MagicPower));
        sb.AppendLine(UIRichTextColors.Line("Crit Chance", $"{targetStats.CritChance:F1}%", UIRichTextColors.Crit));
        sb.AppendLine(UIRichTextColors.Line("Initiative", $"{targetStats.Initiative}", UIRichTextColors.Initiative));
        sb.AppendLine(UIRichTextColors.Line("Accuracy", $"{targetStats.Accuracy:F1}%", UIRichTextColors.Accuracy));
        sb.AppendLine(UIRichTextColors.Line("Evasion", $"{targetStats.Evasion:F1}%", UIRichTextColors.Evasion));
        sb.AppendLine();

        sb.AppendLine(
            $"{UIRichTextColors.Paint("Armor", UIRichTextColors.Armor)}: " +
            $"{UIRichTextColors.Paint(targetStats.Armor.ToString(), UIRichTextColors.Armor)} " +
            $"({UIRichTextColors.Paint($"{targetStats.ArmorPhysicalReductionPercent:F1}% physical reduction", UIRichTextColors.Armor)})"
        );

        sb.AppendLine(UIRichTextColors.Line("Physical Resistance", $"{targetStats.PhysicalResistance:F1}%", UIRichTextColors.Physical));
        sb.AppendLine(UIRichTextColors.Line("Fire Resistance", $"{targetStats.FireResistance:F1}%", UIRichTextColors.Fire));
        sb.AppendLine(UIRichTextColors.Line("Earth Resistance", $"{targetStats.EarthResistance:F1}%", UIRichTextColors.Earth));
        sb.AppendLine(UIRichTextColors.Line("Wind Resistance", $"{targetStats.WindResistance:F1}%", UIRichTextColors.Wind));
        sb.AppendLine(UIRichTextColors.Line("Lightning Resistance", $"{targetStats.LightningResistance:F1}%", UIRichTextColors.Lightning));
        sb.AppendLine(UIRichTextColors.Line("Ice Resistance", $"{targetStats.IceResistance:F1}%", UIRichTextColors.Ice));

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