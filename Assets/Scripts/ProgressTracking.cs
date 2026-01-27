using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProgressTracking : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private RectTransform progressArrow;
    
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
            yield return new WaitForSeconds(retryDelay);
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
        playerPositionX = Mathf.Abs(endPoint.position.x - player.position.x);
        playerProgress = 1 - (playerPositionX / levelLength);
        
        if (progressBar != null)
        {
            progressBar.fillAmount = playerProgress;
        }
        
        if (progressArrow != null && progressBar != null)
        {
            progressArrow.anchoredPosition = new Vector2(
                progressBar.rectTransform.rect.width * playerProgress, 
                progressArrow.anchoredPosition.y
            );
        }
    }

    private bool TryAssignTransforms()
    {
        try
        {
            GameObject endPointObj = GameObject.FindGameObjectWithTag("Finish");
            GameObject spawnPointObj = GameObject.FindGameObjectWithTag("Respawn");
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            
            if (endPointObj == null || spawnPointObj == null || playerObj == null)
            {
                return false;
            }
            
            endPoint = endPointObj.transform;
            spawnPoint = spawnPointObj.transform;
            player = playerObj.transform;
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"ProgressTracking: Exception during transform assignment: {ex.Message}");
            return false;
        }
    }
    
    private void CalculateLevelLength()
    {
        if (endPoint != null && spawnPoint != null)
        {
            levelLength = Mathf.Abs(endPoint.position.x - spawnPoint.position.x);
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
