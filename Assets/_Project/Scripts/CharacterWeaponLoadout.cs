using System;
using UnityEngine;

public class CharacterWeaponLoadout : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponDefinition startingWeapon;
    [SerializeField] private WeaponDefinition equippedWeapon;

    public event Action<WeaponDefinition> OnWeaponChanged;

    public WeaponDefinition EquippedWeapon => equippedWeapon;

    private void Awake()
    {
        if (equippedWeapon == null)
            equippedWeapon = startingWeapon;
    }

    public void EquipWeapon(WeaponDefinition newWeapon)
    {
        if (newWeapon == null)
            return;

        equippedWeapon = newWeapon;
        OnWeaponChanged?.Invoke(equippedWeapon);
    }

    public void ForceRefresh()
    {
        OnWeaponChanged?.Invoke(equippedWeapon);
    }
}