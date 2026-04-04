using UnityEngine;

public class SkillBookUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSkillLoadout loadout;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private SkillBookSlotUI[] slots;

    private void Start()
    {
        if (loadout == null)
            loadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (loadout != null)
            loadout.OnLoadoutChanged += RefreshNow;

        RefreshNow();
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged -= RefreshNow;
    }

    public void RefreshNow()
    {
        if (slots == null || slots.Length == 0)
            return;

        if (loadout == null || rootCanvas == null)
        {
            ClearAll();
            return;
        }

        var skills = loadout.AvailableSkills;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            if (i < skills.Count && skills[i] != null)
                slots[i].Bind(skills[i], rootCanvas);
            else
                slots[i].ClearSlot();
        }
    }

    private void ClearAll()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].ClearSlot();
        }
    }
}