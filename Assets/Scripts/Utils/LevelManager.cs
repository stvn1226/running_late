using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    private bool isReplaying = false;
    
    public static LevelManager Instance { get; private set; }
    
    public System.Action OnLevelReplayStarted;
    public System.Action OnLevelReplayCompleted;
    
    public int LastSeed { get; set; }
    
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
    
    public void ReplayLevel()
    {
        if (isReplaying) return;
        
        isReplaying = true;
        Time.timeScale = 1f;

        AudioManager.Instance?.StopAll();
        OnLevelReplayStarted?.Invoke();
        
        GameLogger.Log("LEVEL", "Reloading scene");
        
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.ReloadCurrentScene();
        }
        else
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }
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
}