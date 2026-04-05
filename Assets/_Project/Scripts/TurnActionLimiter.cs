using System.Collections.Generic;
using UnityEngine;

public class TurnActionLimiter : MonoBehaviour
{
    private bool basicAttackUsedThisTurn;
    private readonly HashSet<SkillDefinition> usedSkillsThisTurn = new();
    private readonly HashSet<string> usedCustomActionsThisTurn = new();

    public bool IsBasicAttackUsedThisTurn => basicAttackUsedThisTurn;

    public void ResetTurnUsage()
    {
        basicAttackUsedThisTurn = false;
        usedSkillsThisTurn.Clear();
        usedCustomActionsThisTurn.Clear();
    }

    public bool CanUseBasicAttack()
    {
        return !basicAttackUsedThisTurn;
    }

    public void MarkBasicAttackUsed()
    {
        basicAttackUsedThisTurn = true;
    }

    public bool CanUseSkill(SkillDefinition skill)
    {
        return skill != null && !usedSkillsThisTurn.Contains(skill);
    }

    public void MarkSkillUsed(SkillDefinition skill)
    {
        if (skill != null)
            usedSkillsThisTurn.Add(skill);
    }

    public bool IsSkillUsedThisTurn(SkillDefinition skill)
    {
        return skill != null && usedSkillsThisTurn.Contains(skill);
    }

    public bool CanUseCustomAction(string actionKey)
    {
        return !string.IsNullOrWhiteSpace(actionKey) && !usedCustomActionsThisTurn.Contains(actionKey);
    }

    public void MarkCustomActionUsed(string actionKey)
    {
        if (!string.IsNullOrWhiteSpace(actionKey))
            usedCustomActionsThisTurn.Add(actionKey);
    }

    public bool IsCustomActionUsedThisTurn(string actionKey)
    {
        return !string.IsNullOrWhiteSpace(actionKey) && usedCustomActionsThisTurn.Contains(actionKey);
    }
}