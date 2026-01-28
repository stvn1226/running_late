using NUnit.Framework;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class RecentLevelsList : MonoBehaviour
{
    public static RecentLevelsList instance { get; private set; }

    private const int MAX_RECENT_LEVELS = 5;

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

        recentLevels = new List<LevelSavingParams>();
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

    }

    public void RemoveRecentLevel(int seed)
    {
        if (recentLevels.Exists(l => l.savedLevelSeed == seed))
        {
            //recentLevels.Remove(levelEntry);
            recentLevels.RemoveAll(l => l.savedLevelSeed == seed);
        }
    }

    public List<LevelSavingParams> GetRecentLevels()
    {
        return recentLevels;
    }
}
