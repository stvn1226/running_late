using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProgressTracking : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private RectTransform progressArrow;
    [SerializeField] private RectTransform fillMask;
    
    [Header("Retry Settings")]
    [SerializeField] private float retryDelay = 1f;
    [SerializeField] private int maxRetries = 5;
    
    private Transform endPoint;
    private Transform spawnPoint;
    private Transform player;
    private float levelLength;
    private float playerPositionX;
    private float playerProgress;
    private bool isInitialized = false;

    // Optimizations
    private float lastUpdatedProgress = -1f;
    private WaitForSeconds retryWait;
    
    public static ProgressTracking Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // Cache the WaitForSeconds object to prevent garbage collection spikes
        retryWait = new WaitForSeconds(retryDelay);
        
        UpdateProgressUI(0f);
        StartCoroutine(InitializeWithRetry());
    }

    private IEnumerator InitializeWithRetry()
    {
        int attempts = 0;
        
        while (!isInitialized && attempts < maxRetries)
        {
            attempts++;
            
            if (TryAssignTransforms())
            {
                CalculateLevelLength();
                isInitialized = true;
                Debug.Log($"ProgressTracking: Successfully initialized on attempt {attempts}");
                yield break;
            }
            
            Debug.Log($"ProgressTracking: Initialization attempt {attempts} failed, retrying in {retryDelay}s");
            yield return retryWait;
        }
        
        if (!isInitialized)
        {
            Debug.LogError("ProgressTracking: Failed to initialize after max retries. Required objects not found in scene.");
        }
    }

    void Update()
    {
        if (!isInitialized || player == null)
            return;

        if (endPoint == null || spawnPoint == null)
        {
            Debug.LogWarning("ProgressTracking: References lost, attempting to re-initialize");
            isInitialized = false;
            StartCoroutine(InitializeWithRetry());
            return;
        }
        
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        // Avoid repeated native property access
        float currentTargetX = endPoint.position.x;
        float currentPlayerX = player.position.x;
        
        playerPositionX = Mathf.Abs(currentTargetX - currentPlayerX);
        
        // Clamp to prevent out-of-bounds UI when player overshoots
        playerProgress = Mathf.Clamp01(1f - (playerPositionX / levelLength));
        
        // Only update UI if there's a visual change to save CPU & Canvas rebuilding
        if (Mathf.Abs(playerProgress - lastUpdatedProgress) > 0.001f)
        {
            UpdateProgressUI(playerProgress);
            lastUpdatedProgress = playerProgress;
        }
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
            float maskWidth = progressBar.rectTransform.rect.width * progress;

            if (progressArrow != null)
            {
                Vector2 arrowPos = progressArrow.anchoredPosition;
                arrowPos.x = maskWidth;
                progressArrow.anchoredPosition = arrowPos;
            }
            
            if (fillMask != null)
            {
                Vector2 maskSize = fillMask.sizeDelta;
                maskSize.x = maskWidth;
                fillMask.sizeDelta = maskSize;
            }
        }
    }

    private bool TryAssignTransforms()
    {
        // Cache references avoiding unnecessary FindGameObjectWithTag calls
        if (endPoint == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Finish");
            if (obj != null) endPoint = obj.transform;
        }

        if (spawnPoint == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Respawn");
            if (obj != null) spawnPoint = obj.transform;
        }

        if (player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) player = obj.transform;
        }
        
        return endPoint != null && spawnPoint != null && player != null;
    }
    
    private void CalculateLevelLength()
    {
        if (endPoint != null && spawnPoint != null)
        {
            levelLength = Mathf.Abs(endPoint.position.x - spawnPoint.position.x);
            // Prevent division by zero
            if (levelLength <= 0f)
            {
                levelLength = 0.01f;
            }
        }
    }
    
    public float GetProgressPercentage()
    {
        return playerProgress * 100f;
    }

    public float GetPlayerPositionX()
    {
        return player != null ? player.position.x : 0f;
    }
}
