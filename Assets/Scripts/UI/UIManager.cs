using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Navigation")]
    public Button firstSelectedButton;
    
    [Header("UI References")]
    public GameObject pausePanel;
    public Button retryButton;
    public Button newLevelButton;
    public Toggle seedToggle;
    public TMP_InputField seedInputField;
    public Toggle fps60Toggle;
    public Toggle fps120Toggle;
    
    [Header("Time Scale UI")]
    public TextMeshProUGUI timeScaleDisplay;
    public Button[] timeScaleButtons;
    
    [Header("Systems")]
    public LevelStarter levelStarter;
    public LevelManager levelManager;
    public TimeScaleManager timeScaleManager;
    
    public PlayerInputActions inputActions;
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
        SetupTimeScaleButtons();
    }
    
    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Enable();
            inputActions.Gameplay.Pause.performed += OnPausePressed;
            inputActions.UI.Pause.performed += OnPausePressed;
            inputActions.Gameplay.Reset.performed += OnResetPressed;
            
            // Time Scale Controls
            inputActions.Gameplay.TimeScaleUp.performed += OnTimeScaleUpPressed;
            inputActions.Gameplay.TimeScaleDown.performed += OnTimeScaleDownPressed;
            inputActions.Gameplay.TimeScaleToggle.performed += OnTimeScaleTogglePressed;
        }
    
        // Connect UI events
        retryButton.onClick.AddListener(RestartCurrentLevel);
        newLevelButton.onClick.AddListener(GenerateNewLevel);
        seedToggle.onValueChanged.AddListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.AddListener(OnSeedInputChanged);
        
        // Time Scale events
        if (timeScaleManager != null)
        {
            timeScaleManager.OnTimeScaleChanged += UpdateTimeScaleDisplay;
        }
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Pause.performed -= OnPausePressed;
            inputActions.UI.Pause.performed -= OnPausePressed;
            inputActions.Gameplay.Reset.performed -= OnResetPressed;
            
            // Time Scale Controls
            inputActions.Gameplay.TimeScaleUp.performed -= OnTimeScaleUpPressed;
            inputActions.Gameplay.TimeScaleDown.performed -= OnTimeScaleDownPressed;
            inputActions.Gameplay.TimeScaleToggle.performed -= OnTimeScaleTogglePressed;
            
            inputActions.Gameplay.Disable();
            inputActions.UI.Disable();
        }
    
        retryButton.onClick.RemoveListener(RestartCurrentLevel);
        newLevelButton.onClick.RemoveListener(GenerateNewLevel);
        seedToggle.onValueChanged.RemoveListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.RemoveListener(OnSeedInputChanged);
        
        // Time Scale events
        if (timeScaleManager != null)
        {
            timeScaleManager.OnTimeScaleChanged -= UpdateTimeScaleDisplay;
        }
    }
    
    private void Start()
    {
        pausePanel.SetActive(false);
        LoadSettings();
    
        var eventSystem = EventSystem.current;
        var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
    
        GameLogger.LogUI($"EventSystem actionsAsset: {uiModule.actionsAsset?.name}");
        GameLogger.LogUI($"Move action: {uiModule.move?.action?.name}");
        GameLogger.LogUI($"Submit action: {uiModule.submit?.action?.name}");
        GameLogger.LogUI($"Cancel action: {uiModule.cancel?.action?.name}");
    
        if (uiModule.move?.action != null)
        {
            GameLogger.LogUI($"Move action enabled: {uiModule.move.action.enabled}");
            GameLogger.LogUI($"Move action bindings: {uiModule.move.action.bindings.Count}");
        }
        
        UpdateTimeScaleDisplay(1f);
    }

    // TIME SCALE SYSTEM
    private void OnTimeScaleUpPressed(InputAction.CallbackContext context)
    {
        if (!isPaused && timeScaleManager != null)
        {
            timeScaleManager.CycleTimeScaleUp();
        }
    }
    
    private void OnTimeScaleDownPressed(InputAction.CallbackContext context)
    {
        if (!isPaused && timeScaleManager != null)
        {
            timeScaleManager.CycleTimeScaleDown();
        }
    }
    
    private void OnTimeScaleTogglePressed(InputAction.CallbackContext context)
    {
        if (!isPaused && timeScaleManager != null)
        {
            timeScaleManager.ToggleResetPause();
        }
    }
    
    private void SetupTimeScaleButtons()
    {
        if (timeScaleButtons == null || timeScaleManager == null) return;
        
        float[] scales = timeScaleManager.GetAllTimeScales();
        
        for (int i = 0; i < timeScaleButtons.Length && i < scales.Length; i++)
        {
            int index = i;
            float scale = scales[i];
            
            if (timeScaleButtons[i] != null)
            {
                timeScaleButtons[i].onClick.AddListener(() => timeScaleManager.SetTimeScaleByIndex(index));
                
                // Update button text
                TextMeshProUGUI buttonText = timeScaleButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = scale == 0f ? "PAUSE" : $"{scale}x";
                }
            }
        }
    }
    
    private void UpdateTimeScaleDisplay(float currentScale)
    {
        if (timeScaleDisplay != null)
        {
            string displayText = currentScale == 0f ? "PAUSED" : $"{currentScale}x";
            timeScaleDisplay.text = $"Speed: {displayText}";
        }
        
        // Update button visual states
        if (timeScaleButtons != null && timeScaleManager != null)
        {
            int currentIndex = timeScaleManager.GetCurrentIndex();
            
            for (int i = 0; i < timeScaleButtons.Length; i++)
            {
                if (timeScaleButtons[i] != null)
                {
                    ColorBlock colors = timeScaleButtons[i].colors;
                    colors.normalColor = (i == currentIndex) ? Color.green : Color.white;
                    timeScaleButtons[i].colors = colors;
                }
            }
        }
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

        // Notify TimeScaleManager about pause state
        if (timeScaleManager != null)
        {
            timeScaleManager.OnGamePaused(isPaused);
        }
        else
        {
            // Fallback if no TimeScaleManager
            Time.timeScale = isPaused ? 0f : 1f;
        }

        if (isPaused)
        {
            inputActions.Gameplay.Disable();
            inputActions.UI.Enable();
        
            if (firstSelectedButton != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
            }
        }
        else
        {
            inputActions.UI.Disable();
            inputActions.Gameplay.Enable();
        
            EventSystem.current.SetSelectedGameObject(null);
        }
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
                GameLogger.LogWarning("UI", "Invalid seed format!");
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
        GameLogger.LogUI($"Frame rate set to {targetFPS} FPS");
    }
    
    private void LoadSettings()
    {
        int savedFrameRate = PlayerPrefs.GetInt("TargetFrameRate", 60);
        SetTargetFrameRate(savedFrameRate);
    }
    
    public bool IsPaused => isPaused;
    
    private void Update()
    {
        if (isPaused)
        {
            var uiModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        
            if (uiModule.move?.action != null)
            {
                Vector2 moveValue = uiModule.move.action.ReadValue<Vector2>();
                if (moveValue.magnitude > 0.1f)
                {
                    GameLogger.LogVerbose("UI", $"EventSystem move action receiving: {moveValue}");
                }
            }
        
            if (uiModule.submit?.action != null)
            {
                bool submitPressed = uiModule.submit.action.WasPressedThisFrame();
                bool submitIsPressed = uiModule.submit.action.IsPressed();
            
                GameLogger.LogVerbose("UI", $"Submit pressed this frame: {submitPressed}, Submit is pressed: {submitIsPressed}");
            
                if (submitPressed)
                {
                    GameLogger.LogUI("EventSystem submit action pressed!");
                }
            }
        }
    }
    
    private void OnResetPressed(InputAction.CallbackContext context)
    {
        if (!isPaused)
        {
            RestartCurrentLevel();
        }
    }
}
