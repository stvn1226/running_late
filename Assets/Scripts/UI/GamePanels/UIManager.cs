using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private PauseMenuPanel pauseMenuPanel;
    [SerializeField] private VictoryPanel victoryPanel;
    [SerializeField] private DefeatPanel defeatPanel;
    
    [Header("Other Panels")]
    [SerializeField] private SettingsPanel settingsPanel;
    
    public PlayerInputActions inputActions;
    private bool isPaused = false;
    private bool isEndGameActive = false;
    private Dictionary<System.Type, BaseMenuPanel> menuPanels;
    private BaseMenuPanel currentActivePanel;
    
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
        InitializePanels();
    }
    
    private void InitializePanels()
    {
        menuPanels = new Dictionary<System.Type, BaseMenuPanel>();
        
        if (pauseMenuPanel != null) 
            menuPanels.Add(typeof(PauseMenuPanel), pauseMenuPanel);
        
        HideAllPanels();
    }
    
    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Pause.performed += OnPausePressed;
            inputActions.UI.Pause.performed += OnPausePressed;
            inputActions.UI.Cancel.performed += OnCancelPressed;
        }
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Pause.performed -= OnPausePressed;
            inputActions.UI.Pause.performed -= OnPausePressed;
            inputActions.UI.Cancel.performed -= OnCancelPressed;
            
            inputActions.Gameplay.Disable();
            inputActions.UI.Disable();
        }
    }
    
    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        // Use a unified logic to avoid double-processing when ESC is bound to both Pause and Cancel
        HandleBackOrEscape();
    }
    
    public void OnPausePressed(InputAction.CallbackContext context)
    {
        HandleBackOrEscape();
    }

    /// <summary>
    /// Unified handler for Back/Escape/Pause actions
    /// </summary>
    public void HandleBackOrEscape()
    {
        if (isEndGameActive) return;

        // 1. If Settings is open, go back to Pause menu
        if (settingsPanel != null && settingsPanel.IsVisible)
        {
            settingsPanel.Hide();
            if (pauseMenuPanel != null)
            {
                ShowPanel<PauseMenuPanel>();
            }
            AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
            GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
            return;
        }
    
        // 2. If Pause menu is open, close all and resume
        if (currentActivePanel == pauseMenuPanel)
        {
            HideAllPanels();
            AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
        }
        // 3. If nothing is open, open Pause menu
        else if (currentActivePanel == null)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isEndGameActive) return;
        
        if (currentActivePanel == pauseMenuPanel)
        {
            HideAllPanels();
        }
        else if (currentActivePanel == null)
        {
            ShowPanel<PauseMenuPanel>();
        }
    }
    
    public void ShowPanel<T>() where T : BaseMenuPanel
    {
        if (menuPanels.TryGetValue(typeof(T), out BaseMenuPanel panel))
        {
            HideAllPanels();
            panel.Show();
            currentActivePanel = panel;
            
            isPaused = true;
            isEndGameActive = false;
            SetInputContext(true);
            
            Time.timeScale = 0f;
            
            if (panel is PauseMenuPanel)
            {
                AudioManager.Instance?.Play(AudioManager.SoundType.PauseOpen);
            }
            
            if (panel is PauseMenuPanel && AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.UI_PAUSE, new Dictionary<string, object>
                {
                    { "panel_name", typeof(T).Name }
                });
            }
        }
        else
        {
            GameLogger.LogWarning("UI", $"Panel of type {typeof(T).Name} not found!");
        }
    }
    
    public void HideAllPanels()
    {
        bool wasPaused = isPaused && !isEndGameActive;
        
        // Hide all registered panels
        foreach (var panel in menuPanels.Values)
        {
            if (panel != null) panel.Hide();
        }
        
        // Hide explicitly referenced panels
        if (victoryPanel != null) victoryPanel.Hide();
        if (defeatPanel != null) defeatPanel.Hide();
        if (settingsPanel != null) settingsPanel.Hide();
        
        currentActivePanel = null;
        isPaused = false;
        isEndGameActive = false;
        SetInputContext(false);
        
        Time.timeScale = 1f;
        
        if (wasPaused && AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.UI_RESUME, null);
        }
    }
    
    private void SetInputContext(bool uiMode)
    {
        if (uiMode)
        {
            inputActions.Gameplay.Disable();
            inputActions.UI.Enable();
        }
        else
        {
            inputActions.UI.Disable();
            inputActions.Gameplay.Enable();
        }
    }
    
    public void ShowVictoryPanel()
    {
        if (victoryPanel != null)
        {
            HideAllPanels();
            victoryPanel.Show();
            currentActivePanel = victoryPanel;
            
            isPaused = true;
            isEndGameActive = true;
            SetInputContext(true);
            Time.timeScale = 0f;
            
            AudioManager.Instance?.StopMusic();
            AudioManager.Instance?.Play(AudioManager.SoundType.MusicVictory);
            
            GameLogger.Log("UI", "Victory panel shown");
        }
        else
        {
            GameLogger.LogError("UI", "VictoryPanel not assigned!");
        }
    }
    
    public void ShowLosePanel()
    {
        if (defeatPanel != null)
        {
            HideAllPanels();
            defeatPanel.Show();
            currentActivePanel = defeatPanel;
            
            isPaused = true;
            isEndGameActive = true;
            SetInputContext(true);
            Time.timeScale = 0f;
            
            AudioManager.Instance?.StopMusic();
            AudioManager.Instance?.Play(AudioManager.SoundType.MusicDefeat);
            
            GameLogger.Log("UI", "Defeat panel shown");
        }
        else
        {
            GameLogger.LogError("UI", "DefeatPanel not assigned!");
        }
    }
    
    public void ShowEndGamePanel()
    {
        ShowVictoryPanel();
    }
    
    public void NotifyPanelClosed()
    {
        currentActivePanel = null;
        isPaused = false;
        SetInputContext(false);
        Time.timeScale = 1f;
    
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.UI_RESUME, null);
        }
    }
    
    public bool IsPaused => isPaused;
    public bool IsEndGameActive => isEndGameActive;
    public BaseMenuPanel CurrentActivePanel => currentActivePanel;
}
