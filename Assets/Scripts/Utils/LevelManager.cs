using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private bool isReplaying = false;
    
    public static LevelManager Instance { get; private set; }
    
    // Events for other systems to respond to level reset
    public System.Action OnLevelReplayStarted;
    public System.Action OnLevelReplayCompleted;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize input actions
        try
        {
            inputActions = new PlayerInputActions();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize PlayerInputActions: {e.Message}");
        }
    }
    
    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Enable();
            inputActions.Gameplay.Reset.performed += OnReplayPerformed;
        }
        
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Reset.performed -= OnReplayPerformed;
            inputActions.Gameplay.Disable();
        }
        
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnReplayPerformed(InputAction.CallbackContext context)
    {
        if (!isReplaying)
        {
            ReplayLevel();
        }
    }
    
    public void ReplayLevel(bool shouldReload = true)
    {
        if (isReplaying) return;
            
        Debug.Log("Restarting level completely...");
        isReplaying = true;
        Time.timeScale = 1f; // Ensure time scale is normal

        // Notify other systems that reset is starting
        OnLevelReplayStarted?.Invoke();
        
        // Immediately reload the scene
        if (shouldReload) PerformSceneReload();
    }
    
    private void PerformSceneReload()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Reloading scene completely: {currentSceneName}");
        
        // Reload the current scene (this resets everything to initial state)
        SceneManager.LoadScene(currentSceneName);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isReplaying)
        {
            Debug.Log($"Level completely reset: {scene.name}");
            isReplaying = false;
            
            // Notify other systems that reset is complete
            OnLevelReplayCompleted?.Invoke();
        }
    }
        
    // Check if currently resetting (useful for other systems)
    public bool IsReplaying()
    {
        return isReplaying;
    }
    
    // Get current scene name
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
    
    private void OnDestroy()
    {
        // Cancel any pending invokes (though we don't use any)
        CancelInvoke();
    }
}