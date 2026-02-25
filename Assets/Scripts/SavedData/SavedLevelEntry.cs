using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Utils;

public class SavedLevelEntry : MonoBehaviour
{
    public int rowNumber;

    [SerializeField] private Image bgImage;
    [SerializeField] private TextMeshProUGUI levelNameDisplay;
    [SerializeField] private TextMeshProUGUI levelSeedDisplay;
    [SerializeField] private TextMeshProUGUI levelBestTimeDisplay;
    [SerializeField] public string levelName;
    [SerializeField] public int levelSeed;
    [SerializeField] private TimeRecord levelBestTime;
    [SerializeField] public Button startButton;
    [SerializeField] public Button renameButton;
    [SerializeField] public Button saveButton;
    [SerializeField] public Button deleteButton;

    private SavedLevelsPanel parent;
    private bool isRecentsEntry;
    private PlayerInputActions inputActions;
    private GameObject lastSelectedObject;

    public SavedLevelEntry(int seed, string name)
    {
        levelSeed = seed;
        levelName = name;
    }

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void Start()
    {
        LoadLevelNameFromPlayerPrefs();
        LoadBestTimeFromRecords();
        
        parent = GetComponentInParent<SavedLevelsPanel>();

        startButton.onClick.AddListener(StartLevel);
        renameButton.onClick.AddListener(OpenRenamePanel);
        saveButton.onClick.AddListener(SaveLevel);
        deleteButton.onClick.AddListener(DeleteLevel);
    }

    private void OnEnable()
    {
        inputActions.UI.Navigate.performed += OnNavigate;
        inputActions.UI.Enable();
    }

    private void OnDisable()
    {
        inputActions.UI.Navigate.performed -= OnNavigate;
        inputActions.UI.Disable();
    }

    private void Update()
    {
        CheckIfHighlighted();
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (IsAnyButtonSelected() && parent != null)
        {
            parent.UpdateScrollPosition(rowNumber);
        }
    }

    private bool IsAnyButtonSelected()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected == startButton.gameObject || 
               selected == renameButton.gameObject || 
               selected == saveButton.gameObject || 
               selected == deleteButton.gameObject;
    }

    public void SetupEntry(int seed, string name, bool isRecents, int rowNum)
    {
        rowNumber = rowNum;
        levelSeed = seed;
        levelName = name;
        isRecentsEntry = isRecents;

        if (isRecents)
        {
            levelName = name;
        }
        else
        {
            LoadLevelNameFromPlayerPrefs();
        }

        if (levelNameDisplay != null)
        {
            levelNameDisplay.text = levelName;
        }
        if (levelSeedDisplay != null)
        {
            levelSeedDisplay.text = $"Seed: {levelSeed}";
        }

        LoadBestTimeFromRecords();

        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(isRecents);
        }
        if (renameButton != null)
        {
            renameButton.gameObject.SetActive(!isRecents);
        }
    }

    private void StartLevel()
    {
        parent.OnPlayClicked(this);
    }

    private void OpenRenamePanel() //Used only in the Saved Levels menu
    {
        parent.OnRenameClicked(this);
    }

    public void RenameLevel(string newName) //Used only in the Saved Levels menu
    {
        levelName = newName;
        levelNameDisplay.text = levelName;
        
        SaveLevelNameToPlayerPrefs();
    }
    
    private void SaveLevelNameToPlayerPrefs()
    {
        string key = $"SavedLevel_{levelSeed}_Name";
        PlayerPrefs.SetString(key, levelName);
        PlayerPrefs.Save();
        GameLogger.Log("SAVED LEVELS", $"Saved level name '{levelName}' for seed {levelSeed}");
    }
    
    public void LoadLevelNameFromPlayerPrefs()
    {
        string key = $"SavedLevel_{levelSeed}_Name";
        if (PlayerPrefs.HasKey(key))
        {
            levelName = PlayerPrefs.GetString(key);
            if (levelNameDisplay != null)
            {
                levelNameDisplay.text = levelName;
            }
            GameLogger.Log("SAVED LEVELS", $"Loaded level name '{levelName}' for seed {levelSeed}");
        }
    }

    public void LoadBestTimeFromRecords()
    {
        if (LeaderboardManager.Instance == null)
        {
            GameLogger.LogWarning("SAVED LEVELS", "LeaderboardManager instance not found");
            if (levelBestTimeDisplay != null)
            {
                levelBestTimeDisplay.text = "Best: -:--.--";
            }
            return;
        }

        levelBestTime = LeaderboardManager.Instance.GetBestTime(levelSeed);
        if (levelBestTimeDisplay != null)
        {
            if (levelBestTime != null)
            {
                levelBestTimeDisplay.text = $"Best: {levelBestTime.GetFormattedTime()}";
            }
            else
            {
                levelBestTimeDisplay.text = "Best: -:--.--";
            }
        }
    }

    private void SaveLevel() //Used only in the Recent Levels menu
    {
        if (SavedLevelsList.instance != null)
        {
            SavedLevelsList.instance.AddSavedLevel(levelSeed);

            SaveLevelNameToPlayerPrefs();

            GameLogger.Log("SAVED LEVELS", $"Saved level {levelSeed} ('{levelName}') to Saved Levels");
        }
        else
        {
            GameLogger.LogWarning("SAVED LEVELS", "SavedLevelsList instance not found");
        }

    }

    private void DeleteLevel()
    {
        int whereDidRemove = 0;

        if (isRecentsEntry)
        {
            whereDidRemove = RecentLevelsList.instance.RemoveRecentLevel(levelSeed);
            GameLogger.Log("RECENT LEVELS", $"Removed level {levelSeed} from Recent Levels");
        }
        else
        {
            string key = $"SavedLevel_{levelSeed}_Name";
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                GameLogger.Log("SAVED LEVELS", $"Deleted saved level name for seed {levelSeed}");
            }

            whereDidRemove = SavedLevelsList.instance.RemoveSavedLevel(levelSeed);
            GameLogger.Log("SAVED LEVELS", $"Removed level {levelSeed} from Saved Levels");
        }

        parent.PopulateMenu(true, whereDidRemove);
    }

    private void CheckIfHighlighted()
    {
        if (bgImage == null) return;
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        
        if (currentSelected != lastSelectedObject)
        {
            if (IsAnyButtonSelected())
            {
                bgImage.color = Color.red;
            }
            else
            {
                bgImage.color = Color.white;
            }
            
            lastSelectedObject = currentSelected;
        }
    }
}
