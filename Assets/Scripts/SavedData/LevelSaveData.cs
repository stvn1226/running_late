using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelSaveData
{
    public int seed;
    public string name;
    public int pathMode;

    public LevelSaveData(int seed, string name, int pathMode = 0)
    {
        this.seed = seed;
        this.name = name;
        this.pathMode = pathMode;
    }
}

[Serializable]
public class LevelListData
{
    public List<LevelSaveData> levels = new List<LevelSaveData>();
}
