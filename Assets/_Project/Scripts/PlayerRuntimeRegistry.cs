using UnityEngine;

public static class PlayerRuntimeRegistry
{
    public static GameObject CurrentPlayerRoot { get; private set; }

    public static void Register(GameObject playerRoot)
    {
        CurrentPlayerRoot = playerRoot;
    }

    public static void Clear(GameObject playerRoot)
    {
        if (CurrentPlayerRoot == playerRoot)
            CurrentPlayerRoot = null;
    }

    public static GameObject ResolvePlayerRoot()
    {
        if (CurrentPlayerRoot != null && CurrentPlayerRoot.activeInHierarchy)
            return CurrentPlayerRoot;

        PlayerTurnController turnController = Object.FindFirstObjectByType<PlayerTurnController>();
        if (turnController != null)
        {
            CurrentPlayerRoot = turnController.gameObject;
            return CurrentPlayerRoot;
        }

        PlayerNavMeshMover mover = Object.FindFirstObjectByType<PlayerNavMeshMover>();
        if (mover != null)
        {
            CurrentPlayerRoot = mover.gameObject;
            return CurrentPlayerRoot;
        }

        CharacterInventory inventory = Object.FindFirstObjectByType<CharacterInventory>();
        if (inventory != null)
        {
            CurrentPlayerRoot = inventory.gameObject;
            return CurrentPlayerRoot;
        }

        return null;
    }

    public static T Get<T>() where T : Component
    {
        GameObject root = ResolvePlayerRoot();
        return root != null ? root.GetComponent<T>() : null;
    }
}