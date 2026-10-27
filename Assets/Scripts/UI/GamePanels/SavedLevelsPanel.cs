using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SavedLevelsPanel : BaseMainMenuPanel
{
    public bool isRecentsPanel = true;
    [SerializeField] private VerticalLayoutGroup entriesLayoutGroup;
    [SerializeField] private List<SavedLevelEntry> savedLevelEntries;
    [SerializeField] private SavedLevelEntry exampleEntry;
    [SerializeField] private TextInputPanel renamePanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private int visibleEntryCount = 5;

    private int currentTopVisibleIndex = 0;

    public List<SavedLevelEntry> SavedLevelEntries => savedLevelEntries;

    protected override void SetupPanel()
    {
        if (entriesLayoutGroup == null)
        {
            entriesLayoutGroup = GetComponentInChildren<VerticalLayoutGroup>();
        }
        
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
        }
        
        savedLevelEntries = new List<SavedLevelEntry>();
        SetupBackButton();
    }

    private void OnDestroy()
    {

    }

    public void OnPlayClicked(SavedLevelEntry savedLevelEntry)
    {
        GameLogger.Log("RECENTS", "Play clicked");

        PlayerPrefs.SetInt("UseNewLevelSeed", 1);
        PlayerPrefs.SetInt("NewLevelSeed", savedLevelEntry.levelSeed);
        PlayerPrefs.SetInt("NewLevelPathMode", savedLevelEntry.levelPathMode);
        PlayerPrefs.Save();

        if (menuController != null)
        {
            menuController.StartGame();
        }
    }

    public void OnRenameClicked(SavedLevelEntry savedLevelEntry)
    {
        GameLogger.Log("SAVED LEVELS", "Rename clicked");
        if (renamePanel != null)
        {
            renamePanel.SetActiveEntry(savedLevelEntry);
            renamePanel.ChangePlaceholderText(savedLevelEntry);
            renamePanel.Show();
        }
    }

    public void OnBackClicked()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);

        if (menuController != null)
        {
            menuController.ShowMainPanelStep2();
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        SetupBackButton();
    }

    private void SetupBackButton()
    {
        Button backBtn = null;
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            if (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("volver"))
            {
                backBtn = b;
                break;
            }
        }

        if (backBtn == null)
        {
            GameObject backObj = new GameObject("SavedLevelsBackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backObj.transform.SetParent(transform, false);
            backObj.transform.SetAsLastSibling();

            RectTransform rect = backObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(50f, -50f);
            rect.sizeDelta = new Vector2(180f, 65f);

            Image img = backObj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f, 0.88f);
            img.raycastTarget = true;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObj.transform.SetParent(backObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TMPro.TextMeshProUGUI tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "← VOLVER";
            tmp.fontSize = 28f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var existingText = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (existingText != null && existingText.font != null)
            {
                tmp.font = existingText.font;
            }

            backBtn = backObj.GetComponent<Button>();
            backBtn.targetGraphic = img;

            ColorBlock cb = backBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.9f, 0.4f, 1f);
            cb.pressedColor = new Color(0.8f, 0.7f, 0.2f, 1f);
            backBtn.colors = cb;
        }

        if (backBtn != null)
        {
            backBtn.onClick.RemoveListener(OnBackClicked);
            backBtn.onClick.AddListener(OnBackClicked);
            backBtn.gameObject.SetActive(true);
        }
    }

    public void PopulateMenu(bool isFromDelete = false, int whereDidRemove = 0)
    {
        int ct = 0;
        savedLevelEntries.Clear();
        SavedLevelEntry[] existingEntries = entriesLayoutGroup.GetComponentsInChildren<SavedLevelEntry>();
        foreach (SavedLevelEntry entry in existingEntries)
        {
            Destroy(entry.gameObject);
        }

        if (isRecentsPanel)
        {
            foreach (LevelSavingParams l in RecentLevelsList.instance.GetRecentLevels())
            {
                SavedLevelEntry newEntry = Instantiate(exampleEntry, entriesLayoutGroup.transform);
                newEntry.SetupEntry(l.savedLevelSeed, l.savedLevelName, l.pathMode, isRecentsPanel, ct);
                savedLevelEntries.Add(newEntry);
                ct++;
            }
        }
        else
        {
            foreach (LevelSavingParams l in SavedLevelsList.instance.GetSavedLevels())
            {
                SavedLevelEntry newEntry = Instantiate(exampleEntry, entriesLayoutGroup.transform);
                newEntry.SetupEntry(l.savedLevelSeed, l.savedLevelName, l.pathMode, isRecentsPanel, ct);
                savedLevelEntries.Add(newEntry);
                ct++;
            }
        }

        currentTopVisibleIndex = 0;
        scrollRect.verticalNormalizedPosition = 1.0f;

        if (savedLevelEntries.Count > 0)
        {
            int targetIndex = isFromDelete ? whereDidRemove : 0;
            
            if (targetIndex >= savedLevelEntries.Count)
            {
                targetIndex = savedLevelEntries.Count - 1;
            }

            defaultSelectedButton = savedLevelEntries[targetIndex].startButton;
            
            if (isFromDelete)
            {
                EventSystem.current.SetSelectedGameObject(savedLevelEntries[targetIndex].deleteButton.gameObject);
            }
        }
        else
        {
            defaultSelectedButton = null;
        }
    }

    public void UpdateScrollPosition(int selectedIndex)
    {
        if (scrollRect == null || savedLevelEntries.Count == 0)
            return;

        int totalEntries = savedLevelEntries.Count;
        
        if (totalEntries <= visibleEntryCount)
        {
            scrollRect.verticalNormalizedPosition = 1.0f;
            currentTopVisibleIndex = 0;
            return;
        }

        int bottomVisibleIndex = currentTopVisibleIndex + visibleEntryCount - 1;
        
        if (selectedIndex < currentTopVisibleIndex)
        {
            currentTopVisibleIndex = selectedIndex;
        }
        else if (selectedIndex > bottomVisibleIndex)
        {
            currentTopVisibleIndex = selectedIndex - (visibleEntryCount - 1);
        }

        int scrollSteps = totalEntries - visibleEntryCount;
        float scrollValue = 1.0f - ((float)currentTopVisibleIndex / scrollSteps);
        
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollValue);
    }

    public int GetEntryIndex(SavedLevelEntry entry)
    {
        return savedLevelEntries.IndexOf(entry);
    }
}
