using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingTheme", menuName = "Runner/Building Theme Set")]
public class BuildingThemeSet : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique theme identifier (e.g. 'Set1', 'Set3', 'police', 'tea'). Used to prevent repeating the same building theme consecutively.")]
    public string themeId;

    [Tooltip("Minimum number of floors required for this specific theme. If > 0, overrides the building's floor count.")]
    public int minFloorsOverride = 0;

    [Header("Structure Prefabs")]
    public GameObject[] groundPrefabs;
    public GameObject[] floorPrefabs;
    public GameObject[] roofPrefabs;
    public GameObject[] roofObstaclePrefabs;
}
