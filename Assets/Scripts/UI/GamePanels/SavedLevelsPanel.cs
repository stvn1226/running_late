using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SavedLevelsPanel : BaseMainMenuPanel
{
    public bool isRecentsPanel = true;
    [SerializeField] private VerticalLayoutGroup entriesLayoutGroup;
    [SerializeField] private List<SavedLevelEntry> savedLevelEntries;
    [SerializeField] private SavedLevelEntry exampleEntry;
    [SerializeField] private TextInputPanel renamePanel;

    protected override void SetupPanel()
    {
        entriesLayoutGroup = GetComponentInChildren<VerticalLayoutGroup>();
        savedLevelEntries = new List<SavedLevelEntry>();

        //if (entriesLayoutGroup != null)
        //{
        //    // populate entriesLayoutGroup with savedLevelEntries
        //}
    }

    private void OnDestroy()
    {

    }

    public void OnPlayClicked(SavedLevelEntry savedLevelEntry)
    {
        GameLogger.Log("RECENTS", "Play clicked");

        PlayerPrefs.SetInt("UseNewLevelSeed", 1);
        PlayerPrefs.SetInt("NewLevelSeed", savedLevelEntry.levelSeed);
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
            menuController.ShowMainPanel();
        }
    }

    public void PopulateMenu()
    {
        savedLevelEntries.Clear();
        SavedLevelEntry[] existingEntries = entriesLayoutGroup.GetComponentsInChildren<SavedLevelEntry>();
        foreach (SavedLevelEntry entry in existingEntries)
        {
            Destroy(entry.gameObject);
        }

        if (isRecentsPanel)
        {
            // Load recent levels and populate savedLevelEntries
            foreach (LevelSavingParams l in RecentLevelsList.instance.GetRecentLevels())
            {
                // Create SavedLevelEntry for each recent level and add to savedLevelEntries
                SavedLevelEntry newEntry = Instantiate(exampleEntry, entriesLayoutGroup.transform);
                newEntry.SetupEntry(l.savedLevelSeed, l.savedLevelName, isRecentsPanel);
                savedLevelEntries.Add(newEntry);
            }
        }
        else
        {
            // Load all saved levels and populate savedLevelEntries
            foreach (LevelSavingParams l in SavedLevelsList.instance.GetSavedLevels())
            {
                // Create SavedLevelEntry for each recent level and add to savedLevelEntries
                SavedLevelEntry newEntry = Instantiate(exampleEntry, entriesLayoutGroup.transform);
                newEntry.SetupEntry(l.savedLevelSeed, l.savedLevelName, isRecentsPanel);
                savedLevelEntries.Add(newEntry);
            }

        }
    }
}
