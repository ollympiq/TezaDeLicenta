using UnityEngine;

[RequireComponent(typeof(CharacterHealth))]
public class EnemyHealthBarSpawner : MonoBehaviour
{
    [SerializeField] private Transform followTarget;

    private void Start()
    {
        CharacterHealth health = GetComponent<CharacterHealth>();

        if (followTarget == null)
            followTarget = transform;

        if (WorldHealthBarManager.Instance != null)
            WorldHealthBarManager.Instance.CreateBar(health, followTarget);
    }
}