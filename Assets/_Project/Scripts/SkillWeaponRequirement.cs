using UnityEngine;

public enum SkillWeaponRequirement
{
    [InspectorName("Any Weapon")]
    AnyWeapon = 0,

    [InspectorName("Melee Weapon")]
    MeleeWeapon = 1,

    [InspectorName("Ranged Weapon")]
    RangedWeapon = 2,

    [InspectorName("Magic Weapon")]
    MagicWeapon = 3
}