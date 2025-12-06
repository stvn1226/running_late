using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : BaseMainMenuPanel
{
    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    protected override void SetupPanel()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
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
