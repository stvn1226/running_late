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
        scrollRect = GetComponent<ScrollRect>();
        viewport = scrollRect.viewport;
        content = scrollRect.content;
    }
    
    private void Update()
    {
        GameObject currentSelected = EventSystem.current?.currentSelectedGameObject;
        
        if (currentSelected != null && currentSelected != lastSelectedObject)
        {
            lastSelectedObject = currentSelected;
            ScrollToSelected(currentSelected);
        }
    }
    
    private void ScrollToSelected(GameObject selected)
    {
        RectTransform selectedRect = selected.GetComponent<RectTransform>();
        
        if (selectedRect == null || !selectedRect.IsChildOf(content))
            return;
        
        Canvas.ForceUpdateCanvases();
        
        float viewportHeight = viewport.rect.height;
        float contentHeight = content.rect.height;
        
        if (contentHeight <= viewportHeight)
            return;
        
        float elementY = Mathf.Abs(selectedRect.anchoredPosition.y);
        float elementHeight = selectedRect.rect.height;
        
        float currentScrollPos = content.anchoredPosition.y;
        
        float elementTop = elementY;
        float elementBottom = elementY + elementHeight;
        
        float visibleTop = currentScrollPos;
        float visibleBottom = currentScrollPos + viewportHeight;
        
        float targetScrollPos = currentScrollPos;
        
        if (elementTop < visibleTop)
        {
            targetScrollPos = elementTop;
        }
        else if (elementBottom > visibleBottom)
        {
            targetScrollPos = elementBottom - viewportHeight;
        }
        
        targetScrollPos = Mathf.Clamp(targetScrollPos, 0f, contentHeight - viewportHeight);
        
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetScrollPos);
    }
}
