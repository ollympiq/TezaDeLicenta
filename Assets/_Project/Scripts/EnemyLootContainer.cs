using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterHealth))]
public class EnemyLootContainer : MonoBehaviour
{
    [Header("Loot Identity")]
    [SerializeField] private EnemyLootTier lootTier = EnemyLootTier.Normal;

    [Header("Debug Preview Loot")]
    [SerializeField] private List<ItemDefinition> debugPreviewLootDefinitions = new();

    private CharacterHealth health;
    private readonly List<ItemInstance> lootItems = new();

    public EnemyLootTier LootTier => lootTier;
    public bool IsLootable { get; private set; }
    public IReadOnlyList<ItemInstance> LootItems => lootItems;
    public int ItemCount => lootItems.Count;

    private void Awake()
    {
        health = GetComponent<CharacterHealth>();
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
        IsLootable = true;
    }

    public void RebuildDebugPreviewLoot()
    {
        lootItems.Clear();

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
    }
}