using System.Collections.Generic;
using UnityEngine;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private TurnOrderSlotUI slotPrefab;

    private readonly List<TurnOrderSlotUI> spawnedSlots = new List<TurnOrderSlotUI>();

    private void Awake()
    {
        if (turnManager == null)
            turnManager = TurnManager.Instance != null
                ? TurnManager.Instance
                : FindFirstObjectByType<TurnManager>();
    }

    private void OnEnable()
    {
        if (turnManager != null)
            turnManager.OnTurnStateChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (turnManager != null)
            turnManager.OnTurnStateChanged -= Refresh;
    }

    public void Refresh()
    {
        if (turnManager == null || slotContainer == null || slotPrefab == null)
        {
            ClearAllSlots();
            return;
        }

        List<TurnManager.TurnActorPortraitData> order = turnManager.GetVisibleTurnOrderPortraits();

        EnsureSlotCount(order.Count);

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (i < order.Count)
            {
                spawnedSlots[i].gameObject.SetActive(true);
                spawnedSlots[i].SetData(order[i], i == 0);
            }
            else
            {
                spawnedSlots[i].Clear();
                spawnedSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private void EnsureSlotCount(int neededCount)
    {
        while (spawnedSlots.Count < neededCount)
        {
            TurnOrderSlotUI newSlot = Instantiate(slotPrefab, slotContainer);
            newSlot.gameObject.SetActive(true);
            spawnedSlots.Add(newSlot);
        }
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
                spawnedSlots[i].gameObject.SetActive(false);
        }
    }
}