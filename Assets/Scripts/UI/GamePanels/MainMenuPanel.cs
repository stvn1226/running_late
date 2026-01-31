using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : BaseMainMenuPanel
{
    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button recentLevels;
    [SerializeField] private Button savedLevels;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    protected override void SetupPanel()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (recentLevels != null)
        {
            recentLevels.onClick.AddListener(OnRecentsClicked);
        }

        if (savedLevels != null)
        {
            savedLevels.onClick.AddListener(OnSavedClicked);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(OnCreditsClicked);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (recentLevels != null)
            recentLevels.onClick.RemoveListener(OnRecentsClicked);

        if (savedLevels != null)
            savedLevels.onClick.RemoveListener(OnSavedClicked);

        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(OnCreditsClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }
    
    private void OnPlayClicked()
    {
        GameLogger.Log("MENU", "Play clicked");
        
        if (menuController != null)
        {
            menuController.StartGame();
        }
    }
    
    private void OnRecentsClicked()
    {
        GameLogger.Log("MENU", "Recent levels clicked");

        if (menuController != null)
        {
            menuController.ShowRecentLevelsPanel();
        }
    }

    private void OnSavedClicked()
    {
        GameLogger.Log("MENU", "Saved levels clicked");

        if (menuController != null)
        {
            menuController.ShowSavedLevelsPanel();
        }
    }

    private void OnCreditsClicked()
    {
        GameLogger.Log("MENU", "Credits clicked");
        
        if (menuController != null)
        {
            menuController.ShowCreditsPanel();
        }
    }
    
    private void OnSettingsClicked()
    {
        GameLogger.Log("MENU", "Settings clicked");
        
        if (menuController != null)
        {
            menuController.ShowSettingsPanel();
        }
    }
    
    private void OnQuitClicked()
    {
        GameLogger.Log("MENU", "Quit clicked");
        
        if (menuController != null)
        {
            menuController.QuitGame();
        }
    }
}
