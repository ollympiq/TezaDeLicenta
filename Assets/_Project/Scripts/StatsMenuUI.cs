using System.Text;
using TMPro;
using UnityEngine;

public class StatsMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStats targetStats;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
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
        if (targetStats == null)
        {
            if (titleText != null)
                titleText.text = "Unknown";

            if (statsText != null)
                statsText.text = string.Empty;

            return;
        }

        if (titleText != null)
            titleText.text = GetDisplayName(targetStats.gameObject);

        if (statsText == null)
            return;

        StringBuilder sb = new StringBuilder();

        string typeLabel = GetTargetTypeLabel(targetStats.gameObject);
        if (!string.IsNullOrEmpty(typeLabel))
        {
            sb.AppendLine(UIRichTextColors.DualLine("Type", typeLabel, UIRichTextColors.White, UIRichTextColors.White));
            sb.AppendLine();
        }

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

        sb.AppendLine(UIRichTextColors.Line("Elemental Bonus (All)", $"{targetStats.ElementalDamageBonusPercent:F1}%", UIRichTextColors.MagicPower));
        sb.AppendLine(UIRichTextColors.Line("Fire Damage Bonus", $"{targetStats.FireDamageBonusPercent:F1}%", UIRichTextColors.Fire));
        sb.AppendLine(UIRichTextColors.Line("Earth Damage Bonus", $"{targetStats.EarthDamageBonusPercent:F1}%", UIRichTextColors.Earth));
        sb.AppendLine(UIRichTextColors.Line("Wind Damage Bonus", $"{targetStats.WindDamageBonusPercent:F1}%", UIRichTextColors.Wind));
        sb.AppendLine(UIRichTextColors.Line("Lightning Damage Bonus", $"{targetStats.LightningDamageBonusPercent:F1}%", UIRichTextColors.Lightning));
        sb.AppendLine(UIRichTextColors.Line("Ice Damage Bonus", $"{targetStats.IceDamageBonusPercent:F1}%", UIRichTextColors.Ice));
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

    private string GetDisplayName(GameObject targetObject)
    {
        if (targetObject == null)
            return "Unknown";

        string rawName = targetObject.name;
        if (string.IsNullOrWhiteSpace(rawName))
            return "Unknown";

        string name = rawName.Replace("(Clone)", "").Trim();

        string[] parts = name.Split('_');
        if (parts.Length >= 3)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 2; i < parts.Length; i++)
            {
                if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(parts[i]);
            }

            name = sb.ToString();
        }

        name = name.Replace('_', ' ').Trim();

        if (name.EndsWith(" MB"))
            name = name.Substring(0, name.Length - 3).Trim();

        if (name.EndsWith(" Boss"))
            name = name.Substring(0, name.Length - 5).Trim();

        return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
    }

    private string GetTargetTypeLabel(GameObject targetObject)
    {
        if (targetObject == null)
            return string.Empty;

        EnemyLootContainer lootContainer = targetObject.GetComponent<EnemyLootContainer>();
        if (lootContainer == null)
            return string.Empty;

        switch (lootContainer.LootTier)
        {
            case EnemyLootTier.MiniBoss:
                return "Mini Boss";
            case EnemyLootTier.Boss:
                return "Boss";
            default:
                return "Normal";
        }
    }
}