using UnityEngine;
using UnityEngine.InputSystem;

public class TurnTestController : MonoBehaviour
{
    [SerializeField] private EnemyTurnController enemyTurn;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
            enemyTurn.StartTurn();
    }
}