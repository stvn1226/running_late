using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class CreditsPanel : BaseMainMenuPanel
{
    [Header("Credits UI")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI creditsText;
    
    [Header("Logo Display")]
    [SerializeField] private Image studioLogoImage;
    [SerializeField] private Image gameLogoImage;
    
    [Header("Credits Data")]
    [SerializeField] private CreditsConfiguration creditsConfig;
    
    [Header("Animation Settings")]
    [SerializeField] private bool autoScroll = true;
    [SerializeField] private float scrollSpeed = 0.1f;
    [SerializeField] private float scrollDelay = 1f;

    [Header("Manual Scroll Settings")]
    [SerializeField] private float manualScrollSpeed = 0.4f;
    [SerializeField] private float idleResumeDelay = 1.5f;
    
    private PlayerInputActions inputActions;
    private float idleTimer = 0f;
    private float bottomTimer = 0f;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
        }
    }
    
    protected override void SetupPanel()
    {
        // Panel de créditos sin botones interactivos; el retorno se realiza con Cancel (B / Esc).
    }

    protected override void SetDefaultSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
    
    protected override void OnShow()
    {
        inputActions?.UI.Enable();
        idleTimer = scrollDelay;
        bottomTimer = 0f;

        LoadLogos();
        
        if (creditsText != null)
        {
            if (creditsConfig != null)
            {
                creditsText.text = creditsConfig.GenerateCreditsText();
                GameLogger.Log("CREDITS", $"Loaded credits from configuration: {creditsConfig.name}");
            }
            else
            {
                creditsText.text = GetFallbackCredits();
                GameLogger.LogWarning("CREDITS", "No credits configuration assigned! Using fallback.");
            }
        }
        
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        
        GameLogger.Log("CREDITS", "Credits panel shown");
    }
    
    protected override void OnHide()
    {
        inputActions?.UI.Disable();
        GameLogger.Log("CREDITS", "Credits panel hidden");
    }

    private void Update()
    {
        if (!isVisible || scrollRect == null) return;

        float verticalInput = 0f;
        if (inputActions != null)
        {
            Vector2 nav = inputActions.UI.Navigate.ReadValue<Vector2>();
            verticalInput = nav.y;
        }

        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            // Desplazamiento manual con Stick (izq/der), D-Pad o W / S / Flechas
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                scrollRect.verticalNormalizedPosition + (verticalInput * manualScrollSpeed * Time.unscaledDeltaTime)
            );
            idleTimer = idleResumeDelay;
            bottomTimer = 0f;
        }
        else
        {
            if (idleTimer > 0f)
            {
                idleTimer -= Time.unscaledDeltaTime;
            }
            else if (autoScroll)
            {
                if (scrollRect.verticalNormalizedPosition > 0.001f)
                {
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                        scrollRect.verticalNormalizedPosition - (scrollSpeed * Time.unscaledDeltaTime)
                    );
                }
                else
                {
                    bottomTimer += Time.unscaledDeltaTime;
                    if (bottomTimer >= 2f)
                    {
                        scrollRect.verticalNormalizedPosition = 1f;
                        idleTimer = scrollDelay;
                        bottomTimer = 0f;
                    }
                }
            }
        }
    }
    
    private void LoadLogos()
    {
        if (creditsConfig == null) return;
        
        if (studioLogoImage != null)
        {
            if (creditsConfig.studioLogo != null)
            {
                studioLogoImage.sprite = creditsConfig.studioLogo;
                studioLogoImage.gameObject.SetActive(true);
                studioLogoImage.preserveAspect = true;
            }
            else
            {
                studioLogoImage.gameObject.SetActive(false);
            }
        }
        
        if (gameLogoImage != null)
        {
            if (creditsConfig.gameLogo != null)
            {
                gameLogoImage.sprite = creditsConfig.gameLogo;
                gameLogoImage.gameObject.SetActive(true);
                gameLogoImage.preserveAspect = true;
            }
            else
            {
                gameLogoImage.gameObject.SetActive(false);
            }
        }
    }
    
    public void OnBackClicked()
    {
        if (menuController != null)
        {
            menuController.ShowMainPanel();
        }
    }

    
    private string GetFallbackCredits()
    {
        return @"<align=center><size=70><b>UNBREAKABLE INTERACTIVE</b></size>
        <size=50>presents</size>

        <size=60><b>RUNNING LATE</b></size>


        <size=40><b>CREDITS</b></size>

        Please assign a CreditsConfiguration asset
        to display proper credits.


        <size=50><b>THANK YOU FOR PLAYING!</b></size>

        © 2026 Unbreakable Interactive
        All Rights Reserved
        </align>";
    }
}
