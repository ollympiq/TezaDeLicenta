using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public static class PlayerSceneReferenceBinder
{
    private const BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void BindAllToPlayer(GameObject playerRoot)
    {
        if (playerRoot == null)
        {
            Debug.LogWarning("PlayerSceneReferenceBinder: playerRoot este null.");
            return;
        }

        PlayerRuntimeRegistry.Register(playerRoot);

        CharacterStats stats = playerRoot.GetComponent<CharacterStats>();
        CharacterHealth health = playerRoot.GetComponent<CharacterHealth>();
        CharacterEquipment equipment = playerRoot.GetComponent<CharacterEquipment>();
        CharacterInventory inventory = playerRoot.GetComponent<CharacterInventory>();
        PlayerSkillLoadout skillLoadout = playerRoot.GetComponent<PlayerSkillLoadout>();
        PlayerWallet wallet = playerRoot.GetComponent<PlayerWallet>();
        PlayerProgression progression = playerRoot.GetComponent<PlayerProgression>();
        PlayerAP ap = playerRoot.GetComponent<PlayerAP>();
        PlayerTurnController turnController = playerRoot.GetComponent<PlayerTurnController>();
        PlayerNavMeshMover mover = playerRoot.GetComponent<PlayerNavMeshMover>();
        PlayerCombatController combatController = playerRoot.GetComponent<PlayerCombatController>();
        NavMeshAgent agent = playerRoot.GetComponent<NavMeshAgent>();

        BindPlayerClassInitializers(stats, health, equipment, inventory, skillLoadout, wallet);
        BindCombatEntryHandlers(stats, health, equipment, inventory, skillLoadout, wallet, progression);
        BindCombatExitHandlers(equipment, inventory, skillLoadout, wallet, progression);
        BindLobbyEntryHandlers(stats, equipment, inventory, skillLoadout, wallet, progression);
        BindLobbySceneControllers(stats, equipment, inventory, skillLoadout, wallet, progression);
        BindLobbyInteractionControllers(playerRoot.transform, agent);
        BindTraderSystems(inventory, wallet);
        BindTurnManagers(turnController);
        BindCamera(playerRoot.transform);
        BindPlayerUI(stats, health, ap, progression, inventory, equipment, skillLoadout, wallet);
        BindCombatUI(turnController, mover, combatController, ap);
        BindEnemySystems(stats);

        RefreshKnownUI();
    }

    private static void BindPlayerClassInitializers(
        CharacterStats stats,
        CharacterHealth health,
        CharacterEquipment equipment,
        CharacterInventory inventory,
        PlayerSkillLoadout skillLoadout,
        PlayerWallet wallet)
    {
        PlayerClassInitializer[] initializers = FindAll<PlayerClassInitializer>();

        for (int i = 0; i < initializers.Length; i++)
        {
            SetField(initializers[i], "characterStats", stats);
            SetField(initializers[i], "characterHealth", health);
            SetField(initializers[i], "characterEquipment", equipment);
            SetField(initializers[i], "characterInventory", inventory);
            SetField(initializers[i], "playerSkillLoadout", skillLoadout);
            SetField(initializers[i], "playerWallet", wallet);
        }
    }

    private static void BindCombatEntryHandlers(
        CharacterStats stats,
        CharacterHealth health,
        CharacterEquipment equipment,
        CharacterInventory inventory,
        PlayerSkillLoadout skillLoadout,
        PlayerWallet wallet,
        PlayerProgression progression)
    {
        CombatEntryHandler[] handlers = FindAll<CombatEntryHandler>();

        for (int i = 0; i < handlers.Length; i++)
        {
            SetField(handlers[i], "playerProgression", progression);
            SetField(handlers[i], "playerWallet", wallet);
            SetField(handlers[i], "characterInventory", inventory);
            SetField(handlers[i], "characterEquipment", equipment);
            SetField(handlers[i], "playerSkillLoadout", skillLoadout);
            SetField(handlers[i], "characterStats", stats);
            SetField(handlers[i], "characterHealth", health);
        }
    }

    private static void BindCombatExitHandlers(
        CharacterEquipment equipment,
        CharacterInventory inventory,
        PlayerSkillLoadout skillLoadout,
        PlayerWallet wallet,
        PlayerProgression progression)
    {
        CombatExitToLobby[] handlers = FindAll<CombatExitToLobby>();

        for (int i = 0; i < handlers.Length; i++)
        {
            SetField(handlers[i], "playerProgression", progression);
            SetField(handlers[i], "playerWallet", wallet);
            SetField(handlers[i], "characterInventory", inventory);
            SetField(handlers[i], "characterEquipment", equipment);
            SetField(handlers[i], "playerSkillLoadout", skillLoadout);
        }
    }

    private static void BindLobbyEntryHandlers(
        CharacterStats stats,
        CharacterEquipment equipment,
        CharacterInventory inventory,
        PlayerSkillLoadout skillLoadout,
        PlayerWallet wallet,
        PlayerProgression progression)
    {
        LobbyEntryHandler[] handlers = FindAll<LobbyEntryHandler>();

        for (int i = 0; i < handlers.Length; i++)
        {
            SetField(handlers[i], "playerProgression", progression);
            SetField(handlers[i], "playerWallet", wallet);
            SetField(handlers[i], "characterInventory", inventory);
            SetField(handlers[i], "characterEquipment", equipment);
            SetField(handlers[i], "playerSkillLoadout", skillLoadout);
            SetField(handlers[i], "characterStats", stats);
        }
    }

    private static void BindLobbySceneControllers(
        CharacterStats stats,
        CharacterEquipment equipment,
        CharacterInventory inventory,
        PlayerSkillLoadout skillLoadout,
        PlayerWallet wallet,
        PlayerProgression progression)
    {
        LobbySceneController[] controllers = FindAll<LobbySceneController>();

        for (int i = 0; i < controllers.Length; i++)
        {
            SetField(controllers[i], "playerProgression", progression);
            SetField(controllers[i], "playerWallet", wallet);
            SetField(controllers[i], "characterInventory", inventory);
            SetField(controllers[i], "characterEquipment", equipment);
            SetField(controllers[i], "playerSkillLoadout", skillLoadout);
            SetField(controllers[i], "characterStats", stats);
        }
    }

    private static void BindLobbyInteractionControllers(Transform playerTransform, NavMeshAgent playerAgent)
    {
        LobbyInteractionController[] controllers = FindAll<LobbyInteractionController>();

        for (int i = 0; i < controllers.Length; i++)
        {
            SetField(controllers[i], "playerRoot", playerTransform);
            SetField(controllers[i], "playerAgent", playerAgent);
        }
    }

    private static void BindTraderSystems(CharacterInventory inventory, PlayerWallet wallet)
    {
        InventoryUI inventoryUI = FindFirst<InventoryUI>();

        TraderShopUI[] traderShopUIs = FindAll<TraderShopUI>();

        for (int i = 0; i < traderShopUIs.Length; i++)
        {
            SetField(traderShopUIs[i], "playerWallet", wallet);
            SetField(traderShopUIs[i], "playerInventory", inventory);
            SetField(traderShopUIs[i], "playerInventoryUI", inventoryUI);
        }
    }

    private static void BindTurnManagers(PlayerTurnController turnController)
    {
        TurnManager[] managers = FindAll<TurnManager>();

        for (int i = 0; i < managers.Length; i++)
            SetField(managers[i], "playerTurn", turnController);
    }

    private static void BindCamera(Transform playerTransform)
    {
        SimpleFollowCamera[] cameras = FindAll<SimpleFollowCamera>();

        for (int i = 0; i < cameras.Length; i++)
            SetField(cameras[i], "target", playerTransform);
    }

    private static void BindPlayerUI(
        CharacterStats stats,
        CharacterHealth health,
        PlayerAP ap,
        PlayerProgression progression,
        CharacterInventory inventory,
        CharacterEquipment equipment,
        PlayerSkillLoadout skillLoadout,
        PlayerWallet wallet)
    {
        APTextUI[] apTexts = FindAll<APTextUI>();
        for (int i = 0; i < apTexts.Length; i++)
            SetField(apTexts[i], "playerAP", ap);

        PlayerHealthBarUI[] healthBars = FindAll<PlayerHealthBarUI>();
        for (int i = 0; i < healthBars.Length; i++)
            SetField(healthBars[i], "targetHealth", health);

        StatsMenuUI[] statsMenus = FindAll<StatsMenuUI>();
        for (int i = 0; i < statsMenus.Length; i++)
            SetField(statsMenus[i], "targetStats", stats);

        PlayerStatAllocationUI[] allocationUIs = FindAll<PlayerStatAllocationUI>();
        for (int i = 0; i < allocationUIs.Length; i++)
        {
            SetField(allocationUIs[i], "progression", progression);
            SetField(allocationUIs[i], "stats", stats);
        }

        InventoryUI[] inventories = FindAll<InventoryUI>();
        for (int i = 0; i < inventories.Length; i++)
        {
            SetField(inventories[i], "inventory", inventory);
            SetField(inventories[i], "equipment", equipment);
        }

        SkillBookUI[] skillBooks = FindAll<SkillBookUI>();
        for (int i = 0; i < skillBooks.Length; i++)
            SetField(skillBooks[i], "loadout", skillLoadout);

        SkillBarUI[] skillBars = FindAll<SkillBarUI>();
        for (int i = 0; i < skillBars.Length; i++)
        {
            SetField(skillBars[i], "loadout", skillLoadout);
            SetField(skillBars[i], "combatController", PlayerRuntimeRegistry.Get<PlayerCombatController>());
        }

        SkillCodexUI[] codexUIs = FindAll<SkillCodexUI>();
        for (int i = 0; i < codexUIs.Length; i++)
            SetField(codexUIs[i], "loadout", skillLoadout);

        PlayerGoldUI[] goldUIs = FindAll<PlayerGoldUI>();
        for (int i = 0; i < goldUIs.Length; i++)
            SetField(goldUIs[i], "playerWallet", wallet);

        EnemyLootUI[] enemyLootUIs = FindAll<EnemyLootUI>();
        for (int i = 0; i < enemyLootUIs.Length; i++)
        {
            enemyLootUIs[i].SetPlayerReferences(inventory, wallet, inventory != null ? inventory.transform : null);

            SetField(enemyLootUIs[i], "playerInventory", inventory);
            SetField(enemyLootUIs[i], "playerWallet", wallet);
            SetField(enemyLootUIs[i], "playerTransform", inventory != null ? inventory.transform : null);
        }
    }

    private static void BindCombatUI(
        PlayerTurnController turnController,
        PlayerNavMeshMover mover,
        PlayerCombatController combatController,
        PlayerAP ap)
    {
        MoveAPCursorPreviewUI[] movePreviews = FindAll<MoveAPCursorPreviewUI>();

        for (int i = 0; i < movePreviews.Length; i++)
        {
            SetField(movePreviews[i], "mover", mover);
            SetField(movePreviews[i], "playerTurnController", turnController);
            SetField(movePreviews[i], "combatController", combatController);
            SetField(movePreviews[i], "playerAP", ap);
        }

        SkillAreaPreviewController[] areaPreviews = FindAll<SkillAreaPreviewController>();

        for (int i = 0; i < areaPreviews.Length; i++)
            SetField(areaPreviews[i], "combatController", combatController);

        MoveRangeGridVisualizer[] rangeVisualizers = FindAll<MoveRangeGridVisualizer>();

        for (int i = 0; i < rangeVisualizers.Length; i++)
        {
            SetField(rangeVisualizers[i], "playerAP", ap);
            SetField(rangeVisualizers[i], "mover", mover);
            SetField(rangeVisualizers[i], "center", mover != null ? mover.transform : null);
        }
    }

    private static void BindEnemySystems(CharacterStats playerStats)
    {
        EnemySpawner[] spawners = FindAll<EnemySpawner>();

        for (int i = 0; i < spawners.Length; i++)
            SetField(spawners[i], "playerStats", playerStats);

        EnemyTurnController[] enemies = FindAll<EnemyTurnController>();

        for (int i = 0; i < enemies.Length; i++)
            SetField(enemies[i], "targetStats", playerStats);
    }

    private static void RefreshKnownUI()
    {
        StatsMenuUI[] statsMenus = FindAll<StatsMenuUI>();
        for (int i = 0; i < statsMenus.Length; i++)
            statsMenus[i].Refresh();

        PlayerHealthBarUI[] healthBars = FindAll<PlayerHealthBarUI>();
        for (int i = 0; i < healthBars.Length; i++)
            healthBars[i].Refresh();

        PlayerStatAllocationUI[] allocations = FindAll<PlayerStatAllocationUI>();
        for (int i = 0; i < allocations.Length; i++)
            allocations[i].RefreshNow();

        SkillBookUI[] skillBooks = FindAll<SkillBookUI>();
        for (int i = 0; i < skillBooks.Length; i++)
            skillBooks[i].RefreshNow();

        SkillBarUI[] skillBars = FindAll<SkillBarUI>();
        for (int i = 0; i < skillBars.Length; i++)
            skillBars[i].RefreshNow();

        SkillCodexUI[] codexUIs = FindAll<SkillCodexUI>();
        for (int i = 0; i < codexUIs.Length; i++)
            codexUIs[i].RefreshNow();

        PlayerGoldUI[] goldUIs = FindAll<PlayerGoldUI>();
        for (int i = 0; i < goldUIs.Length; i++)
            goldUIs[i].RefreshNow();

        InventoryUI[] inventories = FindAll<InventoryUI>();
        for (int i = 0; i < inventories.Length; i++)
            inventories[i].RefreshAll();

        TraderShopUI[] traderShopUIs = FindAll<TraderShopUI>();
        for (int i = 0; i < traderShopUIs.Length; i++)
            traderShopUIs[i].RefreshAll();
    }

    private static T[] FindAll<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }

    private static T FindFirst<T>() where T : Object
    {
        T[] all = FindAll<T>();
        return all != null && all.Length > 0 ? all[0] : null;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        if (target == null || value == null)
            return;

        FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);

        if (field == null)
            return;

        if (!field.FieldType.IsInstanceOfType(value))
            return;

        field.SetValue(target, value);
    }
}