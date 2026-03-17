using UnityEngine;
using UnityEngine.UI;

public static class UISkillDragState
{
    public static SkillDefinition CurrentSkill;

    private static GameObject dragIconObject;
    private static RectTransform dragIconRect;

    public static void BeginDrag(SkillDefinition skill, Canvas rootCanvas, Sprite icon, Vector2 startPosition)
    {
        Clear();

        CurrentSkill = skill;

        if (rootCanvas == null || icon == null)
            return;

        dragIconObject = new GameObject("SkillDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragIconObject.transform.SetParent(rootCanvas.transform, false);

        dragIconRect = dragIconObject.GetComponent<RectTransform>();

        Image image = dragIconObject.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;

        CanvasGroup cg = dragIconObject.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        dragIconRect.sizeDelta = new Vector2(48f, 48f);
        dragIconRect.position = startPosition;
    }

    public static void UpdateDrag(Vector2 position)
    {
        if (dragIconRect != null)
            dragIconRect.position = position;
    }

    public static void Clear()
    {
        CurrentSkill = null;

        if (dragIconObject != null)
            Object.Destroy(dragIconObject);

        dragIconObject = null;
        dragIconRect = null;
    }
}