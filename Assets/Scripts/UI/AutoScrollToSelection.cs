using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollToSelection : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform viewport;
    private RectTransform content;
    private GameObject lastSelectedObject;

    private void Awake()
    {
        InitReferences();
    }

    private void OnEnable()
    {
        InitReferences();
        lastSelectedObject = null;
    }

    private void InitReferences()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect != null)
        {
            if (viewport == null)
                viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

            if (content == null)
                content = scrollRect.content;
        }
    }

    private void Update()
    {
        GameObject currentSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (currentSelected != null && currentSelected != lastSelectedObject)
        {
            lastSelectedObject = currentSelected;
            ScrollToSelected(currentSelected);
        }
    }

    public void ResetScroll()
    {
        InitReferences();
        lastSelectedObject = null;

        if (content != null)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        }

        if (scrollRect != null)
        {
            scrollRect.velocity = Vector2.zero;
        }
    }

    private void ScrollToSelected(GameObject selected)
    {
        InitReferences();

        if (viewport == null || content == null)
            return;

        RectTransform targetRect = GetTargetRect(selected);
        if (targetRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        float viewportHeight = viewport.rect.height;
        float contentHeight = content.rect.height;

        if (contentHeight <= viewportHeight)
            return;

        // Get corners in world space, then convert to viewport local space
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        // corners: 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
        float rowTop = viewport.InverseTransformPoint(corners[1]).y;
        float rowBottom = viewport.InverseTransformPoint(corners[0]).y;

        float viewTop = viewport.rect.yMax;
        float viewBottom = viewport.rect.yMin;

        float margin = 8f;
        float currentY = content.anchoredPosition.y;
        float targetY = currentY;

        if (rowBottom < viewBottom + margin)
        {
            // Row is below the visible viewport -> scroll down (content moves UP)
            float diff = (viewBottom + margin) - rowBottom;
            targetY = currentY + diff;
        }
        else if (rowTop > viewTop - margin)
        {
            // Row is above the visible viewport -> scroll up (content moves DOWN)
            float diff = rowTop - (viewTop - margin);
            targetY = currentY - diff;
        }

        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
        targetY = Mathf.Clamp(targetY, 0f, maxScroll);

        if (!Mathf.Approximately(targetY, currentY))
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
            if (scrollRect != null)
                scrollRect.velocity = Vector2.zero;
        }
    }

    private RectTransform GetTargetRect(GameObject selected)
    {
        if (selected == null || content == null) return null;

        Transform current = selected.transform;
        if (!current.IsChildOf(content)) return null;

        // Find the direct child of content (e.g. LeaderboardRowUI) that encloses the selected control
        Transform directChild = current;
        while (directChild != null && directChild.parent != content)
        {
            directChild = directChild.parent;
        }

        return (directChild as RectTransform) ?? selected.GetComponent<RectTransform>();
    }
}
