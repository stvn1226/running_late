using UnityEngine;

public class RecentLevelsPanel : BaseMainMenuPanel
{
    [SerializeField] SavedLevelEntry savedLevelEntry;

    protected override void SetupPanel()
    {
        //if (defaultSelectedButton != null)
        //{
        //    defaultSelectedButton.onClick.AddListener(OnBackClicked);
        //}
        if (savedLevelEntry != null)
        {
            savedLevelEntry.startButton.onClick.AddListener(OnPlayClicked);
        }
    }

    private void OnDestroy()
    {
        //if (defaultSelectedButton != null)
        //{
        //    defaultSelectedButton.onClick.RemoveListener(OnBackClicked);
        //}
        if (savedLevelEntry != null)
        {
            savedLevelEntry.startButton.onClick.RemoveListener(OnPlayClicked);
        }
    }

    private void OnPlayClicked()
    {
        GameLogger.Log("RECENTS", "Play clicked");

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
