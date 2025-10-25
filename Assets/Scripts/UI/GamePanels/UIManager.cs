using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private PauseMenuPanel pauseMenuPanel;
    [SerializeField] private VictoryPanel victoryPanel;
    [SerializeField] private DefeatPanel defeatPanel;
    
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
            inputActions.Gameplay.Enable();
            inputActions.Gameplay.Pause.performed += OnPausePressed;
            inputActions.UI.Pause.performed += OnPausePressed;
        }
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Pause.performed -= OnPausePressed;
            inputActions.UI.Pause.performed -= OnPausePressed;
            
            inputActions.Gameplay.Disable();
            inputActions.UI.Disable();
        }
    }
    
    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (isEndGameActive)
        {
            return;
        }
        
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
        }
        else
        {
            GameLogger.LogWarning("UI", $"Panel of type {typeof(T).Name} not found!");
        }
    }
    
    public void HideAllPanels()
    {
        foreach (var panel in menuPanels.Values)
        {
            panel.Hide();
        }
        
        if (victoryPanel != null)
            victoryPanel.Hide();
        
        if (defeatPanel != null)
            defeatPanel.Hide();
        
        currentActivePanel = null;
        isPaused = false;
        isEndGameActive = false;
        SetInputContext(false);
        
        Time.timeScale = 1f;
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
    
    public bool IsPaused => isPaused;
    public bool IsEndGameActive => isEndGameActive;
    public BaseMenuPanel CurrentActivePanel => currentActivePanel;
}
