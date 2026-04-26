using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLogUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TextMeshProUGUI entryPrefab;

    [Header("Behavior")]
    [SerializeField] private int maxEntries = 200;
    [SerializeField] private bool autoScrollToBottom = true;
    [SerializeField] private bool showTimestamp = true;

    [Header("Colors")]
    [SerializeField] private Color infoColor = Color.white;
    [SerializeField] private Color combatColor = new Color(1f, 0.82f, 0.35f);
    [SerializeField] private Color successColor = new Color(0.45f, 1f, 0.45f);
    [SerializeField] private Color warningColor = new Color(1f, 0.7f, 0.2f);
    [SerializeField] private Color errorColor = new Color(1f, 0.35f, 0.35f);

    private readonly Queue<TextMeshProUGUI> spawnedEntries = new Queue<TextMeshProUGUI>();
    private bool initialized;

    private void Awake()
    {
        if (entryPrefab != null)
            entryPrefab.gameObject.SetActive(false);

        initialized = true;
    }

    private void OnEnable()
    {
        GameLog.OnEntryAdded += HandleEntryAdded;

        if (initialized)
            RebuildFromHistory();
    }

    private void OnDisable()
    {
        GameLog.OnEntryAdded -= HandleEntryAdded;
    }

    public void ClearVisualLog()
    {
        while (spawnedEntries.Count > 0)
        {
            TextMeshProUGUI entry = spawnedEntries.Dequeue();
            if (entry != null)
                Destroy(entry.gameObject);
        }
    }

    private void RebuildFromHistory()
    {
        ClearVisualLog();

        IReadOnlyList<GameLogEntry> history = GameLog.Entries;
        for (int i = 0; i < history.Count; i++)
            SpawnEntry(history[i], false);

        ForceScrollToBottom();
    }

    private void HandleEntryAdded(GameLogEntry entry)
    {
        SpawnEntry(entry, autoScrollToBottom);
    }

    private void SpawnEntry(GameLogEntry entry, bool scrollToBottom)
    {
        if (entryPrefab == null || contentRoot == null)
            return;

        TextMeshProUGUI label = Instantiate(entryPrefab, contentRoot);
        label.gameObject.SetActive(true);
        label.text = BuildEntryText(entry);
        label.color = GetColor(entry.Type);

        spawnedEntries.Enqueue(label);

        while (spawnedEntries.Count > maxEntries)
        {
            TextMeshProUGUI oldest = spawnedEntries.Dequeue();
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (scrollToBottom)
            ForceScrollToBottom();
    }

    private string BuildEntryText(GameLogEntry entry)
    {
        if (!showTimestamp)
            return entry.Message;

        return $"[{entry.Timestamp:HH:mm:ss}] {entry.Message}";
    }

    private Color GetColor(GameLogType type)
    {
        switch (type)
        {
            case GameLogType.Combat: return combatColor;
            case GameLogType.Success: return successColor;
            case GameLogType.Warning: return warningColor;
            case GameLogType.Error: return errorColor;
            default: return infoColor;
        }
    }

    private void ForceScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}