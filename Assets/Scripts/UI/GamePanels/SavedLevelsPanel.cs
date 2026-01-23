using UnityEngine;

public class SavedLevelsPanel : BaseMainMenuPanel
{
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

    private void OnBackClicked()
    {
        if (menuController != null)
        {
            menuController.ShowMainPanel();
        }
    }
}
