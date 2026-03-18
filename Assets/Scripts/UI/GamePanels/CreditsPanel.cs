using UnityEngine;
using UnityEngine.UI;
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
    
    private Coroutine scrollCoroutine;
    
    protected override void SetupPanel()
    {
        if (defaultSelectedButton != null)
        {
            defaultSelectedButton.onClick.AddListener(OnBackClicked);
        }
    }
    
    private void OnDestroy()
    {
        if (defaultSelectedButton != null)
        {
            defaultSelectedButton.onClick.RemoveListener(OnBackClicked);
        }
    }
    
    protected override void OnShow()
    {
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
        
        if (autoScroll)
        {
            scrollCoroutine = StartCoroutine(AutoScrollCredits());
        }
        
        GameLogger.Log("CREDITS", "Credits panel shown");
    }
    
    protected override void OnHide()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }
        
        GameLogger.Log("CREDITS", "Credits panel hidden");
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
    
    private void OnBackClicked()
    {
        if (menuController != null)
        {
            menuController.ShowMainPanel();
        }
    }
    
    private IEnumerator AutoScrollCredits()
    {
        yield return new WaitForSecondsRealtime(scrollDelay);
        
        while (isVisible && scrollRect != null)
        {
            if (scrollRect.verticalNormalizedPosition > 0f)
            {
                scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.unscaledDeltaTime;
                yield return null;
            }
            else
            {
                yield return new WaitForSecondsRealtime(2f);
                scrollRect.verticalNormalizedPosition = 1f;
                yield return new WaitForSecondsRealtime(scrollDelay);
            }
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
