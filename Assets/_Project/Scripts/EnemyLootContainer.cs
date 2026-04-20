using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterHealth))]
public class EnemyLootContainer : MonoBehaviour
{
    [Header("Loot Identity")]
    [SerializeField] private EnemyLootTier lootTier = EnemyLootTier.Normal;

    [Header("Generation")]
    [SerializeField] private bool useGeneratedLoot = true;
    

    [Header("Debug Preview Loot")]
    [SerializeField] private List<ItemDefinition> debugPreviewLootDefinitions = new List<ItemDefinition>();

    private CharacterHealth health;
    private readonly List<ItemInstance> lootItems = new List<ItemInstance>();
    private int goldAmount;

    public EnemyLootTier LootTier => lootTier;
    public bool IsLootable { get; private set; }
    public IReadOnlyList<ItemInstance> LootItems => lootItems;
    public int ItemCount => lootItems.Count;
    public int GoldAmount => Mathf.Max(0, goldAmount);

    private void Awake()
    {
        health = GetComponent<CharacterHealth>();

        if (!useGeneratedLoot)
            RebuildDebugPreviewLoot();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDied;
    }

    private void HandleDied(CharacterHealth deadHealth)
    {
        if (useGeneratedLoot)
            GenerateLootNow();
        else
            RebuildDebugPreviewLoot();

        IsLootable = true;
    }

    public void GenerateLootNow()
    {
        lootItems.Clear();
        goldAmount = 0;

        LootGenerator generator = LootGenerator.Instance;
        if (generator == null)
        {
            Debug.LogWarning("EnemyLootContainer: nu exista LootGenerator in scena.");
            return;
        }

        GeneratedLootResult result = generator.GenerateLoot(lootTier, ResolveItemLevel());
        goldAmount = result.GoldAmount;
        SetLootItems(result.Items);
    }
    private int ResolveItemLevel()
    {
        if (CurrentLevelContext.Instance != null)
            return CurrentLevelContext.Instance.CurrentLevel;

        return 1;
    }
    public void RebuildDebugPreviewLoot()
    {
        lootItems.Clear();
        goldAmount = 0;

        for (int i = 0; i < debugPreviewLootDefinitions.Count; i++)
        {
            ItemDefinition def = debugPreviewLootDefinitions[i];
            if (def == null)
                continue;

            lootItems.Add(new ItemInstance(def, 1));
        }
    }

    public ItemInstance GetItemAt(int index)
    {
        if (index < 0 || index >= lootItems.Count)
            return null;

        ItemInstance item = lootItems[index];
        return item != null && item.IsValid ? item : null;
    }

    public ItemInstance TakeAt(int index)
    {
        if (index < 0 || index >= lootItems.Count)
            return null;

        ItemInstance item = lootItems[index];
        lootItems.RemoveAt(index);
        return item;
    }

    public int TakeGold()
    {
        int taken = GoldAmount;
        goldAmount = 0;
        return taken;
    }

    public void SetLootItems(IEnumerable<ItemInstance> newItems)
    {
        lootItems.Clear();

        if (newItems == null)
            return;

        foreach (ItemInstance item in newItems)
        {
            if (item != null && item.IsValid)
                lootItems.Add(item);
        }
    }

    public void ClearLoot()
    {
        lootItems.Clear();
        goldAmount = 0;
    }
}