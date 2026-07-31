using System.Collections.Generic;
using UnityEngine;

public class SavedLevelsList : MonoBehaviour
{
    public static SavedLevelsList instance;

    //private const int MAX_SAVED_LEVELS = 5;

    private const string SAVE_KEY = "SavedLevelsData";

    [SerializeField] private List<LevelSavingParams> savedLevels;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else instance = this;
        DontDestroyOnLoad(gameObject);  // Add this line

        //savedLevels = new List<LevelSavingParams>();
        LoadFromPlayerPrefs();
    }

    public void AddSavedLevel(int seed, int pathMode)
    {
        savedLevels.RemoveAll(l => l.savedLevelSeed == seed);

        string levelName = $"{seed}";
        string key = $"SavedLevel_{seed}_Name";
        if (PlayerPrefs.HasKey(key))
        {
            levelName = PlayerPrefs.GetString(key);
        }

        LevelSavingParams levelEntry = LevelSavingParams.Create(seed, levelName, pathMode);

        if (!savedLevels.Exists(l => l.savedLevelSeed == seed))
        {
            savedLevels.Insert(0, levelEntry);
        }

        SaveToPlayerPrefs();
    }

    public int RemoveSavedLevel(int seed)
    {
        int removedLevelLocation = 0;
        if (savedLevels.Exists(l => l.savedLevelSeed == seed))
        {
            removedLevelLocation = savedLevels.FindIndex(l => l.savedLevelSeed == seed);
            savedLevels.RemoveAll(l => l.savedLevelSeed == seed);
            SaveToPlayerPrefs();
        }

        return removedLevelLocation;
    }

    public List<LevelSavingParams> GetSavedLevels()
    {
        return savedLevels;
    }

    private void SaveToPlayerPrefs()
    {
        LevelListData data = new LevelListData();
        foreach (var level in savedLevels)
        {
            data.levels.Add(new LevelSaveData(level.savedLevelSeed, level.savedLevelName, level.pathMode));
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        GameLogger.Log("SAVED LEVELS", $"Saved {savedLevels.Count} saved levels to PlayerPrefs");
    }

    private void LoadFromPlayerPrefs()
    {
        savedLevels = new List<LevelSavingParams>();

        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            LevelListData data = JsonUtility.FromJson<LevelListData>(json);

            if (data != null && data.levels != null)
            {
                foreach (var levelData in data.levels)
                {
                    LevelSavingParams level = LevelSavingParams.Create(levelData.seed, levelData.name, levelData.pathMode);
                    savedLevels.Add(level);
                }

                GameLogger.Log("SAVED LEVELS", $"Loaded {savedLevels.Count} saved levels from PlayerPrefs");
            }
        }
        else
        {
            GameLogger.Log("SAVED LEVELS", "No saved levels found, starting fresh");
        }
    }

}
