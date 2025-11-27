using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;
    public Button playButton;
    public Button quitButton;
    
    [Header("Fade Settings")]
    public CanvasGroup menuCanvasGroup;
    public float fadeOutDuration = 0.5f;

    [Header("UI References")]
    public Canvas gameplayCanvas;
    
    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
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

    private void OnPlayClicked()
    {
        GameLogger.Log("MENU", "Play clicked - Starting game");
        StartCoroutine(StartGame());
    }

    private void OnQuitClicked()
    {
        GameLogger.Log("MENU", "Quit clicked");
        Application.Quit();
    }

    private IEnumerator StartGame()
    {
        if (menuCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                menuCanvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
                yield return null;
            }
            menuCanvasGroup.alpha = 0f;
        }
        gameplayCanvas.gameObject.SetActive(true);
        
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.inputActions.UI.Disable();
            UIManager.Instance.inputActions.Gameplay.Enable();
        }
        
        GameLogger.Log("MENU", "Game started - Gameplay controls active");
    }
}
