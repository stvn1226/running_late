using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelSaveData
{
    public int seed;
    public string name;

    public LevelSaveData(int seed, string name)
    {
        this.seed = seed;
        this.name = name;
    }
}

[Serializable]
public class LevelListData
{
    public List<LevelSaveData> levels = new List<LevelSaveData>();
}
