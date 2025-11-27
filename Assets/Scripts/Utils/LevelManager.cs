using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
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
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
