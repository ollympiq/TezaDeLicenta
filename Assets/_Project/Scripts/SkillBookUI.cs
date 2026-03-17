using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillBookUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSkillLoadout loadout;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillBookItemUI itemPrefab;

    private readonly List<SkillBookItemUI> spawnedItems = new List<SkillBookItemUI>();

    private void Start()
    {
        if (loadout == null)
            loadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        RefreshBook();
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(TogglePanel);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
            return;

        bool nextState = !panelRoot.activeSelf;
        panelRoot.SetActive(nextState);

        if (nextState)
            RefreshBook();
        else
            UISkillDragState.Clear();
    }

    public void ClosePanel()
    {
        if (panelRoot == null)
            return;

        panelRoot.SetActive(false);
        UISkillDragState.Clear();
    }

    public void RefreshBook()
    {
        if (loadout == null || contentRoot == null || itemPrefab == null || rootCanvas == null)
            return;

        UISkillDragState.Clear();

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();

        for (int i = 0; i < loadout.AvailableSkills.Count; i++)
        {
            SkillDefinition skill = loadout.AvailableSkills[i];
            if (skill == null)
                continue;

            SkillBookItemUI item = Instantiate(itemPrefab, contentRoot);
            item.Bind(skill, rootCanvas);
            spawnedItems.Add(item);
        }
    }
}