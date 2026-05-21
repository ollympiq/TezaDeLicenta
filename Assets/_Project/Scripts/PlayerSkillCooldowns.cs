using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillCooldowns : MonoBehaviour
{
    private readonly Dictionary<SkillDefinition, int> cooldowns = new Dictionary<SkillDefinition, int>();

    public event Action OnCooldownsChanged;

    public bool IsOnCooldown(SkillDefinition skill)
    {
        return GetRemainingCooldown(skill) > 0;
    }

    public int GetRemainingCooldown(SkillDefinition skill)
    {
        if (skill == null)
            return 0;

        if (!cooldowns.TryGetValue(skill, out int remaining))
            return 0;

        return Mathf.Max(0, remaining);
    }

    public void StartCooldown(SkillDefinition skill)
    {
        if (skill == null)
            return;

        int cooldownTurns = Mathf.Max(0, skill.CooldownTurns);

        if (cooldownTurns <= 0)
            return;

        cooldowns[skill] = cooldownTurns;
        OnCooldownsChanged?.Invoke();
    }

    public void TickStartOfPlayerTurn()
    {
        if (cooldowns.Count == 0)
            return;

        List<SkillDefinition> keys = new List<SkillDefinition>(cooldowns.Keys);
        bool changed = false;

        for (int i = 0; i < keys.Count; i++)
        {
            SkillDefinition skill = keys[i];

            if (skill == null)
            {
                cooldowns.Remove(skill);
                changed = true;
                continue;
            }

            cooldowns[skill]--;

            if (cooldowns[skill] <= 0)
                cooldowns.Remove(skill);

            changed = true;
        }

        if (changed)
            OnCooldownsChanged?.Invoke();
    }

    public void ClearAll()
    {
        if (cooldowns.Count == 0)
            return;

        cooldowns.Clear();
        OnCooldownsChanged?.Invoke();
    }
}