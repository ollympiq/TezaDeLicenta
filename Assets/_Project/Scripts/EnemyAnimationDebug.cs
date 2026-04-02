using UnityEngine;

public class EnemyAnimationDebug : MonoBehaviour
{
    [SerializeField] private EnemyAnimationController anim;
    [SerializeField] private Transform target;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            anim.PlayAttackAnimation(target);
    }
}