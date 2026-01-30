using System.Collections.Generic;
using UnityEngine;

public class RecentLevelsList : MonoBehaviour
{
    public static RecentLevelsList instance { get; private set; }

    private const int MAX_RECENT_LEVELS = 20;
    private const string SAVE_KEY = "RecentLevelsData";

    [SerializeField] private List<LevelSavingParams> recentLevels;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else instance = this;
        DontDestroyOnLoad(gameObject);  // Add this line

        //recentLevels = new List<LevelSavingParams>();
        LoadFromPlayerPrefs();
    }

    public void AddRecentLevel(int seed)
    {
        LevelSavingParams levelEntry = new LevelSavingParams(seed, $"{seed}");
        recentLevels.RemoveAll(l => l.savedLevelSeed == seed);

        if (!recentLevels.Exists(l => l.savedLevelSeed == seed))
        {
            recentLevels.Insert(0, levelEntry);
        }

        if (recentLevels.Count > MAX_RECENT_LEVELS)
        {
            recentLevels.RemoveRange(MAX_RECENT_LEVELS, recentLevels.Count - MAX_RECENT_LEVELS);
        }

        SaveToPlayerPrefs();
    }

    public int RemoveRecentLevel(int seed)
    {
        int removedLevelLocation = 0;
        if (recentLevels.Exists(l => l.savedLevelSeed == seed))
        {
            removedLevelLocation = recentLevels.FindIndex(l => l.savedLevelSeed == seed);
            recentLevels.RemoveAll(l => l.savedLevelSeed == seed);
            SaveToPlayerPrefs();
        }

        return removedLevelLocation;
    }

    public List<LevelSavingParams> GetRecentLevels()
    {
        return recentLevels;
    }

    private void SaveToPlayerPrefs()
    {
        LevelListData data = new LevelListData();
        foreach (var level in recentLevels)
        {
            data.levels.Add(new LevelSaveData(level.savedLevelSeed, level.savedLevelName));
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        GameLogger.Log("RECENT LEVELS", $"Saved {recentLevels.Count} recent levels to PlayerPrefs");
    }

    private void LoadFromPlayerPrefs()
    {
        recentLevels = new List<LevelSavingParams>();

        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            LevelListData data = JsonUtility.FromJson<LevelListData>(json);

            if (data != null && data.levels != null)
            {
                foreach (var levelData in data.levels)
                {
                    LevelSavingParams level = new LevelSavingParams(levelData.seed, levelData.name);
                    recentLevels.Add(level);
                }

                GameLogger.Log("RECENT LEVELS", $"Loaded {recentLevels.Count} recent levels from PlayerPrefs");
            }
        }
        else
        {
            GameLogger.Log("RECENT LEVELS", "No saved recent levels found, starting fresh");
        }
    }

}
