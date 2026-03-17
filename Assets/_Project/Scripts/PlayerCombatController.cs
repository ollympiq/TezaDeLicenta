using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterBasicAttack))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Targeting")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float rayDistance = 500f;

    [Header("Cursor")]
    [SerializeField] private Texture2D defaultCursor;

    private CharacterBasicAttack basicAttack;

    private SkillDefinition selectedSkill;
    private int selectedSlotIndex = -1;

    public event Action OnSelectedSkillChanged;

    public int SelectedSlotIndex => selectedSlotIndex;

    public bool HasTargetingSkillSelected =>
        selectedSkill != null &&
        selectedSkill.TargetingMode != SkillTargetingMode.None;

    private void Awake()
    {
        basicAttack = GetComponent<CharacterBasicAttack>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        ApplyCursor();
    }

    private void Update()
    {
        if (!HasTargetingSkillSelected)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClearSelectedSkill();
            return;
        }

        if (Mouse.current == null || mainCamera == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearSelectedSkill();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            if (selectedSkill.TargetingMode == SkillTargetingMode.Enemy)
                TryUseSelectedSkillOnEnemy();
        }
    }

    public void ToggleSkillSelection(SkillDefinition skill, int slotIndex)
    {
        if (skill == null)
        {
            ClearSelectedSkill();
            return;
        }

        if (selectedSkill == skill && selectedSlotIndex == slotIndex)
        {
            ClearSelectedSkill();
            return;
        }

        selectedSkill = skill;
        selectedSlotIndex = slotIndex;

        ApplyCursor();
        OnSelectedSkillChanged?.Invoke();
    }

    public void ClearSelectedSkill()
    {
        selectedSkill = null;
        selectedSlotIndex = -1;

        ApplyCursor();
        OnSelectedSkillChanged?.Invoke();
    }

    private void TryUseSelectedSkillOnEnemy()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, enemyMask))
            return;

        CharacterStats targetStats = hit.collider.GetComponentInParent<CharacterStats>();
        if (targetStats == null)
            return;

        bool usedSuccessfully = false;

        switch (selectedSkill.SkillType)
        {
            case SkillType.BasicAttack:
                usedSuccessfully = basicAttack != null && basicAttack.TryAttackTarget(targetStats);
                break;

            case SkillType.Active:
                Debug.Log($"{selectedSkill.DisplayName} exista in bara, dar inca nu este implementat.");
                break;

            case SkillType.Passive:
                Debug.Log($"{selectedSkill.DisplayName} este un skill pasiv si nu poate fi folosit prin click.");
                break;
        }

        if (usedSuccessfully && !selectedSkill.KeepSelectedAfterUse)
            ClearSelectedSkill();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplyCursor()
    {
        Texture2D cursorTexture = defaultCursor;
        Vector2 hotspot = Vector2.zero;

        if (selectedSkill != null && selectedSkill.CursorTexture != null)
        {
            cursorTexture = selectedSkill.CursorTexture;
            hotspot = selectedSkill.CursorHotspot;
        }

        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}