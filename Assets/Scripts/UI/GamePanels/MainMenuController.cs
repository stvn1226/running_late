using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private CreditsPanel creditsPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    
    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("UI References")]
    [SerializeField] private Canvas gameplayCanvas;
    
    private BaseMainMenuPanel currentPanel;
    
    private void Start()
    {
        InitializePanels();
        ShowMainPanel();
        
        if (gameplayCanvas != null)
        {
            gameplayCanvas.gameObject.SetActive(false);
        }
        
        Time.timeScale = 0f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.inputActions.UI.Enable();
            UIManager.Instance.inputActions.Gameplay.Disable();
        }
        
        GameLogger.Log("MENU", "Main Menu ready - UI controls active");
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
    }
    
    public void ShowMainPanel()
    {
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
        }
        
        currentPanel = targetPanel;
        currentPanel.Show();
        
        GameLogger.Log("MENU", $"Switched to panel: {targetPanel.GetType().Name}");
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
