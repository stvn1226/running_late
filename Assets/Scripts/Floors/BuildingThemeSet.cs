using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingTheme", menuName = "Runner/Building Theme Set")]
public class BuildingThemeSet : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Identificador único del tema (e.g. 'cine', 'depato', 'police', 'tea'). Se usa para evitar repetir el mismo tipo de edificio consecutivamente.")]
    public string themeId;

    [Header("Structure Prefabs")]
    public GameObject[] groundPrefabs;
    public GameObject[] floorPrefabs;
    public GameObject[] roofPrefabs;
    public GameObject[] roofObstaclePrefabs;
}
