using System.Text;

public struct DamageResult
{
    public DamageType DamageType;

    public bool Hit;
    public float HitChance;

    public int RolledDamage;
    public float ScalingBonus;

    public float ClassBonusPercent;
    public float ElementalBonusPercent;

    public int PreCritDamage;

    public bool WasCritical;
    public float CritMultiplier;
    public int PostCritDamage;

    public float ArmorReductionPercent;
    public int PostArmorDamage;

    public float ResistancePercent;
    public int FinalDamage;

    public string BuildLogLine(string attackerName, string attackName, string targetName)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(attackerName);
        sb.Append(" used ");
        sb.Append(attackName);
        sb.Append(" on ");
        sb.Append(targetName);
        sb.Append(" | Type: ");
        sb.Append(DamageType);

        if (!Hit)
        {
            sb.Append(" | MISSED");
            sb.Append(" | HitChance: ");
            sb.Append(HitChance.ToString("F1"));
            sb.Append('%');
            return sb.ToString();
        }

        sb.Append(" | Roll: ");
        sb.Append(RolledDamage);

        if (ScalingBonus > 0.01f)
        {
            sb.Append(" | Scaling: +");
            sb.Append(ScalingBonus.ToString("F1"));
        }

        if (ClassBonusPercent > 0.01f)
        {
            sb.Append(" | Class/Weapon Bonus: +");
            sb.Append(ClassBonusPercent.ToString("F1"));
            sb.Append('%');
        }

        if (ElementalBonusPercent > 0.01f)
        {
            sb.Append(" | Elemental Bonus: +");
            sb.Append(ElementalBonusPercent.ToString("F1"));
            sb.Append('%');
        }

        sb.Append(" | PreCrit: ");
        sb.Append(PreCritDamage);

        if (WasCritical)
        {
            sb.Append(" | Crit x");
            sb.Append(CritMultiplier.ToString("F1"));
            sb.Append(" => ");
            sb.Append(PostCritDamage);
        }

        if (ArmorReductionPercent > 0.01f)
        {
            sb.Append(" | Armor: -");
            sb.Append(ArmorReductionPercent.ToString("F1"));
            sb.Append('%');
            sb.Append(" => ");
            sb.Append(PostArmorDamage);
        }

        if (ResistancePercent > 0.01f)
        {
            sb.Append(" | Resistance: -");
            sb.Append(ResistancePercent.ToString("F1"));
            sb.Append('%');
        }

        sb.Append(" | Final: ");
        sb.Append(FinalDamage);

        return sb.ToString();
    }
}