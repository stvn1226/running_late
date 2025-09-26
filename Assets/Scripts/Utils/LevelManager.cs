using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private bool isReplaying = false;
    
    public static LevelManager Instance { get; private set; }
    
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
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Reset.performed -= OnReplayPerformed;
            inputActions.Gameplay.Disable();
        }
        
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
            
        isReplaying = true;
        Time.timeScale = 1f;

        OnLevelReplayStarted?.Invoke();
        
        if (shouldReload) PerformSceneReload();
    }
    
    private void PerformSceneReload()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isReplaying)
        {
            isReplaying = false;
            OnLevelReplayCompleted?.Invoke();
        }
    }
        
    public bool IsReplaying()
    {
        return isReplaying;
    }
    
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
    }
}
