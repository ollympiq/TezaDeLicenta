using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterBasicAttack))]
public class TestPlayerAttackInput : MonoBehaviour
{
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float searchRadius = 3f;

    private CharacterBasicAttack basicAttack;

    private void Awake()
    {
        basicAttack = GetComponent<CharacterBasicAttack>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryAttackNearestEnemy();
        }
    }

    private void TryAttackNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, enemyMask);

        CharacterStats bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            CharacterStats targetStats = hit.GetComponentInParent<CharacterStats>();
            if (targetStats == null || targetStats.gameObject == gameObject)
                continue;

            CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();
            if (targetHealth == null || targetHealth.IsDead)
                continue;

            Vector3 a = transform.position;
            Vector3 b = targetStats.transform.position;
            a.y = 0f;
            b.y = 0f;

            float dist = Vector3.Distance(a, b);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestTarget = targetStats;
            }
        }

        if (bestTarget == null)
        {
            Debug.Log("Nu exista niciun inamic in raza de atac.");
            return;
        }

        basicAttack.TryAttackTarget(bestTarget);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
#endif
}