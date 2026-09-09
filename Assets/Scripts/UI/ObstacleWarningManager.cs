using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObstacleWarningManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject warningIndicatorPrefab;
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private Camera mainCamera;
    
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 45f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("UI Settings")]
    [SerializeField] private float screenEdgeOffset = 80f;
    [SerializeField] private int maxIndicators = 10;
    
    private List<WarningIndicator> indicatorPool;
    private List<IWarningIndicator> trackedObstacles;
    private RectTransform canvasRectTransform;
    private Collider2D[] colliderBuffer;
    private float canvasHalfHeight;
    
    private class WarningIndicator
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image obstacleImage;
        public bool isActive;
    }
    
    private void OnEnable()
    {
        if (indicatorPool == null)
        {
            InitializeReferences();
            InitializeIndicatorPool();
            trackedObstacles = new List<IWarningIndicator>(maxIndicators);
            colliderBuffer = new Collider2D[100];
        }
    }
    
    private void InitializeReferences()
    {
        if (mainCamera == null)
            mainCamera = GameReferences.Instance.MainCamera;
        
        if (gameCanvas == null)
            gameCanvas = FindFirstObjectByType<Canvas>();
        
        if (gameCanvas != null)
        {
            canvasRectTransform = gameCanvas.GetComponent<RectTransform>();
            canvasHalfHeight = canvasRectTransform.rect.height * 0.5f;
        }
    }
    
    private void InitializeIndicatorPool()
    {
        indicatorPool = new List<WarningIndicator>(maxIndicators);
        
        if (warningIndicatorPrefab == null || gameCanvas == null)
            return;
        
        for (int i = 0; i < maxIndicators; i++)
        {
            GameObject instance = Instantiate(warningIndicatorPrefab, gameCanvas.transform);
            
            // Handle nested prefab structure (Canvas wrapper)
            Transform actualIndicator = instance.transform.Find("ObstacleWarning");
            if (actualIndicator == null && instance.transform.childCount > 0)
            {
                Transform canvasChild = instance.transform.GetChild(0);
                actualIndicator = canvasChild.Find("ObstacleWarning");
            }
            
            GameObject indicatorGO = (actualIndicator != null) ? actualIndicator.gameObject : instance;
            
            // Reparent if needed
            if (indicatorGO != instance)
            {
                indicatorGO.transform.SetParent(gameCanvas.transform, false);
                Destroy(instance);
            }
            
            Image[] images = indicatorGO.GetComponentsInChildren<Image>();
            
            WarningIndicator indicator = new WarningIndicator
            {
                gameObject = indicatorGO,
                rectTransform = indicatorGO.GetComponent<RectTransform>(),
                obstacleImage = images.Length >= 2 ? images[1] : null,
                isActive = false
            };
            
            indicatorGO.SetActive(false);
            indicatorPool.Add(indicator);
        }
    }
    
    private void Update()
    {
        if (mainCamera == null || canvasRectTransform == null)
            return;

        // Do not show obstacle warnings while controls tutorial is active
        if (UI.GamePanels.ControlsTutorialOverlay.IsShowingTutorial)
        {
            if (indicatorPool != null)
            {
                for (int i = 0; i < indicatorPool.Count; i++)
                {
                    if (indicatorPool[i] != null && indicatorPool[i].isActive)
                    {
                        indicatorPool[i].gameObject.SetActive(false);
                        indicatorPool[i].isActive = false;
                    }
                }
            }
            return;
        }
        
        FindObstaclesInRange();
        UpdateIndicators();
    }
    
    private void FindObstaclesInRange()
    {
        trackedObstacles.Clear();
        
        Vector3 cameraPos = mainCamera.transform.position;
        int count = Physics2D.OverlapCircleNonAlloc(cameraPos, detectionRange, colliderBuffer, obstacleLayer);
        
        for (int i = 0; i < count; i++)
        {
            Collider2D col = colliderBuffer[i];
            IWarningIndicator obstacle = col.GetComponent<IWarningIndicator>() ?? col.GetComponentInParent<IWarningIndicator>();
            
            if (obstacle != null && obstacle.ShouldShowWarning())
            {
                Vector3 obstaclePos = obstacle.GetPosition();
                float distanceX = obstaclePos.x - cameraPos.x;
                
                if (distanceX > 0 && !IsObstacleVisible(obstaclePos))
                {
                    trackedObstacles.Add(obstacle);
                }
            }
        }
    }
    
    private bool IsObstacleVisible(Vector3 worldPosition)
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldPosition);
        return viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z > 0;
    }
    
    private void UpdateIndicators()
    {
        int obstacleCount = trackedObstacles.Count;
        
        for (int i = 0; i < indicatorPool.Count; i++)
        {
            WarningIndicator indicator = indicatorPool[i];
            
            if (i < obstacleCount)
            {
                if (!indicator.isActive)
                {
                    indicator.gameObject.SetActive(true);
                    indicator.isActive = true;
                }
                
                UpdateIndicatorPosition(indicator, trackedObstacles[i]);
                UpdateIndicatorSprite(indicator, trackedObstacles[i]);
            }
            else if (indicator.isActive)
            {
                indicator.gameObject.SetActive(false);
                indicator.isActive = false;
            }
        }
    }
    
    private void UpdateIndicatorPosition(WarningIndicator indicator, IWarningIndicator obstacle)
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(obstacle.GetPosition());
        
        float xPos = -screenEdgeOffset;
        float yPos = (screenPos.y / Screen.height) * canvasRectTransform.rect.height - canvasHalfHeight;
        yPos = Mathf.Clamp(yPos, -canvasHalfHeight + 50f, canvasHalfHeight - 50f);
        
        indicator.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
    }
    
    private void UpdateIndicatorSprite(WarningIndicator indicator, IWarningIndicator obstacle)
    {
        if (indicator.obstacleImage != null)
        {
            Sprite sprite = obstacle.GetWarningSprite();
            if (sprite != null)
            {
                indicator.obstacleImage.sprite = sprite;
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(mainCamera.transform.position, detectionRange);
        }
    }
}
