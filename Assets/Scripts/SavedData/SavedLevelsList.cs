using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class SavedLevelsList : MonoBehaviour
{
    public static SavedLevelsList instance;

    private const int MAX_SAVED_LEVELS = 5;

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

        savedLevels = new List<LevelSavingParams>();
    }

    public void AddSavedLevel(int seed)
    {
        LevelSavingParams levelEntry = new LevelSavingParams(seed, $"{seed}");
        savedLevels.RemoveAll(l => l.savedLevelSeed == seed);

        if (!savedLevels.Exists(l => l.savedLevelSeed == seed))
        {
            savedLevels.Insert(0, levelEntry);
        }

        if (savedLevels.Count > MAX_SAVED_LEVELS)
        {
            savedLevels.RemoveRange(MAX_SAVED_LEVELS, savedLevels.Count - MAX_SAVED_LEVELS);
        }
    }

    public void RemoveSavedLevel(int seed)
    {
        if (savedLevels.Exists(l => l.savedLevelSeed == seed))
        {
            //savedLevels.Remove(levelEntry);
            savedLevels.RemoveAll(l => l.savedLevelSeed == seed);
        }
    }

    public List<LevelSavingParams> GetSavedLevels()
    {
        return savedLevels;
    }

}
