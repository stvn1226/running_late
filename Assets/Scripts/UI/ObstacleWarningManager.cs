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
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private List<WarningIndicator> indicatorPool;
    private List<IWarningIndicator> trackedObstacles;
    private RectTransform canvasRectTransform;
    
    private class WarningIndicator
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image arrowImage;
        public Image obstacleImage;
        public bool isActive;
        public IWarningIndicator targetObstacle;
    }
    
    private void Awake()
    {
        Debug.Log("[OBSTACLE_WARNING] *** AWAKE CALLED ***");
    }
    
    private void OnEnable()
    {
        Debug.Log("[OBSTACLE_WARNING] *** OnEnable CALLED ***");
        if (indicatorPool == null)
        {
            try
            {
                GameLogger.Log("OBSTACLE_WARNING", "=== ObstacleWarningManager Initializing ===");
                InitializeReferences();
                GameLogger.Log("OBSTACLE_WARNING", "References initialized");
                InitializeIndicatorPool();
                GameLogger.Log("OBSTACLE_WARNING", "Indicator pool initialized");
                trackedObstacles = new List<IWarningIndicator>();
                GameLogger.Log("OBSTACLE_WARNING", $"Initialization complete - Pool:{indicatorPool?.Count ?? 0}, Canvas:{(gameCanvas != null ? gameCanvas.name : "NULL")}, Camera:{(mainCamera != null ? mainCamera.name : "NULL")}");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError("OBSTACLE_WARNING", $"ERROR in OnEnable: {e.Message}\n{e.StackTrace}");
            }
        }
    }
    
    private void Start()
    {
        Debug.Log("[OBSTACLE_WARNING] *** START CALLED ***");
    }
    
    private void InitializeReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = GameReferences.Instance.MainCamera;
        }
        
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
        }
        
        if (gameCanvas != null)
        {
            canvasRectTransform = gameCanvas.GetComponent<RectTransform>();
        }
        
        if (warningIndicatorPrefab == null)
        {
            GameLogger.LogError("OBSTACLE_WARNING", "Warning indicator prefab is not assigned!");
        }
    }
    
    private void InitializeIndicatorPool()
    {
        indicatorPool = new List<WarningIndicator>();
        
        if (warningIndicatorPrefab == null || gameCanvas == null)
        {
            GameLogger.LogError("OBSTACLE_WARNING", $"Cannot initialize pool - Prefab:{(warningIndicatorPrefab != null)}, Canvas:{(gameCanvas != null)}");
            return;
        }
        
        for (int i = 0; i < maxIndicators; i++)
        {
            GameObject indicatorObj = Instantiate(warningIndicatorPrefab, gameCanvas.transform);
            
            // Try to find the actual indicator (it might be nested in a Canvas wrapper)
            Transform actualIndicator = indicatorObj.transform.Find("ObstacleWarning");
            if (actualIndicator == null)
            {
                // Check if there's a Canvas child first
                Transform canvasChild = indicatorObj.transform.GetChild(0);
                if (canvasChild != null)
                {
                    actualIndicator = canvasChild.Find("ObstacleWarning");
                }
            }
            
            // If still not found, just use the root object itself
            if (actualIndicator == null)
            {
                actualIndicator = indicatorObj.transform;
            }
            
            GameObject indicatorGameObject = actualIndicator.gameObject;
            
            // If we extracted a child, reparent it and destroy the wrapper
            if (actualIndicator != indicatorObj.transform)
            {
                indicatorGameObject.transform.SetParent(gameCanvas.transform, false);
                Destroy(indicatorObj);
            }
            
            WarningIndicator indicator = new WarningIndicator
            {
                gameObject = indicatorGameObject,
                rectTransform = indicatorGameObject.GetComponent<RectTransform>(),
                isActive = false
            };
            
            Image[] images = indicatorGameObject.GetComponentsInChildren<Image>();
            if (images.Length >= 1)
            {
                indicator.arrowImage = images[0];
            }
            if (images.Length >= 2)
            {
                indicator.obstacleImage = images[1];
            }
            
            indicatorGameObject.SetActive(false);
            indicatorPool.Add(indicator);
        }
        
        GameLogger.Log("OBSTACLE_WARNING", $"Created {indicatorPool.Count} warning indicators");
    }
    
    private void Update()
    {
        if (mainCamera == null || canvasRectTransform == null)
        {
            if (showDebugLogs)
            {
                GameLogger.LogWarning("OBSTACLE_WARNING", $"Update skipped - Camera:{(mainCamera != null)}, Canvas:{(canvasRectTransform != null)}");
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
        float searchRadius = detectionRange;
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(cameraPos, searchRadius, obstacleLayer);
        
        if (showDebugLogs && colliders.Length > 0)
        {
            GameLogger.Log("OBSTACLE_WARNING", $"Found {colliders.Length} colliders in range");
        }
        
        foreach (Collider2D col in colliders)
        {
            IWarningIndicator obstacle = col.GetComponent<IWarningIndicator>();
            
            if (obstacle == null)
            {
                obstacle = col.GetComponentInParent<IWarningIndicator>();
            }
            
            if (obstacle != null && obstacle.ShouldShowWarning())
            {
                Vector3 obstaclePos = obstacle.GetPosition();
                float distanceX = obstaclePos.x - cameraPos.x;
                
                if (distanceX > 0 && !IsObstacleVisible(obstaclePos))
                {
                    trackedObstacles.Add(obstacle);
                    
                    if (showDebugLogs)
                    {
                        GameLogger.Log("OBSTACLE_WARNING", $"Tracking obstacle at X:{obstaclePos.x:F1}, distance:{distanceX:F1}");
                    }
                }
                else if (showDebugLogs)
                {
                    GameLogger.Log("OBSTACLE_WARNING", $"Obstacle at X:{obstaclePos.x:F1} is visible or behind camera (distX:{distanceX:F1})");
                }
            }
            else if (showDebugLogs && obstacle == null)
            {
                GameLogger.LogWarning("OBSTACLE_WARNING", $"Collider {col.gameObject.name} has no IWarningIndicator component");
            }
        }
        
        if (showDebugLogs)
        {
            GameLogger.Log("OBSTACLE_WARNING", $"Tracking {trackedObstacles.Count} obstacles for warning display");
        }
    }
    
    private bool IsObstacleVisible(Vector3 worldPosition)
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldPosition);
        return viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z > 0;
    }
    
    private void UpdateIndicators()
    {
        for (int i = 0; i < indicatorPool.Count; i++)
        {
            WarningIndicator indicator = indicatorPool[i];
            
            if (i < trackedObstacles.Count)
            {
                IWarningIndicator obstacle = trackedObstacles[i];
                
                if (!indicator.isActive)
                {
                    indicator.gameObject.SetActive(true);
                    indicator.isActive = true;
                }
                
                indicator.targetObstacle = obstacle;
                UpdateIndicatorPosition(indicator, obstacle);
                UpdateIndicatorSprite(indicator, obstacle);
            }
            else
            {
                if (indicator.isActive)
                {
                    indicator.gameObject.SetActive(false);
                    indicator.isActive = false;
                    indicator.targetObstacle = null;
                }
            }
        }
    }
    
    private void UpdateIndicatorPosition(WarningIndicator indicator, IWarningIndicator obstacle)
    {
        Vector3 obstaclePos = obstacle.GetPosition();
        Vector3 screenPos = mainCamera.WorldToScreenPoint(obstaclePos);
        
        float canvasHeight = canvasRectTransform.rect.height;
        
        // X position: since the indicator is anchored to the right edge (1, 0.5),
        // we use negative offset to move it left from the right edge
        float xPos = -screenEdgeOffset;
        
        // Y position: convert screen Y to canvas Y (centered at 0)
        float yPos = (screenPos.y / Screen.height) * canvasHeight - (canvasHeight / 2f);
        
        // Clamp Y to keep indicators on screen
        yPos = Mathf.Clamp(yPos, -canvasHeight / 2f + 50f, canvasHeight / 2f - 50f);
        
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
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(mainCamera.transform.position, detectionRange);
        }
    }
}
