using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillAreaPreviewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerCombatController combatController;

    [Header("World Preview")]
    [SerializeField] private Transform circlePreview;
    [SerializeField] private float previewYOffset = 0.03f;
    [SerializeField] private float previewThickness = 0.05f;
    [SerializeField] private bool disablePreviewCollidersOnAwake = true;

    [Header("UI Preview")]
    [SerializeField] private TMP_Text affectedCountText;
    [SerializeField] private Vector2 textScreenOffset = new Vector2(120f, 80f);
    [SerializeField] private bool clampTextInsideScreen = true;
    [SerializeField] private float screenPadding = 20f;

    [Header("Raycast")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float rayDistance = 500f;

    [Header("Behavior")]
    [SerializeField] private bool hideWhenNoTargets = false;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (disablePreviewCollidersOnAwake && circlePreview != null)
        {
            Collider[] colliders = circlePreview.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        if (affectedCountText != null)
        {
            affectedCountText.raycastTarget = false;

            CanvasGroup cg = affectedCountText.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = affectedCountText.gameObject.AddComponent<CanvasGroup>();

            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        HidePreview();
    }

    private void LateUpdate()
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (combatController == null || mainCamera == null)
        {
            HidePreview();
            return;
        }

        SkillDefinition skill = combatController.CurrentSelectedSkill;

        if (skill == null || skill.AreaMode != SkillAreaMode.Circle)
        {
            HidePreview();
            return;
        }

        if (!TryGetPreviewCenter(skill, out Vector3 center))
        {
            HidePreview();
            return;
        }

        float radius = Mathf.Max(0.1f, skill.AreaRadius);
        int affectedCount = CountAffectedEnemies(center, radius);

        if (hideWhenNoTargets && affectedCount <= 0)
        {
            HidePreview();
            return;
        }

        ShowCircle(center, radius);
        ShowText(center, affectedCount);
    }

    private bool TryGetPreviewCenter(SkillDefinition skill, out Vector3 center)
    {
        center = Vector3.zero;

        if (Mouse.current == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (skill.TargetingMode == SkillTargetingMode.Enemy)
        {
            if (Physics.Raycast(ray, out RaycastHit enemyHit, rayDistance, enemyMask, QueryTriggerInteraction.Ignore))
            {
                Transform enemyRoot = GetEnemyRoot(enemyHit.collider.transform);
                center = GetGroundedPreviewCenter(enemyHit.collider, enemyRoot);
                return true;
            }

            return false;
        }

        if (Physics.Raycast(ray, out RaycastHit groundHit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            center = groundHit.point;
            return true;
        }

        return false;
    }

    private int CountAffectedEnemies(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyMask, QueryTriggerInteraction.Collide);

        int count = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            CharacterHealth health = hits[i].GetComponentInParent<CharacterHealth>();
            if (health == null)
                continue;

            if (health.IsDead)
                continue;

            count++;
        }

        return count;
    }

    private Transform GetEnemyRoot(Transform t)
    {
        if (t == null)
            return null;

        CharacterHealth health = t.GetComponentInParent<CharacterHealth>();
        if (health != null)
            return health.transform;

        EnemyTurnController enemyTurn = t.GetComponentInParent<EnemyTurnController>();
        if (enemyTurn != null)
            return enemyTurn.transform;

        return t.root;
    }

    private void ShowCircle(Vector3 center, float radius)
    {
        if (circlePreview == null)
            return;

        if (!circlePreview.gameObject.activeSelf)
            circlePreview.gameObject.SetActive(true);

        circlePreview.position = new Vector3(center.x, center.y + previewYOffset, center.z);

        float diameter = radius * 2f;
        circlePreview.localScale = new Vector3(diameter, previewThickness, diameter);
    }

    private void ShowText(Vector3 center, int affectedCount)
    {
        if (affectedCountText == null || mainCamera == null)
            return;

        if (!affectedCountText.gameObject.activeSelf)
            affectedCountText.gameObject.SetActive(true);

        affectedCountText.text = "Afectează:\n" + affectedCount;

        Vector3 worldAnchor = center + new Vector3(0f, previewYOffset, 0f);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldAnchor);

        Vector2 finalPos = new Vector2(screenPos.x, screenPos.y) + textScreenOffset;

        if (clampTextInsideScreen)
        {
            RectTransform rt = affectedCountText.rectTransform;

            float width = rt.rect.width > 0f ? rt.rect.width : 200f;
            float height = rt.rect.height > 0f ? rt.rect.height : 80f;

            finalPos.x = Mathf.Clamp(finalPos.x, screenPadding, Screen.width - width - screenPadding);
            finalPos.y = Mathf.Clamp(finalPos.y, screenPadding, Screen.height - height - screenPadding);
        }

        affectedCountText.rectTransform.position = finalPos;
    }
    private Vector3 GetGroundedPreviewCenter(Collider hitCollider, Transform enemyRoot)
    {
        Vector3 fallback = hitCollider.bounds.center;
        fallback.y = hitCollider.bounds.min.y;

        Vector3 rayStart;

        if (enemyRoot != null)
            rayStart = enemyRoot.position + Vector3.up * 5f;
        else
            rayStart = hitCollider.bounds.center + Vector3.up * 5f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, 20f, groundMask, QueryTriggerInteraction.Ignore))
            return groundHit.point;

        return fallback;
    }
    private void HidePreview()
    {
        if (circlePreview != null && circlePreview.gameObject.activeSelf)
            circlePreview.gameObject.SetActive(false);

        if (affectedCountText != null && affectedCountText.gameObject.activeSelf)
            affectedCountText.gameObject.SetActive(false);
    }
}