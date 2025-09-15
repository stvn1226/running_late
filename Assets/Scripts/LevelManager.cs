using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private bool isResetting = false;
    
    public static LevelManager Instance { get; private set; }
    
    // Events for other systems to respond to level reset
    public System.Action OnLevelResetStarted;
    public System.Action OnLevelResetCompleted;
    
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
            inputActions.Gameplay.Reset.performed += OnResetPerformed;
        }
        
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Reset.performed -= OnResetPerformed;
            inputActions.Gameplay.Disable();
        }
        
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnResetPerformed(InputAction.CallbackContext context)
    {
        if (!isResetting)
        {
            ResetLevel();
        }
    }
    
    public void ResetLevel()
    {
        if (isResetting)
            return;
            
        Debug.Log("Resetting level completely...");
        isResetting = true;
        
        // Notify other systems that reset is starting
        OnLevelResetStarted?.Invoke();
        
        // Immediately reload the scene
        PerformSceneReload();
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
        if (isResetting)
        {
            Debug.Log($"Level completely reset: {scene.name}");
            isResetting = false;
            
            // Notify other systems that reset is complete
            OnLevelResetCompleted?.Invoke();
        }
    }
    
    // Public method for other scripts to trigger level reset
    public void ResetLevelFromScript()
    {
        ResetLevel();
    }
    
    // Check if currently resetting (useful for other systems)
    public bool IsResetting()
    {
        return isResetting;
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