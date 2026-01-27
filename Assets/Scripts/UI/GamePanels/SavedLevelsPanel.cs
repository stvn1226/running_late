using UnityEngine;

public class SavedLevelsPanel : BaseMainMenuPanel
{
    [SerializeField] SavedLevelEntry savedLevelEntry;

    protected override void SetupPanel()
    {
        //if (savedLevelEntry != null)
        //{
        //    savedLevelEntry.startButton.onClick.AddListener(OnPlayClicked);
        //}
    }

    private void OnDestroy()
    {
        //if (savedLevelEntry != null)
        //{
        //    savedLevelEntry.startButton.onClick.RemoveListener(OnPlayClicked);
        //}
    }

    public void OnPlayClicked()
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

    private void OnBackClicked()
    {
        if (menuController != null)
        {
            menuController.ShowMainPanel();
        }
    }
}
