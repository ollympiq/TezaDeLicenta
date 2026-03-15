using UnityEngine;

public static class DamageCalculator
{
    public static DamageResult ResolveAttack(
        CharacterStats attacker,
        CharacterStats defender,
        AttackDefinition attack)
    {
        DamageResult result = new DamageResult();
        result.DamageType = attack.DamageType;

        float hitChance = Mathf.Clamp(attacker.Accuracy + attack.BonusAccuracy - defender.Evasion, 5f, 95f);
        result.HitChance = hitChance;

        bool hit = Random.Range(0f, 100f) <= hitChance;
        result.Hit = hit;

        if (!hit)
        {
            result.FinalDamage = 0;
            return result;
        }

        int rolledDamage = Random.Range(attack.MinDamage, attack.MaxDamage + 1);

        float offensivePower = attack.DamageType == DamageType.Physical
            ? attacker.PhysicalPower
            : attacker.MagicPower;

        float rawDamage = rolledDamage + offensivePower * attack.PowerScaling;
        result.BaseDamage = Mathf.RoundToInt(rawDamage);

        bool wasCrit = attack.CanCrit && Random.Range(0f, 100f) <= attacker.CritChance;
        result.WasCritical = wasCrit;

        if (wasCrit)
            rawDamage *= 2f;

        float armorReduction = 0f;

        if (attack.DamageType == DamageType.Physical)
        {
            armorReduction = defender.ArmorPhysicalReductionPercent;
            rawDamage *= 1f - armorReduction / 100f;
        }

        result.ArmorReductionPercent = armorReduction;

        float resistance = GetResistance(defender, attack.DamageType);
        result.ResistancePercent = resistance;

        rawDamage *= 1f - resistance / 100f;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage));
        result.FinalDamage = finalDamage;

        return result;
    }

    private static float GetResistance(CharacterStats stats, DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return stats.PhysicalResistance;
            case DamageType.Fire:
                return stats.FireResistance;
            case DamageType.Earth:
                return stats.EarthResistance;
            case DamageType.Wind:
                return stats.WindResistance;
            case DamageType.Lightning:
                return stats.LightningResistance;
            case DamageType.Ice:
                return stats.IceResistance;
            default:
                return 0f;
        }
    }
}