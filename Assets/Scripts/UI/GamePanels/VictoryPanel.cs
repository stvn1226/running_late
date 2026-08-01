using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils;

public class VictoryPanel : BaseMenuPanel
{
    // ─── Existing Victory UI (unchanged) ────────────────────────────────────────

    [Header("Victory UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private TextMeshProUGUI seedText;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button newLevelButton;
    [SerializeField] private Button quitButton;

    [Header("Seed Controls")]
    [SerializeField] private Toggle seedToggle;
    [SerializeField] private TMP_InputField seedInputField;

    [Header("Victory Phrases")]
    [SerializeField] private string[] victoryPhrases = new string[]
    {
        "PHEW!", "NICE!", "PERFECT!", "AWESOME!", "SPEEDY!"
    };

    [Header("References")]
    [SerializeField] private Timer timer;

    // ─── Challenge Sharing UI ────────────────────────────────────────────────────

    [Header("Challenge Sharing — Display")]
    [Tooltip("Shows the Challenge Code (= seed as-is, no transformation).")]
    [SerializeField] private TextMeshProUGUI challengeCodeValueText;
    [Tooltip("Shows the Route: 'Single Decker' or 'Double Decker'.")]
    [SerializeField] private TextMeshProUGUI routeValueText;

    [Header("Challenge Sharing — Buttons")]
    [SerializeField] private Button copyChallengeCodeButton;
    [Tooltip("Optional: feedback text shown briefly after copying (e.g. 'Copied!').")]
    [SerializeField] private TextMeshProUGUI copyCodeFeedbackText;
    [SerializeField] private Button createChallengeCardButton;
    [Tooltip("Label text on the Create Challenge Card button (changed during generation).")]
    [SerializeField] private TextMeshProUGUI createCardButtonLabel;
    [Tooltip("Optional: shown after successful save. Displays 'Challenge Card Saved ✓'.")]
    [SerializeField] private TextMeshProUGUI cardSavedFeedbackText;
    [Tooltip("Optional Open Folder button. Shown only if ChallengeConfig.showOpenFolderButton is true.")]
    [SerializeField] private Button openFolderButton;
    [Tooltip("Shown only if ChallengeConfig.discordUrl is non-empty.")]
    [SerializeField] private Button joinDiscordButton;

    [Header("Challenge Sharing — Services")]
    [Tooltip("ChallengeConfig ScriptableObject with all settings.")]
    [SerializeField] private ChallengeConfig challengeConfig;
    [Tooltip("ChallengeCardGenerator in the scene that composites and saves the card.")]
    [SerializeField] private ChallengeCardGenerator challengeCardGenerator;

    // ─── Private State ───────────────────────────────────────────────────────────

    private Coroutine phraseRotationCoroutine;
    private float completionTime;
    private int currentSeed;
    private bool isManualSeedMode = false;
    private ChallengeData currentChallengeData;
    
    protected override void SetupButtons()
    {
    }
    
    protected override void RegisterButtonCallbacks()
    {
        retryButton.onClick.AddListener(OnRetryClicked);
        newLevelButton.onClick.AddListener(OnNewLevelClicked);
        seedToggle.onValueChanged.AddListener(OnSeedToggleChanged);
        quitButton.onClick.AddListener(OnQuitClicked);

        // Challenge Sharing buttons
        copyChallengeCodeButton?.onClick.AddListener(OnCopyChallengeCodeClicked);
        createChallengeCardButton?.onClick.AddListener(OnCreateChallengeCardClicked);
        joinDiscordButton?.onClick.AddListener(OnJoinDiscordClicked);
        openFolderButton?.onClick.AddListener(OnOpenFolderClicked);

        // Subscribe to generator result
        if (challengeCardGenerator != null)
            challengeCardGenerator.OnGenerationComplete += OnCardGenerationComplete;
    }
    
    protected override void UnregisterButtonCallbacks()
    {
        retryButton.onClick.RemoveListener(OnRetryClicked);
        newLevelButton.onClick.RemoveListener(OnNewLevelClicked);
        seedToggle.onValueChanged.RemoveListener(OnSeedToggleChanged);
        quitButton.onClick.RemoveListener(OnQuitClicked);

        // Challenge Sharing buttons
        copyChallengeCodeButton?.onClick.RemoveListener(OnCopyChallengeCodeClicked);
        createChallengeCardButton?.onClick.RemoveListener(OnCreateChallengeCardClicked);
        joinDiscordButton?.onClick.RemoveListener(OnJoinDiscordClicked);
        openFolderButton?.onClick.RemoveListener(OnOpenFolderClicked);

        if (challengeCardGenerator != null)
            challengeCardGenerator.OnGenerationComplete -= OnCardGenerationComplete;
    }
    
    protected override void OnShow()
    {
        UpdateVictoryData();
        ShowRandomPhrase();
        InitialiseChallengeUI();
    }
    
    protected override void OnHide()
    {
    }
    
    private void UpdateVictoryData()
    {
        if (timer == null)
            timer = FindObjectOfType<Timer>();

        if (timer != null)
        {
            completionTime = timer.ElapsedTime;
            if (currentTimeText != null)
                currentTimeText.text = timer.GetFormattedTime();
        }
        else
        {
            GameLogger.LogError("UI", "VictoryPanel couldn't find a Timer in the scene! Time will be 0.");
        }

        if (LevelStarter.Instance != null)
        {
            currentSeed = LevelStarter.Instance.GetCurrentSeed();

            // Legacy display — renamed to show "Challenge Code" language
            if (seedText != null)
                seedText.text = $"Challenge Code: {currentSeed}";
        }

        bool isPersonalBest = false;
        bool isFirstClear   = false;
        float bestTime      = completionTime;

        if (LeaderboardManager.Instance != null)
        {
            // Check isFirstClear BEFORE saving so we can detect it
            isFirstClear = !LeaderboardManager.Instance.HasRecord(currentSeed);

            LeaderboardManager.Instance.SaveTime(currentSeed, completionTime);

            TimeRecord bestRecord = LeaderboardManager.Instance.GetBestTime(currentSeed);

            if (bestRecord != null)
            {
                bestTime      = bestRecord.time;
                isPersonalBest = Mathf.Approximately(completionTime, bestRecord.time);

                if (isPersonalBest)
                {
                    bestTimeText.text  = $"<wave>{bestRecord.GetFormattedTime()} (NEW!)";
                    bestTimeText.color = Color.yellow;
                }
                else
                {
                    bestTimeText.text  = $"{bestRecord.GetFormattedTime()}";
                    bestTimeText.color = Color.black;
                }
            }
            else
            {
                bestTimeText.text = "";
            }
        }

        // Build ChallengeData for the sharing system.
        // Challenge Code = seed.ToString() — no transformation applied.
        // Route is a separate field, derived from pathMode.
        LevelPathMode pathMode = LevelStarter.Instance != null
            ? LevelStarter.Instance.pathMode
            : LevelPathMode.DualPath;

        currentChallengeData = ChallengeData.Build(
            currentSeed,
            pathMode,
            completionTime,
            bestTime,
            isPersonalBest,
            isFirstClear,
            challengeConfig);
    }
    
    private void ShowRandomPhrase()
    {
        if (titleText != null && victoryPhrases.Length > 0)
        {
            int randomIndex = Random.Range(0, victoryPhrases.Length);
            titleText.text = victoryPhrases[randomIndex];
        }
    }
    
    private void OnSeedToggleChanged(bool useManualSeed)
    {
        isManualSeedMode = useManualSeed;
        seedInputField.interactable = useManualSeed;
        
        if (useManualSeed && LevelStarter.Instance != null)
        {
            seedInputField.text = LevelStarter.Instance.GetCurrentSeed().ToString();
        }
    }
    
    private void OnRetryClicked()
    {
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
        
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllPanels();
        }
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ReplayLevel();
        }
        else
        {
            GameLogger.LogError("UI", "LevelManager.Instance is null! Cannot retry.");
        }
    }
    
    private void OnNewLevelClicked()
    {
        if (isManualSeedMode && !string.IsNullOrEmpty(seedInputField.text))
        {
            if (int.TryParse(seedInputField.text, out int seed))
            {
                PlayerPrefs.SetInt("NewLevelSeed", seed);
                PlayerPrefs.SetInt("UseNewLevelSeed", 1);
            }
            else
            {
                GameLogger.LogWarning("UI", "Invalid seed format!");
                PlayerPrefs.SetInt("UseNewLevelSeed", 0);
            }
        }
        else
        {
            PlayerPrefs.SetInt("UseNewLevelSeed", 0);
        }
        
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllPanels();
        }
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ReplayLevel();
        }
        else
        {
            GameLogger.LogError("UI", "LevelManager.Instance is null! Cannot start new level.");
        }
    }
    
    private void OnQuitClicked()
    {
        GameLogger.LogUI("Quitting application...");
        
        Time.timeScale = 1f;
        
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMainMenu();
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

    // ─── Challenge Sharing ────────────────────────────────────────────────────────

    /// <summary>
    /// Sets up the Challenge UI section when the VictoryPanel opens.
    /// Shows/hides buttons based on ChallengeConfig (e.g. hides Discord if URL is empty).
    /// </summary>
    private void InitialiseChallengeUI()
    {
        if (challengeConfig == null) return;

        // Populate display fields
        if (challengeCodeValueText != null && currentChallengeData != null)
            challengeCodeValueText.text = currentChallengeData.challengeCode;

        if (routeValueText != null && currentChallengeData != null)
            routeValueText.text = currentChallengeData.routeName;

        // Show/hide Discord button based on config
        if (joinDiscordButton != null)
            joinDiscordButton.gameObject.SetActive(challengeConfig.HasDiscordUrl);

        // Show/hide Open Folder button (off by default — unreliable in Gaming Mode on Steam Deck)
        if (openFolderButton != null)
        {
            openFolderButton.gameObject.SetActive(false); // Revealed only after a successful save
        }

        // Reset Create Card button
        if (createChallengeCardButton != null)
            createChallengeCardButton.interactable = true;

        if (createCardButtonLabel != null)
            createCardButtonLabel.text = "Create Challenge Card";

        // Hide saved feedback initially
        if (cardSavedFeedbackText != null)
            cardSavedFeedbackText.gameObject.SetActive(false);

        // Hide copy feedback initially
        if (copyCodeFeedbackText != null)
            copyCodeFeedbackText.gameObject.SetActive(false);

        // Reset generator state so the button works on re-entry
        challengeCardGenerator?.ResetState();
    }

    // ─── Button Handlers ──────────────────────────────────────────────────────────

    private void OnCopyChallengeCodeClicked()
    {
        if (currentChallengeData == null) return;

        ChallengeShareService.CopyCode(currentChallengeData);
        StartCoroutine(ShowCopyFeedback());
    }

    private void OnCreateChallengeCardClicked()
    {
        if (challengeCardGenerator == null)
        {
            GameLogger.LogError("VICTORY_PANEL", "ChallengeCardGenerator not assigned.");
            return;
        }

        if (currentChallengeData == null)
        {
            GameLogger.LogWarning("VICTORY_PANEL", "ChallengeData not ready — cannot create card.");
            return;
        }

        if (challengeCardGenerator.CurrentState == ChallengeCardState.Generating) return;

        // Disable button to prevent double-tap during generation
        if (createChallengeCardButton != null)
            createChallengeCardButton.interactable = false;

        if (createCardButtonLabel != null)
            createCardButtonLabel.text = "Creating...";

        Texture2D capture = ChallengeCardCaptureService.Instance?.CapturedFrame;
        challengeCardGenerator.RequestGeneration(currentChallengeData, capture);
    }

    private void OnCardGenerationComplete(bool success, string pathOrError)
    {
        if (success)
        {
            if (createCardButtonLabel != null)
                createCardButtonLabel.text = "Challenge Card Saved ✓";

            if (cardSavedFeedbackText != null)
            {
                cardSavedFeedbackText.gameObject.SetActive(true);
                cardSavedFeedbackText.text = "Challenge Card Saved ✓";
            }

            // Reveal Open Folder button only if the config allows it
            if (openFolderButton != null && challengeConfig != null && challengeConfig.showOpenFolderButton)
            {
                openFolderButton.gameObject.SetActive(true);
            }

            // Release the capture from memory after the card has been generated
            ChallengeCardCaptureService.Instance?.ReleaseCapture();

            GameLogger.Log("VICTORY_PANEL", $"Challenge Card generated: {System.IO.Path.GetFileName(pathOrError)}");
        }
        else
        {
            if (createCardButtonLabel != null)
                createCardButtonLabel.text = "Create Challenge Card";

            if (createChallengeCardButton != null)
                createChallengeCardButton.interactable = true;

            GameLogger.LogWarning("VICTORY_PANEL", $"Challenge Card generation failed: {pathOrError}");
        }
    }

    private void OnOpenFolderClicked()
    {
        string savedPath = challengeCardGenerator?.LastSavedPath;
        if (string.IsNullOrEmpty(savedPath)) return;

        string directory = System.IO.Path.GetDirectoryName(savedPath);
        ChallengeCardStorageService.TryOpenFolder(directory);
    }

    private void OnJoinDiscordClicked()
    {
        if (challengeConfig == null) return;
        ChallengeShareService.OpenUrl(challengeConfig.discordUrl, "discord", "VictoryPanel");
    }

    // ─── UI Feedback Coroutines ───────────────────────────────────────────────────

    private IEnumerator ShowCopyFeedback()
    {
        if (copyCodeFeedbackText == null) yield break;

        copyCodeFeedbackText.gameObject.SetActive(true);
        copyCodeFeedbackText.text = "Copied!";
        yield return new WaitForSecondsRealtime(2f);
        copyCodeFeedbackText.gameObject.SetActive(false);
    }
}
