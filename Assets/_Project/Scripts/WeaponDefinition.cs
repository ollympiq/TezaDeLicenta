using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Items/Weapon Definition")]
public class WeaponDefinition : ItemDefinition
{
    [Header("Weapon")]
    [SerializeField] private WeaponFamily weaponFamily = WeaponFamily.Sword;

    [Header("Combat")]
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private int minDamage = 10;
    [SerializeField] private int maxDamage = 16;
    [SerializeField] private float range = 2.2f;
    [SerializeField] private int apCost = 2;
    [SerializeField, Range(0f, 100f)] private float bonusAccuracy = 0f;
    [SerializeField] private bool canCrit = true;

    [Header("Scaling")]
    [SerializeField] private AttributeScalingProfile scaling = new AttributeScalingProfile();

    public override ItemCategory Category => ItemCategory.Weapon;
    public override EquipmentSlot EquipmentSlot => EquipmentSlot.Weapon;

    public WeaponFamily WeaponFamily => weaponFamily;
    public DamageType DamageType => damageType;
    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public float Range => range;
    public int ApCost => apCost;
    public float BonusAccuracy => bonusAccuracy;
    public bool CanCrit => canCrit;
    public AttributeScalingProfile Scaling => scaling;

    private void OnValidate()
    {
        if (maxDamage < minDamage)
            maxDamage = minDamage;

        if (range < 0f)
            range = 0f;

        if (apCost < 0)
            apCost = 0;
    }
}