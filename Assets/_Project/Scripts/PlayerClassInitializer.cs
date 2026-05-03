using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClassInitializer : MonoBehaviour
{
    [Serializable]
    public class ClassPreset
    {
        public CharacterClass classType;

        [Header("Base Attributes")]
        public int strength = 10;
        public int constitution = 10;
        public int dexterity = 10;
        public int intelligence = 10;

        [Header("Start Loadout")]
        public WeaponDefinition startingWeapon;
        public SkillDefinition startingSkill1;
        public SkillDefinition startingSkill2;

        [Header("Common Start Consumables")]
        public PotionDefinition healthPotion;
        public int healthPotionAmount = 3;
        public PotionDefinition apPotion;
        public int apPotionAmount = 2;

        [Header("Start Economy")]
        public int startingGold = 100;
    }

    [Header("References")]
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private CharacterHealth characterHealth;
    [SerializeField] private CharacterEquipment characterEquipment;
    [SerializeField] private CharacterInventory characterInventory;
    [SerializeField] private PlayerSkillLoadout playerSkillLoadout;
    [SerializeField] private PlayerWallet playerWallet;

    [Header("Class Presets")]
    [SerializeField] private List<ClassPreset> presets = new List<ClassPreset>();

    private void Awake()
    {
        if (characterStats == null)
            characterStats = FindFirstObjectByType<CharacterStats>();

        if (characterHealth == null)
            characterHealth = FindFirstObjectByType<CharacterHealth>();

        if (characterEquipment == null)
            characterEquipment = FindFirstObjectByType<CharacterEquipment>();

        if (characterInventory == null)
            characterInventory = FindFirstObjectByType<CharacterInventory>();

        if (playerSkillLoadout == null)
            playerSkillLoadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();
    }

    private void Start()
    {
        if (GameSession.Instance == null)
            return;

        if (!GameSession.Instance.TryConsumeSelectedPlayerClass(out CharacterClass selectedClass))
            return;

        ClassPreset preset = GetPreset(selectedClass);
        if (preset == null)
        {
            Debug.LogWarning("Nu exista preset pentru clasa " + selectedClass);
            return;
        }

        ApplyPreset(preset);
    }

    private ClassPreset GetPreset(CharacterClass classType)
    {
        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i] != null && presets[i].classType == classType)
                return presets[i];
        }

        return null;
    }

    private void ApplyPreset(ClassPreset preset)
    {
        if (characterStats != null)
        {
            characterStats.ApplyLevel1Defaults();
            characterStats.SetCharacterClass(preset.classType, false);
            characterStats.SetBaseAttributes(
                preset.strength,
                preset.constitution,
                preset.dexterity,
                preset.intelligence,
                true);
        }

        if (GameSession.Instance != null)
            GameSession.Instance.RememberAppliedPlayerClass(preset.classType);

        if (characterEquipment != null)
        {
            characterEquipment.ClearAllEquipped(false);

            if (preset.startingWeapon != null)
                characterEquipment.EquipDefinition(preset.startingWeapon);

            characterEquipment.ForceRefresh();
        }

        if (characterInventory != null)
        {
            characterInventory.ClearAll();

            if (preset.healthPotion != null && preset.healthPotionAmount > 0)
                characterInventory.AddItem(preset.healthPotion, preset.healthPotionAmount);

            if (preset.apPotion != null && preset.apPotionAmount > 0)
                characterInventory.AddItem(preset.apPotion, preset.apPotionAmount);
        }

        if (playerSkillLoadout != null)
        {
            playerSkillLoadout.ResetToDefaultState();

            if (preset.startingSkill1 != null)
                playerSkillLoadout.LearnSkill(preset.startingSkill1, false);

            if (preset.startingSkill2 != null)
                playerSkillLoadout.LearnSkill(preset.startingSkill2, false);

            playerSkillLoadout.NotifyChanged();
        }

        if (playerWallet != null)
            playerWallet.SetGold(Mathf.Max(0, preset.startingGold));

        if (characterHealth != null)
            characterHealth.ResetToFull();
    }
}