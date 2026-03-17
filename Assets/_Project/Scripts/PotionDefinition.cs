using UnityEngine;

[CreateAssetMenu(fileName = "PotionDefinition", menuName = "Game/Items/Potion Definition")]
public class PotionDefinition : ItemDefinition
{
    [Header("Potion Effects")]
    [SerializeField] private int healAmount = 0;
    [SerializeField] private int restoreAP = 0;

    public override ItemCategory Category => ItemCategory.Consumable;

    public int HealAmount => Mathf.Max(0, healAmount);
    public int RestoreAP => Mathf.Max(0, restoreAP);
}