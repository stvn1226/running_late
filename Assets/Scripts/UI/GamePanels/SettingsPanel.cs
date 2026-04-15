using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SettingsPanel : BaseMainMenuPanel
{
    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Toggle masterMuteToggle;
    
    [Space]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Toggle musicMuteToggle;
    
    [Space]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Toggle sfxMuteToggle;
    
    [Header("Display Settings")]
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Toggle fps60Toggle;
    [SerializeField] private Toggle fps120Toggle;
    [SerializeField] private TMP_Dropdown fullscreenModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    
    [Header("Controller Settings")]
    [SerializeField] private Toggle vibrationToggle;
    
    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button resetButton;
    
    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;
    
    private Selectable firstSelectable;
    [SerializeField] private PauseMenuPanel pauseMenuReference;
    [SerializeField] private TextMeshProUGUI headerText;
    private bool isInitialized = false;
    
    [Header("Input Prompts")]
    [SerializeField] private InputPromptUI submitButtonPrompt;
    [SerializeField] private InputPromptUI backButtonPrompt;
    [SerializeField] private TextMeshProUGUI controllerTypeText;

    
    private void Awake()
    {
        if (!isInitialized)
        {
            SetupPanel();
            isInitialized = true;
        }
    }
    
    public override void Initialize(MainMenuController controller)
    {
        if (!isInitialized)
        {
            base.Initialize(controller);
            isInitialized = true;
        }
    }
    
    protected override void SetupPanel()
    {
        firstSelectable = defaultSelected != null ? defaultSelected : defaultSelectedButton;
        
        SetupAudioSliders();
        SetupAudioMuteToggles();
        SetupDisplayToggles();
        SetupVibrationToggle();
        SetupButtons();
        SetupScrollNavigation();
    }
    
    private void SetupScrollNavigation()
    {
        Debug.Log("=== SETUP SCROLL NAVIGATION ===");
    
        AddScrollListener(masterVolumeSlider, "MasterVolumeSlider");
        AddScrollListener(masterMuteToggle, "MasterMuteToggle");
        AddScrollListener(musicVolumeSlider, "MusicVolumeSlider");
        AddScrollListener(musicMuteToggle, "MusicMuteToggle");
        AddScrollListener(sfxVolumeSlider, "SFXVolumeSlider");
        AddScrollListener(sfxMuteToggle, "SFXMuteToggle");
        AddScrollListener(vSyncToggle, "VSyncToggle");
        AddScrollListener(fps60Toggle, "60FPSToggle");
        AddScrollListener(fps120Toggle, "120FPSToggle");
        AddScrollListener(fullscreenModeDropdown, "FullscreenDropdown");
        AddScrollListener(resolutionDropdown, "ResolutionDropdown");
        AddScrollListener(vibrationToggle, "VibrationToggle");
        AddScrollListener(backButton, "BackButton");
        AddScrollListener(resetButton, "ResetButton");
    }

    private void AddScrollListener(Selectable selectable, string debugName)
    {
        if (selectable == null) return;
    
        RectTransform rect = selectable.GetComponent<RectTransform>();
        if (rect != null && scrollRect != null)
        {
            float absY = GetAbsoluteYPositionInContent(rect, scrollRect.content);
            Debug.Log($"  {debugName}: AbsY = {absY:F1}");
        }
    
        EventTrigger trigger = selectable.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = selectable.gameObject.AddComponent<EventTrigger>();
        }
    
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Select;
        entry.callback.AddListener((data) => { ScrollToSelectable(selectable); });
        trigger.triggers.Add(entry);
    }
    
    private void ScrollToSelectable(Selectable selectable)
    {
        if (scrollRect == null || selectable == null)
            return;

        RectTransform selectableRect = selectable.GetComponent<RectTransform>();
        if (selectableRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        float viewportHeight = viewport.rect.height;
        float contentHeight = content.rect.height;

        if (contentHeight <= viewportHeight)
            return;

        float elementLocalY = GetAbsoluteYPositionInContent(selectableRect, content);
        float elementHeight = selectableRect.rect.height;

        float targetScrollPos;
        
        float topSectionThreshold = 600f;
        
        if (elementLocalY <= topSectionThreshold)
        {
            targetScrollPos = 0f;
        }
        else if (elementLocalY < viewportHeight * 0.6f)
        {
            targetScrollPos = Mathf.Max(0f, elementLocalY - 150f);
        }
        else
        {
            float elementCenter = elementLocalY + (elementHeight * 0.5f);
            float viewportCenter = viewportHeight * 0.5f;
            targetScrollPos = elementCenter - viewportCenter;
        }

        float maxScrollPos = contentHeight - viewportHeight;
        targetScrollPos = Mathf.Clamp(targetScrollPos, 0f, maxScrollPos);

        Debug.Log($"Element: {selectable.name}, AbsY: {elementLocalY:F1}, Current: {content.anchoredPosition.y:F1}, Target: {targetScrollPos:F1}");

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetScrollPos);
    }

    private float GetAbsoluteYPositionInContent(RectTransform element, RectTransform content)
    {
        float totalY = 0f;
        RectTransform current = element;
        
        while (current != null && current != content)
        {
            totalY += Mathf.Abs(current.anchoredPosition.y);
            current = current.parent as RectTransform;
        }
        
        return totalY;
    }
    
    private void SetupAudioSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }
    
    private void SetupAudioMuteToggles()
    {
        if (masterMuteToggle != null)
        {
            masterMuteToggle.onValueChanged.AddListener(OnMasterMuteToggled);
        }
    
        if (musicMuteToggle != null)
        {
            musicMuteToggle.onValueChanged.AddListener(OnMusicMuteToggled);
        }
    
        if (sfxMuteToggle != null)
        {
            sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteToggled);
        }
    }

    private void OnMasterMuteToggled(bool enabled)
    {
        bool muted = !enabled;
        AudioSettingsManager.Instance?.SetMasterMuted(muted);
    
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.interactable = enabled;
        }
        
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
    }

    private void OnMusicMuteToggled(bool enabled)
    {
        bool muted = !enabled;
        AudioSettingsManager.Instance?.SetMusicMuted(muted);
    
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.interactable = enabled;
        }
        
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
    }

    private void OnSFXMuteToggled(bool enabled)
    {
        bool muted = !enabled;
        AudioSettingsManager.Instance?.SetSFXMuted(muted);
    
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.interactable = enabled;
        }
        
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
    }
    
    private void SetupDisplayToggles()
    {
        if (vSyncToggle != null)
        {
            vSyncToggle.onValueChanged.AddListener(OnVSyncToggled);
        }
        
        if (fps60Toggle != null)
        {
            fps60Toggle.onValueChanged.AddListener((isOn) => {
                if (isOn) OnFrameRateSelected(60);
            });
        }
        
        if (fps120Toggle != null)
        {
            fps120Toggle.onValueChanged.AddListener((isOn) => {
                if (isOn) OnFrameRateSelected(120);
            });
        }
        
        if (fullscreenModeDropdown != null)
        {
            fullscreenModeDropdown.ClearOptions();
            fullscreenModeDropdown.AddOptions(new List<string>
            {
                "Fullscreen",
                "Windowed",
                "Borderless"
            });
            fullscreenModeDropdown.onValueChanged.AddListener(OnFullscreenModeChanged);
            SetDropdownHeight(fullscreenModeDropdown, 400); // Set height to 400
            AddDropdownFocusFix(fullscreenModeDropdown);
        }
        
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            
            if (DisplaySettingsManager.Instance != null)
            {
                List<Resolution> resolutions = DisplaySettingsManager.Instance.GetAvailableResolutions();
                List<string> options = new List<string>();
                
                for (int i = 0; i < resolutions.Count; i++)
                {
                    options.Add(DisplaySettingsManager.Instance.GetResolutionLabel(i));
                }
                
                resolutionDropdown.AddOptions(options);
            }
            
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            SetDropdownHeight(resolutionDropdown, 600); // Set resolution list taller (600)
            AddDropdownFocusFix(resolutionDropdown);
        }
    }
    
    private void SetDropdownHeight(TMP_Dropdown dropdown, float height)
    {
        if (dropdown == null || dropdown.template == null) return;
        
        // The expanded size is controlled by the Template's height
        Vector2 size = dropdown.template.sizeDelta;
        size.y = height;
        dropdown.template.sizeDelta = size;
        
        GameLogger.Log("UI", $"Set {dropdown.name} template height to {height}");
    }
    
    private void AddDropdownFocusFix(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;

        EventTrigger trigger = dropdown.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = dropdown.gameObject.AddComponent<EventTrigger>();
        }

        // Handle opening via PointerClick
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback.AddListener((data) => { StartCoroutine(FixDropdownList(dropdown)); });
        trigger.triggers.Add(pointerClick);

        // Handle opening via Submit (Controller/Keyboard)
        EventTrigger.Entry submit = new EventTrigger.Entry();
        submit.eventID = EventTriggerType.Submit;
        submit.callback.AddListener((data) => { StartCoroutine(FixDropdownList(dropdown)); });
        trigger.triggers.Add(submit);
    }

    private IEnumerator FixDropdownList(TMP_Dropdown dropdown)
    {
        // Give it a moment to instantiate the list
        yield return new WaitForSecondsRealtime(0.05f);
        
        Transform listTransform = dropdown.transform.Find("Dropdown List");
        if (listTransform == null)
        {
            GameObject listObj = GameObject.Find("Dropdown List");
            if (listObj != null) listTransform = listObj.transform;
        }

        if (listTransform != null)
        {
            ScrollRect scrollRect = listTransform.GetComponentInChildren<ScrollRect>();
            RectTransform content = scrollRect != null ? scrollRect.content : null;
            RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;

            if (content != null && viewport != null)
            {
                // 1. Force initial selection to current value
                int targetChildIndex = dropdown.value + 1;
                if (targetChildIndex < content.childCount)
                {
                    GameObject initialTarget = content.GetChild(targetChildIndex).gameObject;
                    if (EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(initialTarget);
                    }
                }

                // 2. Loop while the dropdown is open to handle scrolling
                GameObject lastSelected = null;
                while (listTransform != null && listTransform.gameObject.activeInHierarchy)
                {
                    GameObject currentSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                    
                    if (currentSelected != null && currentSelected != lastSelected && currentSelected.transform.IsChildOf(content))
                    {
                        lastSelected = currentSelected;
                        
                        // Manual Scroll Logic
                        Canvas.ForceUpdateCanvases();
                        RectTransform selectedRect = currentSelected.GetComponent<RectTransform>();
                        
                        float viewportHeight = viewport.rect.height;
                        float contentHeight = content.rect.height;

                        if (contentHeight > viewportHeight)
                        {
                            // Position relative to content top
                            float elementTop = Mathf.Abs(selectedRect.anchoredPosition.y) - (selectedRect.rect.height * selectedRect.pivot.y);
                            float elementBottom = elementTop + selectedRect.rect.height;
                            
                            // Current visible range
                            float scrollProgress = content.anchoredPosition.y; // 0 at top, increases as we scroll down
                            float visibleTop = scrollProgress;
                            float visibleBottom = scrollProgress + viewportHeight;

                            float targetScroll = scrollProgress;

                            if (elementTop < visibleTop)
                            {
                                targetScroll = elementTop;
                            }
                            else if (elementBottom > visibleBottom)
                            {
                                targetScroll = elementBottom - viewportHeight;
                            }

                            targetScroll = Mathf.Clamp(targetScroll, 0f, contentHeight - viewportHeight);
                            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetScroll);
                        }
                    }
                    yield return null;
                }
            }
        }
    }
    
    private void SetupVibrationToggle()
    {
        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
        }
    }
    
    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
    }
    
    protected override void OnShow()
    {
        if (headerText != null)
        {
            // Use the localized title or standard "SETTINGS"
            headerText.text = "SETTINGS"; 
        }
        LoadCurrentSettings();
        ResetScrollToTop();
        UpdateControllerTypeDisplay();
    }
    
    private void UpdateControllerTypeDisplay()
    {
        if (controllerTypeText != null && ControllerTypeDetector.Instance != null)
        {
            ControllerType currentType = ControllerTypeDetector.Instance.CurrentControllerType;
            string deviceName = "";
        
            switch (currentType)
            {
                case ControllerType.Keyboard:
                    deviceName = "Keyboard & Mouse";
                    break;
                case ControllerType.Xbox:
                    deviceName = "Xbox Controller";
                    break;
                case ControllerType.Playstation:
                    deviceName = "PlayStation Controller";
                    break;
                case ControllerType.Switch:
                    deviceName = "Switch Controller";
                    break;
                case ControllerType.Generic:
                    deviceName = "Generic Gamepad";
                    break;
            }
        
            controllerTypeText.text = $"Device: {deviceName}";
        }
    }

    private void OnEnable()
    {
        if (ControllerTypeDetector.Instance != null)
        {
            ControllerTypeDetector.Instance.OnControllerChanged += OnControllerTypeChanged;
        }
    }

    private void OnDisable()
    {
        if (ControllerTypeDetector.Instance != null)
        {
            ControllerTypeDetector.Instance.OnControllerChanged -= OnControllerTypeChanged;
        }
    }

    private void OnControllerTypeChanged(ControllerType newType)
    {
        UpdateControllerTypeDisplay();
        GameLogger.LogInput($"Settings menu updated for controller: {newType}");
    }

    private void ResetScrollToTop()
    {
        if (scrollRect != null && scrollRect.content != null)
        {
            scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, 0f);
        }
    }
    
    private void LoadCurrentSettings()
    {
        if (AudioSettingsManager.Instance != null)
        {
            float masterVolume = AudioSettingsManager.Instance.GetMasterVolume();
            float musicVolume = AudioSettingsManager.Instance.GetMusicVolume();
            float sfxVolume = AudioSettingsManager.Instance.GetSFXVolume();
            
            bool masterMuted = AudioSettingsManager.Instance.IsMasterMuted();
            bool musicMuted = AudioSettingsManager.Instance.IsMusicMuted();
            bool sfxMuted = AudioSettingsManager.Instance.IsSFXMuted();
            
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(masterVolume);
                masterVolumeSlider.interactable = !masterMuted;
                UpdateVolumeText(masterVolumeText, masterVolume);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(musicVolume);
                musicVolumeSlider.interactable = !musicMuted;
                UpdateVolumeText(musicVolumeText, musicVolume);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
                sfxVolumeSlider.interactable = !sfxMuted;
                UpdateVolumeText(sfxVolumeText, sfxVolume);
            }
            
            if (masterMuteToggle != null)
            {
                masterMuteToggle.SetIsOnWithoutNotify(!masterMuted);
            }
            
            if (musicMuteToggle != null)
            {
                musicMuteToggle.SetIsOnWithoutNotify(!musicMuted);
            }
            
            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.SetIsOnWithoutNotify(!sfxMuted);
            }
        }
        
        if (DisplaySettingsManager.Instance != null)
        {
            bool vSyncEnabled = DisplaySettingsManager.Instance.IsVSyncEnabled();
            int targetFPS = DisplaySettingsManager.Instance.GetTargetFrameRate();
            FullScreenMode fullscreenMode = DisplaySettingsManager.Instance.GetFullscreenMode();
            int resolutionIndex = DisplaySettingsManager.Instance.GetCurrentResolutionIndex();
            
            if (vSyncToggle != null)
            {
                vSyncToggle.SetIsOnWithoutNotify(vSyncEnabled);
            }
            
            if (fps60Toggle != null)
            {
                fps60Toggle.SetIsOnWithoutNotify(targetFPS == 60);
            }
            
            if (fps120Toggle != null)
            {
                fps120Toggle.SetIsOnWithoutNotify(targetFPS == 120);
            }
            
            if (fullscreenModeDropdown != null)
            {
                int modeIndex = 0;
                switch (fullscreenMode)
                {
                    case FullScreenMode.ExclusiveFullScreen:
                        modeIndex = 0;
                        break;
                    case FullScreenMode.Windowed:
                        modeIndex = 1;
                        break;
                    case FullScreenMode.FullScreenWindow:
                        modeIndex = 2;
                        break;
                }
                fullscreenModeDropdown.SetValueWithoutNotify(modeIndex);
            }
            
            if (resolutionDropdown != null)
            {
                resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            }
        }
        
        if (GamepadVibrationManager.Instance != null)
        {
            bool vibrationEnabled = GamepadVibrationManager.Instance.IsVibrationEnabled();
            
            if (vibrationToggle != null)
            {
                vibrationToggle.SetIsOnWithoutNotify(vibrationEnabled);
            }
        }
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        AudioSettingsManager.Instance?.SetMasterVolume(value);
        UpdateVolumeText(masterVolumeText, value);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        AudioSettingsManager.Instance?.SetMusicVolume(value);
        UpdateVolumeText(musicVolumeText, value);
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        AudioSettingsManager.Instance?.SetSFXVolume(value);
        UpdateVolumeText(sfxVolumeText, value);
    }
    
    private void UpdateVolumeText(TextMeshProUGUI textComponent, float volume)
    {
        if (textComponent != null)
        {
            textComponent.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }
    
    private void OnVSyncToggled(bool enabled)
    {
        DisplaySettingsManager.Instance?.SetVSync(enabled ? 1 : 0);
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
    }
    
    private void OnFrameRateSelected(int targetFPS)
    {
        DisplaySettingsManager.Instance?.SetTargetFrameRate(targetFPS);
        
        if (fps60Toggle != null)
        {
            fps60Toggle.SetIsOnWithoutNotify(targetFPS == 60);
        }
        
        if (fps120Toggle != null)
        {
            fps120Toggle.SetIsOnWithoutNotify(targetFPS == 120);
        }
        
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
    }
    
    private void OnFullscreenModeChanged(int modeIndex)
    {
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        
        switch (modeIndex)
        {
            case 0:
                mode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                mode = FullScreenMode.Windowed;
                break;
            case 2:
                mode = FullScreenMode.FullScreenWindow;
                break;
        }
        
        DisplaySettingsManager.Instance?.SetFullscreenMode(mode);
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
    }
    
    private void OnResolutionChanged(int resolutionIndex)
    {
        DisplaySettingsManager.Instance?.SetResolution(resolutionIndex);
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.05f);
    }
    
    private void OnVibrationToggled(bool enabled)
    {
        GamepadVibrationManager.Instance?.SetVibrationEnabled(enabled);
        
        if (enabled)
        {
            GamepadVibrationManager.Instance?.TriggerMediumVibration(0.15f);
        }
    }
    
    private void OnResetClicked()
    {
        AudioSettingsManager.Instance?.ResetToDefaults();
        DisplaySettingsManager.Instance?.ResetToDefaults();
        GamepadVibrationManager.Instance?.SetVibrationEnabled(true);
        
        LoadCurrentSettings();
        
        GamepadVibrationManager.Instance?.TriggerMediumVibration(0.15f);
        GameLogger.Log("SETTINGS", "All settings reset to defaults");
    }
    
    public void ShowFromPause()
    {
        Show();
        
        StartCoroutine(ForceSelectionNextFrame());
    }
    
    private IEnumerator ForceSelectionNextFrame()
    {
        yield return null;
        
        EventSystem.current.SetSelectedGameObject(null);
        
        yield return null;
        
        SetDefaultSelection();
    }
    
    private void OnBackClicked()
    {
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
    
        if (pauseMenuReference != null)
        {
            Hide();
            pauseMenuReference.ShowFromSettings();
        }
        else
        {
            menuController?.ShowMainPanel();
        }
    }
}
