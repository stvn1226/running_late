using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavedLevelEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelNameDisplay;
    [SerializeField] private TextMeshProUGUI levelSeedDisplay;
    [SerializeField] public string levelName;
    [SerializeField] public int levelSeed;
    [SerializeField] public Button startButton;
    [SerializeField] public Button renameButton;
    [SerializeField] public Button saveButton;
    [SerializeField] public Button deleteButton;

    private SavedLevelsPanel parent;

    public SavedLevelEntry(int seed, string name)
    {
        levelSeed = seed;
        levelName = name;
    }

    //if startButton is pressed, start the level with the given seed

    private void Start()
    {
        LoadLevelNameFromPlayerPrefs();
        
        parent = GetComponentInParent<SavedLevelsPanel>();

        startButton.onClick.AddListener(StartLevel);
        renameButton.onClick.AddListener(OpenRenamePanel);
        deleteButton.onClick.AddListener(DeleteLevel);
    }

    public void SetupEntry(int seed, string name, bool isRecents)
    {
        levelSeed = seed;
        levelName = name;
        if (levelNameDisplay != null)
        {
            levelNameDisplay.text = levelName;
        }
        if (levelSeedDisplay != null)
        {
            levelSeedDisplay.text = $"Seed: {levelSeed}";
        }
        
        saveButton.gameObject.SetActive(isRecents);
        renameButton.gameObject.SetActive(!isRecents);
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

    private void SaveLevel() //Used only in the Recent Levels menu
    {

    }

    private void DeleteLevel()
    {
        string key = $"SavedLevel_{levelSeed}_Name";
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            GameLogger.Log("SAVED LEVELS", $"Deleted saved level name for seed {levelSeed}");
        }

        if (saveButton.IsActive())
        {
            RecentLevelsList.instance.RemoveRecentLevel(levelSeed);
        }
        else
        {
            SavedLevelsList.instance.RemoveSavedLevel(levelSeed);
        }

        parent.PopulateMenu();
    }
}
