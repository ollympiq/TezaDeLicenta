using UnityEngine;

public class SkillBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSkillLoadout loadout;
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private PlayerAP playerAP;
    [SerializeField] private PlayerSkillCooldowns skillCooldowns;
    [SerializeField] private SkillBarSlotUI[] slots;

    [Header("Action Bar Change Cost")]
    [SerializeField] private bool consumeAPWhenAssigningDuringCombat = true;
    [SerializeField, Min(0)] private int assignSkillAPCost = 1;

    private void Start()
    {
        ResolveReferences();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Setup(this, loadout, i);
        }

        if (loadout != null)
            loadout.OnLoadoutChanged += RefreshNow;

        if (combatController != null)
            combatController.OnSelectedSkillChanged += RefreshNow;

        if (skillCooldowns != null)
            skillCooldowns.OnCooldownsChanged += RefreshNow;

        RefreshNow();
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged -= RefreshNow;

        if (combatController != null)
            combatController.OnSelectedSkillChanged -= RefreshNow;

        if (skillCooldowns != null)
            skillCooldowns.OnCooldownsChanged -= RefreshNow;
    }

    public void HandleSlotClicked(int slotIndex)
    {
        ResolveReferences();

        if (loadout == null || combatController == null)
            return;

        SkillDefinition skill = loadout.GetSkillInSlot(slotIndex);

        if (skill == null)
        {
            combatController.ClearSelectedSkill();
            RefreshNow();
            return;
        }

        int cooldownRemaining = skillCooldowns != null
            ? skillCooldowns.GetRemainingCooldown(skill)
            : 0;

        if (cooldownRemaining > 0)
        {
            GameLog.Warning($"Skill-ul {skill.DisplayName} este in cooldown: {cooldownRemaining} ture.");
            combatController.ClearSelectedSkill();
            RefreshNow();
            return;
        }

        combatController.ToggleSkillSelection(skill, slotIndex);
        RefreshNow();
    }

    public bool TryAssignSkillFromSkillBook(SkillDefinition skill, int slotIndex)
    {
        ResolveReferences();

        if (loadout == null || skill == null)
            return false;

        if (slotIndex < 0 || slotIndex >= loadout.SlotCount)
            return false;

        SkillDefinition currentSkillInSlot = loadout.GetSkillInSlot(slotIndex);

        if (currentSkillInSlot == skill)
        {
            RefreshNow();
            return true;
        }

        if (ShouldBlockAssignmentBecauseNotPlayerTurn())
        {
            GameLog.Warning("Nu poti modifica Action Bar-ul in tura inamicilor.");
            return false;
        }

        bool shouldSpendAP = ShouldSpendAPForAssignment();

        if (shouldSpendAP)
        {
            if (playerAP == null)
            {
                GameLog.Warning("Lipseste PlayerAP. Skill-ul nu poate fi pus in Action Bar.");
                return false;
            }

            if (!playerAP.HasEnoughAP(assignSkillAPCost))
            {
                GameLog.Warning($"Nu ai destul AP pentru a modifica Action Bar-ul. Cost: {assignSkillAPCost} AP.");
                return false;
            }
        }

        bool assigned = loadout.AssignSkillToSlot(skill, slotIndex);

        if (!assigned)
            return false;

        if (shouldSpendAP && assignSkillAPCost > 0)
        {
            bool spent = playerAP.SpendAP(assignSkillAPCost);

            if (!spent)
            {
                GameLog.Warning("AP-ul nu a putut fi consumat dupa modificarea Action Bar-ului.");
                return false;
            }

            GameLog.Info($"Action Bar modificat. Cost: {assignSkillAPCost} AP.");
        }

        RefreshNow();
        return true;
    }

    public void RefreshNow()
    {
        ResolveReferences();

        if (loadout == null || combatController == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            SkillDefinition skill = loadout.GetSkillInSlot(i);
            bool isSelected = combatController.SelectedSlotIndex == i;

            int cooldownRemaining = skillCooldowns != null && skill != null
                ? skillCooldowns.GetRemainingCooldown(skill)
                : 0;

            slots[i].Refresh(skill, isSelected, cooldownRemaining);
        }
    }

    private void ResolveReferences()
    {
        if (loadout == null)
            loadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (combatController == null)
            combatController = FindFirstObjectByType<PlayerCombatController>();

        if (playerAP == null)
            playerAP = FindFirstObjectByType<PlayerAP>();

        if (skillCooldowns == null)
            skillCooldowns = FindFirstObjectByType<PlayerSkillCooldowns>();
    }

    private bool ShouldSpendAPForAssignment()
    {
        if (!consumeAPWhenAssigningDuringCombat)
            return false;

        if (assignSkillAPCost <= 0)
            return false;

        if (TurnManager.Instance == null)
            return false;

        if (!TurnManager.Instance.IsCombatActive)
            return false;

        return TurnManager.Instance.IsPlayerTurnActive;
    }

    private bool ShouldBlockAssignmentBecauseNotPlayerTurn()
    {
        if (TurnManager.Instance == null)
            return false;

        if (!TurnManager.Instance.IsCombatActive)
            return false;

        return !TurnManager.Instance.IsPlayerTurnActive;
    }
}