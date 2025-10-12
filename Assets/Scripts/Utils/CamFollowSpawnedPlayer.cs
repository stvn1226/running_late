using Unity.Cinemachine;
using UnityEngine;
using System.Collections;
using Utils;

public class CamFollowSpawnedPlayer : MonoBehaviour
{
    private CinemachineCamera cinemachineCamera;
    private CinemachineConfiner2D confiner;
    private GameObject player;
    
    private float currentTime = 0f;
    private float maxTime = 0.75f;
    
    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();
    }
    
    void Start()
    {
        StartCoroutine(FindAndFollowPlayer());
        
        if (LevelStarter.Instance != null)
        {
            LevelStarter.Instance.OnLevelGenerationStarted += OnLevelClearing;
            LevelStarter.Instance.OnLevelGenerationCompleted += OnLevelReady;
        }
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReplayStarted += OnLevelRestart;
        }
    }
    
    private void OnLevelClearing()
    {
        ClearCameraBounds();
    }
    
    private void OnLevelRestart()
    {
        player = null;
        ClearCameraBounds();
        StartCoroutine(FindAndFollowPlayer());
    }
    
    private void ClearCameraBounds()
    {
        if (confiner != null)
        {
            confiner.BoundingShape2D = null;
            confiner.InvalidateBoundingShapeCache();
        }
    }
    
    private void OnLevelReady()
    {
        StartCoroutine(UpdateCameraBounds());
    }
    
    private IEnumerator UpdateCameraBounds()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        PolygonCollider2D bounds = null;
        PolygonCollider2D[] allBounds = FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None);
        
        foreach (PolygonCollider2D bound in allBounds)
        {
            if (bound.gameObject.layer == LayerMask.NameToLayer("CamBounds") || 
                bound.name.Contains("CameraBounds") || 
                bound.CompareTag(TagUtils.CAMBOUNDS_TAG))
            {
                bounds = bound;
                break;
            }
        }
        
        if (bounds == null && allBounds.Length > 0)
        {
            bounds = allBounds[0];
        }
        
        if (bounds != null && confiner != null)
        {
            confiner.BoundingShape2D = bounds;
            confiner.InvalidateBoundingShapeCache();
            GameLogger.Log("CAMERA", $"Camera bounds updated with {bounds.points.Length} points");
        }
        else
        {
            GameLogger.LogWarning("CAMERA", "No camera bounds found!");
        }
    }

    private void Update()
    {
        if (player == null && currentTime >= maxTime)
        {
            StartCoroutine(FindAndFollowPlayer());
            currentTime = 0f;
        }
        else
        {
            currentTime += Time.deltaTime;
        }
    }

    private IEnumerator FindAndFollowPlayer()
    {
        yield return null;
        yield return null;
        
        player = null;
        int attempts = 0;
        int maxAttempts = 15;
        
        while (player == null && attempts < maxAttempts)
        {
            if (SpawnManager.Instance != null)
            {
                player = SpawnManager.Instance.GetCurrentPlayer();
            }
            
            if (player == null)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
        }
        
        if (player != null && cinemachineCamera != null)
        {
            PanCamObject panCam = player.GetComponentInChildren<PanCamObject>();
            Transform target = panCam != null ? panCam.transform : player.transform;
            
            cinemachineCamera.Target.TrackingTarget = target;
            cinemachineCamera.ForceCameraPosition(target.position, target.rotation);
            
            GameLogger.Log("CAMERA", "Camera now following player");
        }
    }
    
    private void OnDestroy()
    {
        if (LevelStarter.Instance != null)
        {
            LevelStarter.Instance.OnLevelGenerationStarted -= OnLevelClearing;
            LevelStarter.Instance.OnLevelGenerationCompleted -= OnLevelReady;
        }
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelReplayStarted -= OnLevelRestart;
        }
    }
}
