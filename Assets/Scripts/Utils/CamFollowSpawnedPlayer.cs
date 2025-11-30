using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CamFollowSpawnedPlayer : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera cinemachineCamera;
    public CinemachineConfiner2D confiner;
    public Camera mainCamera;
    
    [Header("Settings")]
    public int menuPriority = 5;
    public int gameplayPriority = 10;
    
    [Header("Menu Camera Behavior")]
    public bool disableLookaheadInMenu = true;
    public Vector3 menuDampingSpeed = new Vector3(0.3f, 0.5f, 0);
    public float menuConfinerDamping = 0f;
    
    [Header("Transition")]
    public float delayBeforeRestoringSettings = 1f;
    
    private Transform playerTransform;
    private bool isFollowingPlayer = false;
    private CinemachinePositionComposer composer;
    
    private Vector3 originalDamping;
    private bool originalLookaheadEnabled;
    private float originalConfinerDamping;
    private bool settingsSaved = false;

    private void Awake()
    {
        if (cinemachineCamera != null)
        {
            // cinemachineCamera.Follow = null;
            cinemachineCamera.Priority.Value = menuPriority;
            
            composer = cinemachineCamera.GetComponent<CinemachinePositionComposer>();
            
            if (composer != null && !settingsSaved)
            {
                originalDamping = composer.Damping;
                originalLookaheadEnabled = composer.Lookahead.Enabled;
                settingsSaved = true;
                
                GameLogger.Log("CAMERA", $"Original settings saved: Damping={originalDamping}, Lookahead={originalLookaheadEnabled}");
            }
        }
        
        if (confiner != null)
        {
            originalConfinerDamping = confiner.Damping;
            
            GameLogger.Log("CAMERA", "Confiner stays ENABLED from start (never disabled)");
        }
        
        GameLogger.Log("CAMERA", $"Camera initialized - Priority={menuPriority} (menu mode)");
    }

    private void Start()
    {
        if (LevelStarter.Instance != null)
        {
            LevelStarter.Instance.OnCameraBoundsUpdated += OnCameraBoundsUpdated;
        }
        
        ApplyMenuCameraSettings();
    }

    private void OnEnable()
    {
        SpawnManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        SpawnManager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnDestroy()
    {
        if (LevelStarter.Instance != null)
        {
            LevelStarter.Instance.OnCameraBoundsUpdated -= OnCameraBoundsUpdated;
        }
    }

    private void ApplyMenuCameraSettings()
    {
        if (composer != null)
        {
            composer.Damping = menuDampingSpeed;
            
            if (disableLookaheadInMenu)
            {
                composer.Lookahead.Enabled = false;
            }
            
            GameLogger.Log("CAMERA", $"Menu settings applied: Damping={menuDampingSpeed}, Lookahead disabled");
        }
        
        if (confiner != null)
        {
            confiner.Damping = menuConfinerDamping;
            GameLogger.Log("CAMERA", $"Confiner damping set to {menuConfinerDamping} (menu mode)");
        }
    }

    private void RestoreGameplayCameraSettings()
    {
        if (composer != null && settingsSaved)
        {
            composer.Damping = originalDamping;
            composer.Lookahead.Enabled = originalLookaheadEnabled;
            
            GameLogger.Log("CAMERA", $"Gameplay settings restored: Damping={originalDamping}, Lookahead={originalLookaheadEnabled}");
        }
        
        if (confiner != null)
        {
            confiner.Damping = originalConfinerDamping;
            GameLogger.Log("CAMERA", $"Confiner damping restored: {originalConfinerDamping}");
        }
    }

    private void OnPlayerSpawned(GameObject player)
    {
        if (player == null) return;
    
        playerTransform = player.transform;
    
        GameLogger.Log("CAMERA", $"Player spawned at {playerTransform.position}");
    
        if (cinemachineCamera != null)
        {
            Transform currentFollow = cinemachineCamera.Follow;
        
            // Si hay un Follow target previo (HomeCameraTarget), muévelo a la posición del Player
            if (currentFollow != null && currentFollow.gameObject.name.Contains("HomeCameraTarget"))
            {
                GameLogger.Log("CAMERA", $"Moving HomeCameraTarget from {currentFollow.position} to {playerTransform.position}");
                currentFollow.position = playerTransform.position;
            
                // Esperar un frame para que Cinemachine procese la nueva posición
                StartCoroutine(DelayedFollowChange());
            }
            else
            {
                // No hay target previo, cambiar directamente
                AssignPlayerAsTarget();
            }
        }
    
        isFollowingPlayer = true;
    }
    
    private IEnumerator DelayedFollowChange()
    {
        // Esperar 1 frame para que Cinemachine procese la nueva posición de HomeCameraTarget
        yield return null;
    
        // Ahora cambiar al Player
        AssignPlayerAsTarget();
    
        // Esperar antes de restaurar gameplay settings
        yield return new WaitForSecondsRealtime(delayBeforeRestoringSettings);
        RestoreGameplayCameraSettings();
    }

    private void AssignPlayerAsTarget()
    {
        cinemachineCamera.Follow = playerTransform;
        cinemachineCamera.Priority.Value = gameplayPriority;
    
        GameLogger.Log("CAMERA", $"Camera now following player - Priority={gameplayPriority}");
    }

    private IEnumerator RestoreSettingsAfterDelay()
    {
        GameLogger.Log("CAMERA", $"Waiting {delayBeforeRestoringSettings}s before restoring gameplay settings");
        
        yield return new WaitForSeconds(delayBeforeRestoringSettings);
        
        RestoreGameplayCameraSettings();
    }

    private void OnCameraBoundsUpdated()
    {
        if (LevelStarter.Instance == null) return;
        
        PolygonCollider2D bounds = LevelStarter.Instance.cameraBounds;
        
        if (bounds != null && confiner != null)
        {
            GameLogger.Log("CAMERA", "=== UPDATING CONFINER BOUNDS (confiner stays enabled) ===");
            
            confiner.BoundingShape2D = bounds;
            confiner.InvalidateBoundingShapeCache();
            
            GameLogger.Log("CAMERA", $"Bounds updated: {LevelStarter.Instance.GetLevelStartX():F2} to {LevelStarter.Instance.GetLevelEndX():F2}");
            GameLogger.Log("CAMERA", "Confiner damping: " + confiner.Damping);
        }
    }
}
