using UnityEngine;

[DisallowMultipleComponent]
public class TraderInteractable : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string traderDisplayName = "Trader";

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField, Min(0.5f)] private float interactionDistance = 2f;

    [Header("UI")]
    [SerializeField] private TraderShopUI shopUI;

    public string TraderDisplayName => string.IsNullOrWhiteSpace(traderDisplayName) ? name : traderDisplayName;
    public float InteractionDistance => Mathf.Max(0.5f, interactionDistance);

    public Vector3 GetInteractionPosition()
    {
        return interactionPoint != null ? interactionPoint.position : transform.position;
    }

    public bool IsInRange(Vector3 worldPosition, float extraSlack = 0f)
    {
        Vector3 a = worldPosition;
        Vector3 b = GetInteractionPosition();

        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b) <= InteractionDistance + Mathf.Max(0f, extraSlack);
    }

    public void OpenShop()
    {
        if (shopUI == null)
        {
            Debug.LogWarning($"Trader {TraderDisplayName} nu are Shop UI asignat.");
            return;
        }

        shopUI.OpenForTrader(this);
    }

    public void CloseShop()
    {
        if (shopUI != null)
            shopUI.Close();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = GetInteractionPosition();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, InteractionDistance);

        if (interactionPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, interactionPoint.position);
            Gizmos.DrawSphere(interactionPoint.position, 0.15f);
        }
    }
}