using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class SavedLevelsList : MonoBehaviour
{
    public static SavedLevelsList instance;

    private List<SavedLevelEntry> savedLevels;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else instance = this;
        DontDestroyOnLoad(gameObject);  // Add this line

        savedLevels = new List<SavedLevelEntry>();
    }

    public void AddSavedLevel(SavedLevelEntry levelEntry)
    {
        if (!savedLevels.Contains(levelEntry))
        {
            savedLevels.Add(levelEntry);
        }
    }

    public void RemoveSavedLevel(SavedLevelEntry levelEntry)
    {
        if (savedLevels.Contains(levelEntry))
        {
            savedLevels.Remove(levelEntry);
        }
    }
}
