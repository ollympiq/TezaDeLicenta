using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterBasicAttack))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CharacterSkillCaster skillCaster;

    [Header("Targeting")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float rayDistance = 500f;

    [Header("Cursor")]
    [SerializeField] private Texture2D defaultCursor;

    private CharacterBasicAttack basicAttack;
    private CharacterHealth health;

    private SkillDefinition selectedSkill;
    private int selectedSlotIndex = -1;
    private bool turnInputEnabled;
    private bool blockMovementThisFrame;

    public event Action OnSelectedSkillChanged;

    public int SelectedSlotIndex => selectedSlotIndex;

    public bool HasTargetingSkillSelected =>
        selectedSkill != null &&
        selectedSkill.TargetingMode != SkillTargetingMode.None;

    public bool BlockMovementThisFrame => blockMovementThisFrame;

    private void Awake()
    {
        basicAttack = GetComponent<CharacterBasicAttack>();

        if (skillCaster == null)
            skillCaster = GetComponent<CharacterSkillCaster>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        health = GetComponent<CharacterHealth>();
        turnInputEnabled = false;
    }

    private void Start()
    {
        ApplyCursor();
    }

    private void Update()
    {
        blockMovementThisFrame = false;

        if (health != null && health.IsDead)
            return;

        if (!turnInputEnabled)
        {
            if (HasTargetingSkillSelected)
                ClearSelectedSkill();

            return;
        }

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

            blockMovementThisFrame = true;

            switch (selectedSkill.TargetingMode)
            {
                case SkillTargetingMode.Enemy:
                    TryUseSelectedSkillOnEnemy();
                    break;

                case SkillTargetingMode.Ground:
                    TryUseSelectedSkillOnGround();
                    break;

                case SkillTargetingMode.Self:
                    Debug.Log("Skill-urile Self nu sunt implementate inca.");
                    break;
            }
        }
    }

    public void ToggleSkillSelection(SkillDefinition skill, int slotIndex)
    {
        if (!turnInputEnabled)
        {
            ClearSelectedSkill();
            return;
        }

        if (health != null && health.IsDead)
        {
            ClearSelectedSkill();
            return;
        }

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
        blockMovementThisFrame = true;

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
                usedSuccessfully = skillCaster != null && skillCaster.TryUseSkillOnTarget(selectedSkill, targetStats);
                break;

            case SkillType.Passive:
                Debug.Log($"{selectedSkill.DisplayName} este un skill pasiv si nu poate fi folosit prin click.");
                break;
        }

        if (usedSuccessfully && !selectedSkill.KeepSelectedAfterUse)
            ClearSelectedSkill();
    }

    private void TryUseSelectedSkillOnGround()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundMask))
            return;

        bool usedSuccessfully = false;

        switch (selectedSkill.SkillType)
        {
            case SkillType.Active:
                usedSuccessfully = skillCaster != null && skillCaster.TryUseSkillAtPoint(selectedSkill, hit.point);
                break;

            case SkillType.BasicAttack:
                Debug.Log("Basic Attack nu poate fi folosit pe ground.");
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
            if (IsCursorTextureUsable(selectedSkill.CursorTexture))
            {
                cursorTexture = selectedSkill.CursorTexture;
                hotspot = selectedSkill.CursorHotspot;
            }
            else
            {
                Debug.LogWarning($"Cursor texture pentru skill-ul '{selectedSkill.DisplayName}' nu are setarile corecte de import.");
            }
        }

        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }

    private bool IsCursorTextureUsable(Texture2D texture)
    {
        if (texture == null)
            return false;

        if (!texture.isReadable)
            return false;

        if (texture.mipmapCount > 1)
            return false;

        return true;
    }

    public void SetTurnInputEnabled(bool enabled)
    {
        turnInputEnabled = enabled;

        if (!enabled)
            ClearSelectedSkill();
    }
}