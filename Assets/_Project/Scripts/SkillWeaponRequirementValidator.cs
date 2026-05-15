public static class SkillWeaponRequirementValidator
{
    public static bool CanUseSkill(SkillDefinition skill, WeaponDefinition equippedWeapon)
    {
        if (skill == null)
            return false;

        return CanUseRequirement(skill.RequiredWeapon, equippedWeapon);
    }

    public static bool CanUseRequirement(SkillWeaponRequirement requirement, WeaponDefinition equippedWeapon)
    {
        if (equippedWeapon == null)
            return false;

        WeaponFamily family = equippedWeapon.WeaponFamily;

        switch (requirement)
        {
            case SkillWeaponRequirement.AnyWeapon:
                return true;

            case SkillWeaponRequirement.MeleeWeapon:
                return IsMeleeWeapon(family);

            case SkillWeaponRequirement.RangedWeapon:
                return IsRangedWeapon(family);

            case SkillWeaponRequirement.MagicWeapon:
                return IsMagicWeapon(family);

            default:
                return false;
        }
    }

    public static string BuildRequirementMessage(SkillDefinition skill, WeaponDefinition equippedWeapon)
    {
        if (skill == null)
            return "Skill invalid.";

        string equippedText = equippedWeapon != null
            ? equippedWeapon.WeaponFamily.ToString()
            : "None";

        return $"Skill-ul {skill.DisplayName} necesita {GetRequirementDisplayName(skill.RequiredWeapon)}. Arma echipata: {equippedText}.";
    }

    public static string GetRequirementDisplayName(SkillWeaponRequirement requirement)
    {
        switch (requirement)
        {
            case SkillWeaponRequirement.AnyWeapon:
                return "orice arma";

            case SkillWeaponRequirement.MeleeWeapon:
                return "arma melee (Sword, Axe, Spear sau Dagger)";

            case SkillWeaponRequirement.RangedWeapon:
                return "arma ranged (Bow sau Crossbow)";

            case SkillWeaponRequirement.MagicWeapon:
                return "arma magic (Staff, Wand sau Spellblade)";

            default:
                return "arma valida";
        }
    }

    private static bool IsMeleeWeapon(WeaponFamily family)
    {
        switch (family)
        {
            case WeaponFamily.Sword:
            case WeaponFamily.Axe:
            case WeaponFamily.Spear:
            case WeaponFamily.Dagger:
                return true;

            default:
                return false;
        }
    }

    private static bool IsRangedWeapon(WeaponFamily family)
    {
        switch (family)
        {
            case WeaponFamily.Bow:
            case WeaponFamily.Crossbow:
                return true;

            default:
                return false;
        }
    }

    private static bool IsMagicWeapon(WeaponFamily family)
    {
        switch (family)
        {
            case WeaponFamily.Staff:
            case WeaponFamily.Wand:
            case WeaponFamily.Spellblade:
                return true;

            default:
                return false;
        }
    }
}