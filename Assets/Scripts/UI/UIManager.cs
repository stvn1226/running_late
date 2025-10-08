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
    
    [Header("Systems")]
    public LevelStarter levelStarter;
    public LevelManager levelManager;
    
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
    }
    
    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Enable();
            inputActions.Gameplay.Pause.performed += OnPausePressed;
            inputActions.UI.Pause.performed += OnPausePressed;
            inputActions.Gameplay.Reset.performed += OnResetPressed; 
        }
    
        // Connect UI events
        retryButton.onClick.AddListener(RestartCurrentLevel);
        newLevelButton.onClick.AddListener(GenerateNewLevel);
        seedToggle.onValueChanged.AddListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.AddListener(OnSeedInputChanged);
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Pause.performed -= OnPausePressed;
            inputActions.UI.Pause.performed -= OnPausePressed;
            inputActions.Gameplay.Reset.performed -= OnResetPressed;
            inputActions.Gameplay.Disable();
            inputActions.UI.Disable();
        }
    
        retryButton.onClick.RemoveListener(RestartCurrentLevel);
        newLevelButton.onClick.RemoveListener(GenerateNewLevel);
        seedToggle.onValueChanged.RemoveListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.RemoveListener(OnSeedInputChanged);
    }
    
    private void Start()
    {
        pausePanel.SetActive(false);
        LoadSettings();
    
        var eventSystem = EventSystem.current;
        var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
    
        Debug.Log($"EventSystem actionsAsset: {uiModule.actionsAsset?.name}");
        Debug.Log($"Move action: {uiModule.move?.action?.name}");
        Debug.Log($"Submit action: {uiModule.submit?.action?.name}");
        Debug.Log($"Cancel action: {uiModule.cancel?.action?.name}");
    
        if (uiModule.move?.action != null)
        {
            Debug.Log($"Move action enabled: {uiModule.move.action.enabled}");
            Debug.Log($"Move action bindings: {uiModule.move.action.bindings.Count}");
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
        Time.timeScale = isPaused ? 0f : 1f;

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
                    Debug.Log($"EventSystem move action receiving: {moveValue}");
                }
            }
        
            if (uiModule.submit?.action != null)
            {
                bool submitPressed = uiModule.submit.action.WasPressedThisFrame();
                bool submitIsPressed = uiModule.submit.action.IsPressed();
            
                Debug.Log($"Submit pressed this frame: {submitPressed}, Submit is pressed: {submitIsPressed}");
            
                if (submitPressed)
                {
                    Debug.Log("EventSystem submit action pressed!");
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
