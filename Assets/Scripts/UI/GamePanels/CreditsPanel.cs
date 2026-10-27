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
        SetupBackButton();
    }

    private Button mobileBackButton;

    private void SetupBackButton()
    {
        // 1. Find and wire the existing BackBtn (InputPromptUI row used on desktop)
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.name.ToLower().Contains("back") && child.gameObject != gameObject)
            {
                // Hide the entire InputPromptUI row on mobile
                if (ControllerTypeDetector.IsMobilePlatform())
                {
                    var prompt = child.GetComponent<InputPromptUI>();
                    if (prompt != null)
                    {
                        prompt.HideAll();
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // Desktop: wire click on the existing row
                    var btn = child.gameObject.GetComponent<Button>() ?? child.gameObject.AddComponent<Button>();
                    btn.onClick.RemoveListener(OnBackClicked);
                    btn.onClick.AddListener(OnBackClicked);
                    Graphic[] graphics = btn.GetComponentsInChildren<Graphic>(true);
                    foreach (var g in graphics) g.raycastTarget = true;
                }
                break;
            }
        }

        // 2. Mobile: create / reuse a proper tappable GO BACK button
        if (ControllerTypeDetector.IsMobilePlatform())
        {
            if (mobileBackButton == null)
            {
                GameObject backObj = new GameObject("CreditsMobileBack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                backObj.transform.SetParent(transform, false);
                backObj.transform.SetAsLastSibling();

                RectTransform rect = backObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot     = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(50f, -50f);
                rect.sizeDelta = new Vector2(200f, 65f);

                Image img = backObj.GetComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.12f, 0.88f);
                img.raycastTarget = true;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                textObj.transform.SetParent(backObj.transform, false);
                RectTransform tr = textObj.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.sizeDelta = Vector2.zero;

                TMPro.TextMeshProUGUI tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "← GO BACK";
                tmp.fontSize = 28f;
                tmp.fontStyle = TMPro.FontStyles.Bold;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                // Inherit font from any existing TMP in the panel
                var refTmp = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (refTmp != null && refTmp.font != null) tmp.font = refTmp.font;

                mobileBackButton = backObj.GetComponent<Button>();
                mobileBackButton.targetGraphic = img;

                ColorBlock cb = mobileBackButton.colors;
                cb.normalColor    = Color.white;
                cb.highlightedColor = new Color(1f, 0.9f, 0.4f, 1f);
                cb.pressedColor   = new Color(0.8f, 0.7f, 0.2f, 1f);
                mobileBackButton.colors = cb;
            }

            mobileBackButton.onClick.RemoveListener(OnBackClicked);
            mobileBackButton.onClick.AddListener(OnBackClicked);
            mobileBackButton.gameObject.SetActive(true);
        }
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
        SetupBackButton();
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

        // Stick/WASD manual override with safe 0.25f deadzone to ignore controller stick drift
        if (Mathf.Abs(verticalInput) > 0.25f)
        {
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
        AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);

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
