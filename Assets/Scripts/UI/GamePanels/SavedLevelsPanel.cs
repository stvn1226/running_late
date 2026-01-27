using UnityEngine;
using UnityEngine.UI;

public class SavedLevelsPanel : BaseMainMenuPanel
{
    [SerializeField] private VerticalLayoutGroup entriesLayoutGroup;
    [SerializeField] private SavedLevelEntry[] savedLevelEntries;
    [SerializeField] private TextInputPanel renamePanel;

    protected override void SetupPanel()
    {
        entriesLayoutGroup = GetComponentInChildren<VerticalLayoutGroup>();

        if (entriesLayoutGroup != null)
        {
            // populate entriesLayoutGroup with savedLevelEntries
        }
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
}
