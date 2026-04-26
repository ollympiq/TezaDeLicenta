using TMPro;
using UnityEngine;

public class EnemyLootUI : MonoBehaviour
{
    public static EnemyLootUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CharacterInventory playerInventory;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private EnemyLootSlotUI[] slots;

    private EnemyLootContainer currentContainer;
    private int lastCollectedGold;
    private bool initialized;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public EnemyLootContainer CurrentContainer => currentContainer;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (Instance != null && Instance != this)
        {
            GameLog.Warning("Exista deja un EnemyLootUI activ in scena.");
            return;
        }

        Instance = this;

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<CharacterInventory>();

        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();

        if (panelRoot == null)
            panelRoot = gameObject;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Setup(this, i);
        }

        currentContainer = null;
        lastCollectedGold = 0;
        initialized = true;
    }

    public void Show(EnemyLootContainer container)
    {
        EnsureInitialized();

        if (container == null || panelRoot == null)
        {
            Hide();
            return;
        }

        currentContainer = container;
        CollectGoldFromCurrentContainer();

        if (currentContainer.ItemCount <= 0)
        {
            Hide();
            return;
        }

        if (titleText != null)
            titleText.text = BuildTitle(currentContainer);

        if (!panelRoot.activeSelf)
            panelRoot.SetActive(true);

        RefreshNow();
    }

    public void Hide()
    {
        EnsureInitialized();

        currentContainer = null;
        lastCollectedGold = 0;

        if (panelRoot != null && panelRoot.activeSelf)
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

        if (!playerInventory.CanAddItemInstance(item))
        {
            GameLog.Warning("Inventarul este plin.");
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
            GameLog.Warning("Inventarul este plin.");
            return;
        }

        string itemName = takenItem.Definition != null
            ? takenItem.Definition.DisplayName
            : "obiect necunoscut";

        GameLog.Success($"Ai adaugat in inventar: {itemName}.");

        RefreshNow();

        if (currentContainer.ItemCount <= 0)
            Hide();
    }

    private void CollectGoldFromCurrentContainer()
    {
        lastCollectedGold = 0;

        if (currentContainer == null || playerWallet == null)
            return;

        int gold = currentContainer.TakeGold();
        if (gold <= 0)
            return;

        playerWallet.AddGold(gold);
        lastCollectedGold = gold;

        GameLog.Success($"Ai primit {gold} gold.");
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
        string tierText;

        switch (container.LootTier)
        {
            case EnemyLootTier.MiniBoss:
                tierText = "Mini Boss Loot";
                break;

            case EnemyLootTier.Boss:
                tierText = "Boss Loot";
                break;

            default:
                tierText = "Loot";
                break;
        }

        if (lastCollectedGold > 0)
            return tierText + "  (+" + lastCollectedGold + " Gold)";

        return tierText;
    }
}