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

    private void OnBackClicked()
    {
        if (menuController != null)
        {
            menuController.ShowMainPanelStep2();
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
