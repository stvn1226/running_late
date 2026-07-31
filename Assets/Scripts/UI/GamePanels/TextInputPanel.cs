using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TextInputPanel : BaseMainMenuPanel
{
    [SerializeField] private TMPro.TMP_InputField inputField;
    [SerializeField] private TMPro.TMP_Text placeholderText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    [SerializeField] private SavedLevelEntry currentEntry;
    
    private GameObject previouslySelectedObject;
    private CanvasGroup parentPanelCanvasGroup;

    protected override void SetupPanel()
    {
        inputField.characterLimit = 16;
        inputField.text = string.Empty;
        okButton.onClick.AddListener(RenameLevel);
        cancelButton.onClick.AddListener(OnBackClicked);
    }

    protected override void OnShow()
    {
        if (parentPanelCanvasGroup != null)
        {
            parentPanelCanvasGroup.interactable = false;
            GameLogger.Log("TEXT INPUT", "Disabled parent panel interactability");
        }
    }
    
    protected override void OnHide()
    {
        if (parentPanelCanvasGroup != null)
        {
            parentPanelCanvasGroup.interactable = true;
            GameLogger.Log("TEXT INPUT", "Re-enabled parent panel interactability");
        }
    }

    private void OnDestroy()
    {
        okButton.onClick.RemoveListener(RenameLevel);
        cancelButton.onClick.RemoveListener(OnBackClicked);
    }

    public void SetActiveEntry(SavedLevelEntry entry)
    {
        currentEntry = entry;
        
        if (EventSystem.current != null)
        {
            previouslySelectedObject = EventSystem.current.currentSelectedGameObject;
            GameLogger.Log("TEXT INPUT", $"Stored previously selected object: {previouslySelectedObject?.name}");
            
            if (previouslySelectedObject != null)
            {
                SavedLevelsPanel parentPanel = previouslySelectedObject.GetComponentInParent<SavedLevelsPanel>();
                if (parentPanel != null)
                {
                    parentPanelCanvasGroup = parentPanel.GetComponent<CanvasGroup>();
                    if (parentPanelCanvasGroup == null)
                    {
                        parentPanelCanvasGroup = parentPanel.gameObject.AddComponent<CanvasGroup>();
                    }
                }
            }
        }
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
            RestorePreviousSelection();
            return;
        }

        currentEntry.RenameLevel(newName);
        inputField.text = string.Empty;
        Hide();
        RestorePreviousSelection();
    }

    private void OnBackClicked()
    {
        inputField.text = string.Empty;
        Hide();
        RestorePreviousSelection();
    }
    
    public void RestorePreviousSelection()
    {
        if (previouslySelectedObject != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(previouslySelectedObject);
            GameLogger.Log("TEXT INPUT", $"Restored selection to: {previouslySelectedObject.name}");
        }
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
