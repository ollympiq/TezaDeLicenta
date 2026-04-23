using System;
using System.Collections.Generic;
using UnityEngine;

public class GameSession : MonoBehaviour
{

    [SerializeField] private CharacterClass selectedPlayerClass = CharacterClass.Unassigned;
    [SerializeField] private bool pendingClassSelection = false;

    public CharacterClass SelectedPlayerClass => selectedPlayerClass;
    public bool HasPendingClassSelection => pendingClassSelection;

    [Serializable]
    public class SavedRolledModifier
    {
        public ItemBonusType bonusType;
        public float value;
    }

    [Serializable]
    public class SavedItemInstance
    {
        public string itemId;
        public ItemRarity rarity;
        public int stackCount;

        public int itemLevel;
        public int rolledMinDamage = -1;
        public int rolledMaxDamage = -1;

        public List<SavedRolledModifier> rolledModifiers = new List<SavedRolledModifier>();
    }

    [Serializable]
    public class SavedEquippedItem
    {
        public EquipmentSlot slot;
        public SavedItemInstance item;
    }

    [Serializable]
    public class SavedSkillLoadout
    {
        public List<string> availableSkillIds = new List<string>();
        public List<string> equippedSkillIds = new List<string>();
    }

    public static GameSession Instance { get; private set; }

    [Header("Run State")]
    [SerializeField] private int currentCombatLevel = 1;
    [SerializeField] private bool pendingLobbyLevelUp = false;

    [Header("Player Persistent Data")]
    [SerializeField] private int savedPlayerLevel = 1;
    [SerializeField] private int savedUnspentStatPoints = 0;
    [SerializeField] private int savedGold = 0;

    [Header("Runtime Saved Data")]
    [SerializeField] private List<SavedItemInstance> savedInventory = new List<SavedItemInstance>();
    [SerializeField] private List<SavedEquippedItem> savedEquipment = new List<SavedEquippedItem>();
    [SerializeField] private SavedSkillLoadout savedSkills = new SavedSkillLoadout();

    [Header("Definition Registries")]
    [SerializeField] private List<ItemDefinition> allItemDefinitions = new List<ItemDefinition>();
    [SerializeField] private List<SkillDefinition> allSkillDefinitions = new List<SkillDefinition>();

    private readonly Dictionary<string, ItemDefinition> itemById = new Dictionary<string, ItemDefinition>();
    private readonly Dictionary<string, SkillDefinition> skillById = new Dictionary<string, SkillDefinition>();

    private bool hasRestorablePlayerState;

    public int CurrentCombatLevel => Mathf.Max(1, currentCombatLevel);
    public bool PendingLobbyLevelUp => pendingLobbyLevelUp;
    public bool HasRestorablePlayerState => hasRestorablePlayerState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RebuildDefinitionCaches();
    }

    private void OnValidate()
    {
        RebuildDefinitionCaches();
    }

    public void ResetRunData()
    {
        currentCombatLevel = 1;
        pendingLobbyLevelUp = false;

        savedPlayerLevel = 1;
        savedUnspentStatPoints = 0;
        savedGold = 0;

        savedInventory.Clear();
        savedEquipment.Clear();
        savedSkills = new SavedSkillLoadout();

        hasRestorablePlayerState = false;
        selectedPlayerClass = CharacterClass.Unassigned;
        pendingClassSelection = false;
    }

    public void BeginNewRun(int startingCombatLevel = 1)
    {
        ResetRunData();
        currentCombatLevel = Mathf.Max(1, startingCombatLevel);
    }

    public void MarkCombatLevelCompleted(int completedLevel)
    {
        currentCombatLevel = Mathf.Max(currentCombatLevel, completedLevel);
        pendingLobbyLevelUp = true;
    }

    public void ConsumePendingLobbyLevelUp()
    {
        pendingLobbyLevelUp = false;
    }

    public void SaveFromPlayer(
        PlayerProgression progression,
        PlayerWallet wallet,
        CharacterInventory inventory,
        CharacterEquipment equipment,
        PlayerSkillLoadout skillLoadout)
    {
        if (progression != null)
        {
            savedPlayerLevel = progression.CurrentLevel;
            savedUnspentStatPoints = progression.UnspentStatPoints;
        }

        if (wallet != null)
            savedGold = wallet.CurrentGold;

        SaveInventory(inventory);
        SaveEquipment(equipment);
        SaveSkills(skillLoadout);

        hasRestorablePlayerState = true;
    }

    public void LoadIntoPlayer(
        PlayerProgression progression,
        PlayerWallet wallet,
        CharacterInventory inventory,
        CharacterEquipment equipment,
        PlayerSkillLoadout skillLoadout)
    {
        if (!hasRestorablePlayerState)
            return;

        if (progression != null)
            progression.SetProgressionState(savedPlayerLevel, savedUnspentStatPoints);

        if (wallet != null)
            wallet.SetGold(savedGold);

        LoadInventory(inventory);
        LoadEquipment(equipment);
        LoadSkills(skillLoadout);
    }

    private void SaveInventory(CharacterInventory inventory)
    {
        savedInventory.Clear();

        if (inventory == null)
            return;

        var items = inventory.Items;
        for (int i = 0; i < items.Count; i++)
        {
            SavedItemInstance saved = BuildSavedItem(items[i]);
            if (saved != null)
                savedInventory.Add(saved);
        }
    }

    private void LoadInventory(CharacterInventory inventory)
    {
        if (inventory == null)
            return;

        inventory.ClearAll();

        for (int i = 0; i < savedInventory.Count; i++)
        {
            ItemInstance instance = RebuildItem(savedInventory[i]);
            if (instance == null)
                continue;

            bool added = inventory.AddItemInstance(instance);
            if (!added)
            {
                Debug.LogWarning("GameSession: inventarul este plin la load.");
                break;
            }
        }
    }

    private void SaveEquipment(CharacterEquipment equipment)
    {
        savedEquipment.Clear();

        if (equipment == null)
            return;

        EquipmentSlot[] slots =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.Head,
            EquipmentSlot.Chest,
            EquipmentSlot.Hands,
            EquipmentSlot.Legs,
            EquipmentSlot.Feet,
            EquipmentSlot.Belt,
            EquipmentSlot.Ring,
            EquipmentSlot.Amulet
        };

        for (int i = 0; i < slots.Length; i++)
        {
            ItemInstance equippedItem = equipment.GetItemInSlot(slots[i]);
            SavedItemInstance savedItem = BuildSavedItem(equippedItem);
            if (savedItem == null)
                continue;

            savedEquipment.Add(new SavedEquippedItem
            {
                slot = slots[i],
                item = savedItem
            });
        }
    }

    private void LoadEquipment(CharacterEquipment equipment)
    {
        if (equipment == null)
            return;

        equipment.ClearAllEquipped(false);

        for (int i = 0; i < savedEquipment.Count; i++)
        {
            SavedEquippedItem saved = savedEquipment[i];
            if (saved == null || saved.item == null)
                continue;

            ItemInstance rebuilt = RebuildItem(saved.item);
            if (rebuilt == null)
                continue;

            equipment.EquipItem(rebuilt);
        }

        equipment.ForceRefresh();
    }

    private void SaveSkills(PlayerSkillLoadout skillLoadout)
    {
        savedSkills = new SavedSkillLoadout();

        if (skillLoadout == null)
            return;

        var available = skillLoadout.AvailableSkills;
        for (int i = 0; i < available.Count; i++)
        {
            SkillDefinition skill = available[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            savedSkills.availableSkillIds.Add(skill.SkillId);
        }

        int slotCount = skillLoadout.SlotCount;
        for (int i = 0; i < slotCount; i++)
        {
            SkillDefinition equipped = skillLoadout.GetSkillInSlot(i);
            savedSkills.equippedSkillIds.Add(equipped != null ? equipped.SkillId : string.Empty);
        }
    }

    private void LoadSkills(PlayerSkillLoadout skillLoadout)
    {
        if (skillLoadout == null)
            return;

        skillLoadout.ResetToDefaultState();

        for (int i = 0; i < savedSkills.availableSkillIds.Count; i++)
        {
            string skillId = savedSkills.availableSkillIds[i];
            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            if (!skillById.TryGetValue(skillId, out SkillDefinition skill) || skill == null)
                continue;

            if (!skillLoadout.HasSkill(skill))
                skillLoadout.LearnSkill(skill, false);
        }

        for (int i = 0; i < savedSkills.equippedSkillIds.Count; i++)
        {
            string skillId = savedSkills.equippedSkillIds[i];
            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            if (!skillById.TryGetValue(skillId, out SkillDefinition skill) || skill == null)
                continue;

            skillLoadout.AssignSkillToSlot(skill, i);
        }

        skillLoadout.NotifyChanged();
    }

    private SavedItemInstance BuildSavedItem(ItemInstance item)
    {
        if (item == null || !item.IsValid || item.Definition == null)
            return null;

        string itemId = item.Definition.ItemId;
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        SavedItemInstance saved = new SavedItemInstance
        {
            itemId = itemId,
            rarity = item.Rarity,
            stackCount = Mathf.Max(1, item.StackCount),
            itemLevel = item.ItemLevel
        };

        if (item.WeaponDefinition != null)
        {
            saved.rolledMinDamage = item.GetWeaponMinDamage();
            saved.rolledMaxDamage = item.GetWeaponMaxDamage();
        }

        var rolledMods = item.RolledModifiers;
        if (rolledMods != null)
        {
            for (int i = 0; i < rolledMods.Count; i++)
            {
                ItemRolledModifier mod = rolledMods[i];
                if (mod == null)
                    continue;

                saved.rolledModifiers.Add(new SavedRolledModifier
                {
                    bonusType = mod.BonusType,
                    value = mod.Value
                });
            }
        }

        return saved;
    }

    private ItemInstance RebuildItem(SavedItemInstance saved)
    {
        if (saved == null || string.IsNullOrWhiteSpace(saved.itemId))
            return null;

        if (!itemById.TryGetValue(saved.itemId, out ItemDefinition definition) || definition == null)
        {
            Debug.LogWarning($"GameSession: nu gasesc ItemDefinition pentru '{saved.itemId}'.");
            return null;
        }

        ItemInstance instance = new ItemInstance(definition, saved.rarity, Mathf.Max(1, saved.stackCount));
        instance.SetItemLevel(Mathf.Max(1, saved.itemLevel));

        if (instance.WeaponDefinition != null && saved.rolledMinDamage > 0 && saved.rolledMaxDamage >= saved.rolledMinDamage)
            instance.SetRolledWeaponDamage(saved.rolledMinDamage, saved.rolledMaxDamage);

        instance.ClearRolledBonuses();

        if (saved.rolledModifiers != null)
        {
            for (int i = 0; i < saved.rolledModifiers.Count; i++)
            {
                SavedRolledModifier mod = saved.rolledModifiers[i];
                if (mod == null)
                    continue;

                instance.AddRolledBonus(mod.bonusType, mod.value);
            }
        }

        return instance;
    }

    private void RebuildDefinitionCaches()
    {
        itemById.Clear();
        skillById.Clear();

        for (int i = 0; i < allItemDefinitions.Count; i++)
        {
            ItemDefinition def = allItemDefinitions[i];
            if (def == null || string.IsNullOrWhiteSpace(def.ItemId))
                continue;

            if (itemById.ContainsKey(def.ItemId))
                Debug.LogWarning($"GameSession: ItemId duplicat detectat: {def.ItemId}");

            itemById[def.ItemId] = def;
        }

        for (int i = 0; i < allSkillDefinitions.Count; i++)
        {
            SkillDefinition def = allSkillDefinitions[i];
            if (def == null || string.IsNullOrWhiteSpace(def.SkillId))
                continue;

            if (skillById.ContainsKey(def.SkillId))
                Debug.LogWarning($"GameSession: SkillId duplicat detectat: {def.SkillId}");

            skillById[def.SkillId] = def;
        }
    }

    public void SetSelectedPlayerClass(CharacterClass classType)
    {
        selectedPlayerClass = classType;
        pendingClassSelection = true;
    }

    public bool TryConsumeSelectedPlayerClass(out CharacterClass classType)
    {
        classType = selectedPlayerClass;

        if (!pendingClassSelection)
            return false;

        pendingClassSelection = false;
        return true;
    }
}