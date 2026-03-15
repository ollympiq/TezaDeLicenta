using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterBasicAttack))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Button basicAttackButton;

    [Header("Targeting")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float rayDistance = 500f;

    [Header("Cursor")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D attackCursor;
    [SerializeField] private Vector2 attackCursorHotspot = new Vector2(8f, 8f);

    [Header("Button Colors")]
    [SerializeField] private Image basicAttackButtonImage;
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = new Color(0.75f, 1f, 0.75f, 1f);

    private CharacterBasicAttack basicAttack;
    private bool isTargetingBasicAttack;

    public bool IsTargetingBasicAttack => isTargetingBasicAttack;

    private void Awake()
    {
        basicAttack = GetComponent<CharacterBasicAttack>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        if (basicAttackButton != null)
            basicAttackButton.onClick.AddListener(ToggleBasicAttackMode);

        RefreshButtonVisual();
        ApplyCursor();
    }

    private void OnDestroy()
    {
        if (basicAttackButton != null)
            basicAttackButton.onClick.RemoveListener(ToggleBasicAttackMode);
    }

    private void Update()
    {
        if (!isTargetingBasicAttack)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelTargetingMode();
            return;
        }

        if (Mouse.current == null || mainCamera == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelTargetingMode();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            TryAttackUnderCursor();
        }
    }

    public void ToggleBasicAttackMode()
    {
        if (isTargetingBasicAttack)
            CancelTargetingMode();
        else
            EnterBasicAttackMode();
    }

    public void EnterBasicAttackMode()
    {
        isTargetingBasicAttack = true;
        RefreshButtonVisual();
        ApplyCursor();
    }

    public void CancelTargetingMode()
    {
        isTargetingBasicAttack = false;
        RefreshButtonVisual();
        ApplyCursor();
    }

    private void TryAttackUnderCursor()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, enemyMask))
            return;

        CharacterStats targetStats = hit.collider.GetComponentInParent<CharacterStats>();
        if (targetStats == null)
            return;

        bool attacked = basicAttack.TryAttackTarget(targetStats);

        if (attacked)
            CancelTargetingMode();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplyCursor()
    {
        if (isTargetingBasicAttack && attackCursor != null)
        {
            Cursor.SetCursor(attackCursor, attackCursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        }
    }

    private void RefreshButtonVisual()
    {
        if (basicAttackButtonImage == null)
            return;

        basicAttackButtonImage.color = isTargetingBasicAttack ? selectedButtonColor : normalButtonColor;
    }
}