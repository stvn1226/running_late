using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public Button retryButton;
    public Button newLevelButton;
    public Toggle seedToggle;
    public TMP_InputField seedInputField;
    public Toggle fps60Toggle;
    public Toggle fps120Toggle;
    
    [Header("Systems")]
    public LevelStarter levelStarter;
    public LevelManager levelManager;
    
    private PlayerInputActions inputActions;
    private bool isPaused = false;
    private bool isManualSeedMode = false;
    
    public static UIManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        inputActions = new PlayerInputActions();
        SetupFrameRateToggles();
    }
    
    private void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Pause.performed += OnPausePressed;
        
        // Connect UI events
        retryButton.onClick.AddListener(RestartCurrentLevel);
        newLevelButton.onClick.AddListener(GenerateNewLevel);
        seedToggle.onValueChanged.AddListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.AddListener(OnSeedInputChanged);
    }
    
    private void OnDisable()
    {
        inputActions.Gameplay.Pause.performed -= OnPausePressed;
        inputActions.Gameplay.Disable();
        
        retryButton.onClick.RemoveListener(RestartCurrentLevel);
        newLevelButton.onClick.RemoveListener(GenerateNewLevel);
        seedToggle.onValueChanged.RemoveListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.RemoveListener(OnSeedInputChanged);
    }
    
    private void Start()
    {
        pausePanel.SetActive(false);
        LoadSettings();
    }
    
    // PAUSE SYSTEM
    private void OnPausePressed(InputAction.CallbackContext context)
    {
        TogglePause();
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }
    
    // LEVEL CONTROL
    public void RestartCurrentLevel()
    {
        if (isPaused) TogglePause();
    
        int currentSeed = levelStarter.GetCurrentSeed();
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
    
        levelManager.ReplayLevel();
    }

    
    public void GenerateNewLevel()
    {
        if (isPaused) TogglePause();
    
        if (isManualSeedMode && !string.IsNullOrEmpty(seedInputField.text))
        {
            if (int.TryParse(seedInputField.text, out int seed))
            {
                PlayerPrefs.SetInt("NewLevelSeed", seed);
                PlayerPrefs.SetInt("UseNewLevelSeed", 1);
                levelManager.ReplayLevel(); 
            }
            else
            {
                Debug.LogWarning("Invalid seed format!");
                PlayerPrefs.SetInt("UseNewLevelSeed", 0);
                levelManager.ReplayLevel(); 
            }
        }
        else
        {
            PlayerPrefs.SetInt("UseNewLevelSeed", 0);
            levelManager.ReplayLevel(); 
        }
    }
    
    // SEED SYSTEM
    private void OnSeedToggleChanged(bool useManualSeed)
    {
        isManualSeedMode = useManualSeed;
        seedInputField.interactable = useManualSeed;
        
        if (useManualSeed)
        {
            seedInputField.text = levelStarter.GetCurrentSeed().ToString();
        }
    }
    
    private void OnSeedInputChanged(string seedText)
    {
        // Validate seed input in real-time
        if (int.TryParse(seedText, out int _))
        {
            seedInputField.image.color = Color.white;
        }
        else
        {
            seedInputField.image.color = Color.red;
        }
    }
    
    // FRAMERATE SYSTEM
    private void SetupFrameRateToggles()
    {
        fps60Toggle.onValueChanged.AddListener((isOn) => {
            if (isOn) SetTargetFrameRate(60);
        });
        
        fps120Toggle.onValueChanged.AddListener((isOn) => {
            if (isOn) SetTargetFrameRate(120);
        });
    }
    
    private void SetTargetFrameRate(int targetFPS)
    {
        Application.targetFrameRate = targetFPS;
        
        fps60Toggle.SetIsOnWithoutNotify(targetFPS == 60);
        fps120Toggle.SetIsOnWithoutNotify(targetFPS == 120);
        
        PlayerPrefs.SetInt("TargetFrameRate", targetFPS);
        Debug.Log($"Frame rate set to {targetFPS} FPS");
    }
    
    private void LoadSettings()
    {
        int savedFrameRate = PlayerPrefs.GetInt("TargetFrameRate", 60);
        SetTargetFrameRate(savedFrameRate);
    }
    
    public bool IsPaused => isPaused;
}
