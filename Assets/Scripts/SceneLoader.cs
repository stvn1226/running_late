using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private const string MAIN_MENU_SCENE = "MainMenu";
    private const string GAMEPLAY_SCENE = "ModularLevel";
    
    public static SceneLoader Instance { get; private set; }
    
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MAIN_MENU_SCENE)
            AudioManager.Instance?.ChangeMusic(AudioManager.SoundType.MusicMenu);
        else if (scene.name == GAMEPLAY_SCENE)
        {
            AudioManager.Instance?.ChangeMusic(AudioManager.SoundType.MusicLevel);
            // AudioManager.Instance?.AddMusic(AudioManager.SoundType.MusicInvincibility);
        }
    }

    public void LoadMainMenu()
    {
        GameLogger.Log("SCENE", "Loading Main Menu");
        AudioManager.Instance?.StopAll();
        Time.timeScale = 1f;
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }
    
    public void LoadGameplay()
    {
        GameLogger.Log("SCENE", "Loading Gameplay");
        Time.timeScale = 1f;
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }
    
    public void ReloadCurrentScene()
    {
        GameLogger.Log("SCENE", "Reloading current scene");
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
    
    public void QuitGame()
    {
        GameLogger.Log("SCENE", "Quitting game");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    public bool IsInMainMenu()
    {
        return SceneManager.GetActiveScene().name == MAIN_MENU_SCENE;
    }
    
    public bool IsInGameplay()
    {
        return SceneManager.GetActiveScene().name == GAMEPLAY_SCENE;
    }
}
