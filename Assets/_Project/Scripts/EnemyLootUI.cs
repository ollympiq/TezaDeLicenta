using TMPro;
using UnityEngine;

public class EnemyLootUI : MonoBehaviour
{
    public static EnemyLootUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CharacterInventory playerInventory;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private EnemyLootSlotUI[] slots;

    private EnemyLootContainer currentContainer;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public EnemyLootContainer CurrentContainer => currentContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<CharacterInventory>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Setup(this, i);
        }

        Hide();
    }

    public void Show(EnemyLootContainer container)
    {
        if (container == null || panelRoot == null)
        {
            Hide();
            return;
        }

        currentContainer = container;

        if (titleText != null)
            titleText.text = BuildTitle(container);

        panelRoot.SetActive(true);
        RefreshNow();
    }

    public void Hide()
    {
        currentContainer = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        ClearAll();
    }

    public void RefreshNow()
    {
        if (slots == null || slots.Length == 0)
            return;

        if (currentContainer == null)
        {
            ClearAll();
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            ItemInstance item = currentContainer.GetItemAt(i);

            if (item != null)
                slots[i].Refresh(item);
            else
                slots[i].ClearSlot();
        }
    }

    public void HandleLootSlotClicked(int slotIndex)
    {
        if (currentContainer == null || playerInventory == null)
            return;

        ItemInstance item = currentContainer.GetItemAt(slotIndex);
        if (item == null || !item.IsValid)
            return;

        // verificam intai ca sa nu scoatem itemul degeaba
        if (!playerInventory.CanAddItemInstance(item))
        {
            Debug.Log("Inventarul este plin.");
            return;
        }

        ItemInstance takenItem = currentContainer.TakeAt(slotIndex);
        if (takenItem == null || !takenItem.IsValid)
        {
            RefreshNow();
            return;
        }

        bool added = playerInventory.AddItemInstance(takenItem);
        if (!added)
        {
            Debug.Log("Inventarul este plin.");
            return;
        }

        RefreshNow();

        if (currentContainer.ItemCount <= 0)
            Hide();
    }

    private void ClearAll()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].ClearSlot();
        }
    }

    private string BuildTitle(EnemyLootContainer container)
    {
        string tierText = container.LootTier switch
        {
            EnemyLootTier.MiniBoss => "Mini Boss Loot",
            EnemyLootTier.Boss => "Boss Loot",
            _ => "Loot"
        };

        return tierText;
    }
}