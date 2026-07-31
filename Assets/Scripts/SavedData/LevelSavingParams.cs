using UnityEngine;

[CreateAssetMenu(fileName = "LevelSavingParams", menuName = "Scriptable Objects/LevelSavingParams")]
public class LevelSavingParams : ScriptableObject
{
    public int savedLevelSeed;
    public string savedLevelName;
    public int pathMode;

    public static LevelSavingParams Create(int seed, string name, int pathMode = 0)
    {
        var instance = ScriptableObject.CreateInstance<LevelSavingParams>();
        instance.savedLevelSeed = seed;
        instance.savedLevelName = name;
        instance.pathMode = pathMode;
        return instance;
    }

    //public int minModules;
    //public int maxModules;

    //public ModulePlan[] modulePlans; //contains module height params + number of buildings

    //public int buildingSpawnChance;

    //public FloorPlan[] floorPlans; //contains distance between buildings + number of floors

    //public int groundObstacleSpawnRate;
    //public int rooftopObstacleSpawnRate;
    //public int flyingObstacleSpawnRate;

    //public int powerUpSpawnRate;

    //public int cableSpawnRate;

    //public int cableMinDistance;
    //public int cableMaxDistance;
}
