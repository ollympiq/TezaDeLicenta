using UnityEngine;

public class SkillBarUI : MonoBehaviour
{
    [SerializeField] private PlayerSkillLoadout loadout;
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private SkillBarSlotUI[] slots;

    private void Start()
    {
        if (loadout == null)
            loadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (combatController == null)
            combatController = FindFirstObjectByType<PlayerCombatController>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Setup(this, loadout, i);
        }

        if (loadout != null)
            loadout.OnLoadoutChanged += RefreshNow;

        if (combatController != null)
            combatController.OnSelectedSkillChanged += RefreshNow;

        RefreshNow();
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged -= RefreshNow;

        if (combatController != null)
            combatController.OnSelectedSkillChanged -= RefreshNow;
    }

    public void HandleSlotClicked(int slotIndex)
    {
        if (loadout == null || combatController == null)
            return;

        SkillDefinition skill = loadout.GetSkillInSlot(slotIndex);

        if (skill == null)
        {
            combatController.ClearSelectedSkill();
            RefreshNow();
            return;
        }

        combatController.ToggleSkillSelection(skill, slotIndex);
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (loadout == null || combatController == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            SkillDefinition skill = loadout.GetSkillInSlot(i);
            bool isSelected = combatController.SelectedSlotIndex == i;
            slots[i].Refresh(skill, isSelected);
        }
    }
}