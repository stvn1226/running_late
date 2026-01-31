using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TextInputPanel : BaseMainMenuPanel
{
    [SerializeField] private TMPro.TMP_InputField inputField;
    [SerializeField] private TMPro.TMP_Text placeholderText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    [SerializeField] private SavedLevelEntry currentEntry;

    protected override void SetupPanel()
    {
        inputField.text = string.Empty;
        okButton.onClick.AddListener(RenameLevel);
        cancelButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDestroy()
    {
        okButton.onClick.RemoveListener(RenameLevel);
        cancelButton.onClick.RemoveListener(OnBackClicked);
    }

    public void SetActiveEntry(SavedLevelEntry entry)
    {
        currentEntry = entry;
    }

    private void RenameLevel()
    {
        if (currentEntry == null)
        {
            GameLogger.Log("TEXT INPUT", "No entry to rename");
            return;
        }

        string newName = inputField.text.Trim();
        
        if (string.IsNullOrEmpty(newName))
        {
            GameLogger.Log("TEXT INPUT", "Empty name provided, keeping original name");
            inputField.text = string.Empty;
            Hide();
            return;
        }

        currentEntry.RenameLevel(newName);
        inputField.text = string.Empty;
        Hide();
    }

    private void OnBackClicked()
    {
        inputField.text = string.Empty;
        Hide();
    }

    public void ChangePlaceholderText(SavedLevelEntry entry)
    {
        if (entry != null && placeholderText != null)
        {
            placeholderText.text = entry.levelName;
        }
        
        if (inputField != null)
        {
            inputField.text = string.Empty;
        }
    }
}
