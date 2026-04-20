using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillLoadout : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private SkillDefinition defaultBasicAttack;

    [Header("Owned Skills")]
    [SerializeField] private List<SkillDefinition> availableSkills = new List<SkillDefinition>();

    [Header("Action Bar")]
    [SerializeField] private SkillDefinition[] equippedSkills = new SkillDefinition[8];

    public event Action OnLoadoutChanged;

    public IReadOnlyList<SkillDefinition> AvailableSkills => availableSkills;
    public int SlotCount => equippedSkills != null ? equippedSkills.Length : 0;
    public SkillDefinition DefaultBasicAttack => defaultBasicAttack;

    private void Awake()
    {
        EnsureSetup();
    }

    private void Start()
    {
        EnsureSetup();
        OnLoadoutChanged?.Invoke();
    }

    private void OnValidate()
    {
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        if (availableSkills == null)
            availableSkills = new List<SkillDefinition>();

        if (equippedSkills == null || equippedSkills.Length != 8)
            equippedSkills = new SkillDefinition[8];

        if (defaultBasicAttack != null && !availableSkills.Contains(defaultBasicAttack))
            availableSkills.Insert(0, defaultBasicAttack);

        if (defaultBasicAttack != null)
            equippedSkills[0] = defaultBasicAttack;
    }

    public SkillDefinition GetSkillInSlot(int slotIndex)
    {
        if (equippedSkills == null || slotIndex < 0 || slotIndex >= equippedSkills.Length)
            return null;

        return equippedSkills[slotIndex];
    }

    public bool HasSkill(SkillDefinition skill)
    {
        if (skill == null)
            return false;

        EnsureSetup();
        return availableSkills.Contains(skill);
    }

    public bool LearnSkill(SkillDefinition skill, bool autoEquipToFirstFreeSlot = false)
    {
        if (skill == null)
            return false;

        EnsureSetup();

        if (availableSkills.Contains(skill))
            return false;

        availableSkills.Add(skill);

        if (autoEquipToFirstFreeSlot)
        {
            for (int i = 1; i < equippedSkills.Length; i++)
            {
                if (equippedSkills[i] == null)
                {
                    equippedSkills[i] = skill;
                    break;
                }
            }
        }

        OnLoadoutChanged?.Invoke();
        return true;
    }

    public bool AssignSkillToSlot(SkillDefinition skill, int slotIndex)
    {
        if (skill == null)
            return false;

        if (slotIndex < 0 || slotIndex >= equippedSkills.Length)
            return false;

        EnsureSetup();

        if (slotIndex == 0)
            return skill == defaultBasicAttack;

        if (skill == defaultBasicAttack)
            return false;

        if (!availableSkills.Contains(skill))
            return false;

        for (int i = 1; i < equippedSkills.Length; i++)
        {
            if (equippedSkills[i] == skill)
                equippedSkills[i] = null;
        }

        equippedSkills[slotIndex] = skill;
        OnLoadoutChanged?.Invoke();
        return true;
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex <= 0 || slotIndex >= equippedSkills.Length)
            return;

        equippedSkills[slotIndex] = null;
        OnLoadoutChanged?.Invoke();
    }

    public void NotifyChanged()
    {
        OnLoadoutChanged?.Invoke();
    }
}