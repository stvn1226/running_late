using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private CreditsPanel creditsPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private SavedLevelsPanel recentLevelsPanel;
    [SerializeField] private SavedLevelsPanel savedLevelsPanel;
    [SerializeField] private TextInputPanel textInputPanel;

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("UI References")]
    [SerializeField] private Canvas gameplayCanvas;
    
    private BaseMainMenuPanel currentPanel;
    private BaseMainMenuPanel previousPanel;
    
    private PlayerInputActions inputActions;
    
    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }
    
    private void Start()
    {
        InitializePanels();
        ShowMainPanel();
        
        if (gameplayCanvas != null)
        {
            gameplayCanvas.gameObject.SetActive(false);
        }
        
        Time.timeScale = 1f;
        
        GameLogger.Log("MENU", "Main Menu ready - UI controls active");
    }
    
    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.UI.Enable();
            inputActions.UI.Cancel.performed += OnCancelPressed;
            GameLogger.Log("MENU", "Cancel button subscribed in Main Menu");
        }
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.UI.Cancel.performed -= OnCancelPressed;
            inputActions.UI.Disable();
        }
    }
    
    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
        }
    }
    
    private void InitializePanels()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Initialize(this);
        }
        
        if (creditsPanel != null)
        {
            creditsPanel.Initialize(this);
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.Initialize(this);
        }

        if (recentLevelsPanel != null)
        {
            recentLevelsPanel.Initialize(this);
        }

        if (savedLevelsPanel != null)
        {
            savedLevelsPanel.Initialize(this);
        }

        if (textInputPanel != null)
        {
            textInputPanel.Initialize(this);
        }
    }

    public void ShowMainPanel()
    {
        SwitchToPanel(mainMenuPanel);
    }

    public void ShowMainPanelStep2()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetForceStep2OnNextShow(true);
        }
        SwitchToPanel(mainMenuPanel);
    }
    
    public void ShowCreditsPanel()
    {
        SwitchToPanel(creditsPanel);
    }
    
    public void ShowSettingsPanel()
    {
        SwitchToPanel(settingsPanel);
    }

    public void ShowRecentLevelsPanel()
    {
        recentLevelsPanel.PopulateMenu();
        SwitchToPanel(recentLevelsPanel);
    }

    public void ShowSavedLevelsPanel()
    {
        savedLevelsPanel.PopulateMenu();
        SwitchToPanel(savedLevelsPanel);
    }

    public void ShowTextInputPanel()
    {
        SwitchToPanel(textInputPanel);
    }

    private void SwitchToPanel(BaseMainMenuPanel targetPanel)
    {
        if (targetPanel == null)
        {
            GameLogger.LogWarning("MENU", "Trying to switch to null panel");
            return;
        }
        
        if (currentPanel != null)
        {
            currentPanel.Hide();
            
            if (targetPanel != mainMenuPanel)
            {
                previousPanel = currentPanel;
            }
        }
        
        currentPanel = targetPanel;
        currentPanel.Show();
        
        GameLogger.Log("MENU", $"Switched to panel: {targetPanel.GetType().Name}");
    }
    
    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        if (currentPanel == null) return;
        
        if (textInputPanel != null && textInputPanel.IsVisible)
        {
            if (IsInputFieldActive())
            {
                DeactivateInputField();
                GameLogger.Log("MENU", "Cancel pressed on TextInputPanel InputField - Deactivating input");
                return;
            }

            GameLogger.Log("MENU", "Cancel pressed on TextInputPanel, hiding it");
            textInputPanel.Hide();
            textInputPanel.RestorePreviousSelection();
            AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
            GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
            return;
        }

        if (currentPanel == mainMenuPanel)
        {
            if (mainMenuPanel.IsInStep2())
            {
                if (IsInputFieldActive())
                {
                    DeactivateInputField();
                    GameLogger.Log("MENU", "Cancel pressed on Step 2 InputField - Deactivating input");
                    return;
                }
                
                GameLogger.Log("MENU", "Cancel pressed on Step 2 - Returning to Step 1");
                mainMenuPanel.ShowStep1();
                AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
                GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
                return;
            }
            
            GameLogger.Log("MENU", "Cancel pressed on Main Menu Step 1 - ignoring");
            return;
        }
        
        if (currentPanel != mainMenuPanel)
        {
            GameLogger.Log("MENU", $"Cancel pressed on {currentPanel.GetType().Name}, returning to Main Menu");
            
            if (currentPanel == recentLevelsPanel || currentPanel == savedLevelsPanel)
            {
                ShowMainPanelStep2();
            }
            else
            {
                ShowMainPanel();
            }
            
            AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
            GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
            return;
        }
    }
    
    private bool IsInputFieldActive()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            return false;
        
        TMPro.TMP_InputField inputField = EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();
        return inputField != null && inputField.isFocused;
    }
    
    private void DeactivateInputField()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            return;
        
        TMPro.TMP_InputField inputField = EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();
        if (inputField != null)
        {
            inputField.DeactivateInputField();
            // Ensure the object remains selected but NOT focused
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            
            // Trigger feedback for deactivation
            GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
        }
    }
    
    public void StartGame()
    {
        StartCoroutine(LoadGameplayAndGenerate());
    }

    public void QuitGame()
    {
        GameLogger.Log("MENU", "Quit clicked");
    
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
    
    public void SetDualPathMode(bool isDualPath)
    {
        // 0 = DualPath, 1 = SingleSidewalk
        int mode = isDualPath ? 0 : 1;
        PlayerPrefs.SetInt("PathMode", mode);
        PlayerPrefs.Save();
        GameLogger.Log("MENU", $"PathMode set to {(isDualPath ? "DualPath" : "SingleSidewalk")}");
    }
    
    private IEnumerator LoadGameplayAndGenerate()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadGameplay();
        }
        else
        {
            SceneManager.LoadScene("ModularLevel");
        }
    
        yield return new WaitForEndOfFrame();
    
        if (LevelStarter.Instance != null)
        {
            GameLogger.Log("MENU", "Forcing level generation after scene load");
            LevelStarter.Instance.ForceRegenerate();
        }
    }
}
