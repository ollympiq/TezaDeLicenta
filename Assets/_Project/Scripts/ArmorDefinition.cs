using UnityEngine;

[CreateAssetMenu(fileName = "ArmorDefinition", menuName = "Game/Items/Armor Definition")]
public class ArmorDefinition : ItemDefinition
{
    [Header("Armor")]
    [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.Chest;
    [SerializeField] private int armorValue = 5;

    [Header("Resistances")]
    [SerializeField, Range(0f, 100f)] private float physicalResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float fireResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float earthResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float windResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float lightningResistance = 0f;
    [SerializeField, Range(0f, 100f)] private float iceResistance = 0f;

    public override ItemCategory Category => ItemCategory.Armor;
    public override EquipmentSlot EquipmentSlot => equipmentSlot;

    public int ArmorValue => Mathf.Max(0, armorValue);

    public float PhysicalResistance => physicalResistance;
    public float FireResistance => fireResistance;
    public float EarthResistance => earthResistance;
    public float WindResistance => windResistance;
    public float LightningResistance => lightningResistance;
    public float IceResistance => iceResistance;

    private void OnValidate()
    {
        armorValue = Mathf.Max(0, armorValue);
    }
}