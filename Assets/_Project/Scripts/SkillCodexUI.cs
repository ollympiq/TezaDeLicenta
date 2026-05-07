using System.Collections.Generic;
using UnityEngine;

public class SkillCodexUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSkillLoadout loadout;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private SkillCodexSlotUI[] slots;

    [Header("All Skills In Game")]
    [SerializeField] private List<SkillDefinition> allSkills = new List<SkillDefinition>();

    [Header("Sorting")]
    [SerializeField] private bool sortSkills = true;
    [SerializeField] private bool placeOwnedSkillsFirstInsideGroup = false;

    [Header("Behaviour")]
    [SerializeField] private bool startVisible = false;
    [SerializeField] private bool autoFindLoadout = true;
    [SerializeField] private bool autoResolveSlots = true;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (autoFindLoadout && loadout == null)
            loadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (autoResolveSlots)
            ResolveSlots();
    }

    private void Start()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged += RefreshNow;

        SetVisible(startVisible);
        RefreshNow();
    }

    private void OnDestroy()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged -= RefreshNow;
    }

    public void Toggle()
    {
        if (panelRoot == null)
            return;

        SetVisible(!panelRoot.activeSelf);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);

        if (visible)
            RefreshNow();
        else if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();
    }

    public void RefreshNow()
    {
        if (slots == null || slots.Length == 0)
            ResolveSlots();

        if (slots == null || slots.Length == 0)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].ClearSlot();
        }

        if (allSkills == null || allSkills.Count == 0)
            return;

        List<SkillDefinition> displaySkills = GetDisplaySkills();

        int count = Mathf.Min(slots.Length, displaySkills.Count);

        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null)
                continue;

            SkillDefinition skill = displaySkills[i];
            bool isOwned = IsSkillOwned(skill);

            slots[i].Bind(skill, isOwned);
        }
    }

    private List<SkillDefinition> GetDisplaySkills()
    {
        List<SkillDefinition> displaySkills = new List<SkillDefinition>();

        for (int i = 0; i < allSkills.Count; i++)
        {
            if (allSkills[i] != null)
                displaySkills.Add(allSkills[i]);
        }

        if (!sortSkills)
            return displaySkills;

        displaySkills.Sort(CompareSkills);
        return displaySkills;
    }

    private int CompareSkills(SkillDefinition a, SkillDefinition b)
    {
        if (a == null && b == null)
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        int groupA = GetSkillGroupOrder(a);
        int groupB = GetSkillGroupOrder(b);

        if (groupA != groupB)
            return groupA.CompareTo(groupB);

        if (placeOwnedSkillsFirstInsideGroup)
        {
            bool ownedA = IsSkillOwned(a);
            bool ownedB = IsSkillOwned(b);

            if (ownedA != ownedB)
                return ownedA ? -1 : 1;
        }

        int effectA = GetMainEffectOrder(a);
        int effectB = GetMainEffectOrder(b);

        if (effectA != effectB)
            return effectA.CompareTo(effectB);

        string nameA = string.IsNullOrWhiteSpace(a.DisplayName) ? a.name : a.DisplayName;
        string nameB = string.IsNullOrWhiteSpace(b.DisplayName) ? b.name : b.DisplayName;

        return string.Compare(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
    }

    private int GetSkillGroupOrder(SkillDefinition skill)
    {
        if (skill == null)
            return 999;

        if (!skill.DealsDamage)
            return 100;

        switch (skill.DamageType)
        {
            case DamageType.Physical:
                return 0;

            case DamageType.Fire:
                return 10;

            case DamageType.Ice:
                return 20;

            case DamageType.Earth:
                return 30;

            case DamageType.Wind:
                return 40;

            case DamageType.Lightning:
                return 50;

            default:
                return 90;
        }
    }

    private int GetMainEffectOrder(SkillDefinition skill)
    {
        if (skill == null || skill.Effects == null || skill.Effects.Count == 0)
            return 999;

        SkillEffectType bestType = SkillEffectType.None;
        int bestOrder = 999;

        for (int i = 0; i < skill.Effects.Count; i++)
        {
            SkillEffectData effect = skill.Effects[i];

            if (effect == null)
                continue;

            int order = GetEffectTypeOrder(effect.EffectType);

            if (order < bestOrder)
            {
                bestOrder = order;
                bestType = effect.EffectType;
            }
        }

        return bestType == SkillEffectType.None ? 999 : bestOrder;
    }

    private int GetEffectTypeOrder(SkillEffectType effectType)
    {
        switch (effectType)
        {
            case SkillEffectType.HealInstant:
                return 0;

            case SkillEffectType.BuffPrimaryAttributes:
                return 10;

            case SkillEffectType.BuffCritChance:
                return 20;

            case SkillEffectType.BuffElementalDamage:
                return 30;

            case SkillEffectType.DamageOverTime:
                return 40;

            case SkillEffectType.SlowMovement:
                return 50;

            case SkillEffectType.SkipTurn:
                return 60;

            default:
                return 999;
        }
    }

    private bool IsSkillOwned(SkillDefinition skill)
    {
        if (skill == null || loadout == null)
            return false;

        if (loadout.HasSkill(skill))
            return true;

        IReadOnlyList<SkillDefinition> ownedSkills = loadout.AvailableSkills;
        if (ownedSkills == null)
            return false;

        for (int i = 0; i < ownedSkills.Count; i++)
        {
            SkillDefinition ownedSkill = ownedSkills[i];

            if (ownedSkill == null)
                continue;

            if (ownedSkill == skill)
                return true;

            if (!string.IsNullOrWhiteSpace(ownedSkill.SkillId) &&
                ownedSkill.SkillId == skill.SkillId)
                return true;
        }

        return false;
    }

    private void ResolveSlots()
    {
        slots = GetComponentsInChildren<SkillCodexSlotUI>(true);
    }
}