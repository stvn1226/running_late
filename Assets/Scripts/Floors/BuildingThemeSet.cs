using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingTheme", menuName = "Runner/Building Theme Set")]
public class BuildingThemeSet : ScriptableObject
{
    [Header("Structure Prefabs")]
    public GameObject[] groundPrefabs;
    public GameObject[] floorPrefabs;
    public GameObject[] roofPrefabs;
    public GameObject[] roofObstaclePrefabs;
}
