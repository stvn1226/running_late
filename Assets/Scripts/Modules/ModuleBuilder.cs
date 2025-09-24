using UnityEngine;

public class ModuleBuilder : MonoBehaviour
{
    [Header("Config")]
    public ModulePlan moduleConfig;

    [Header("Building Materials")]
    public GameObject foundation;
    public GameObject padding;

    public StructureBuilder[] buildingPrefabs;

    private Transform placementTracker;

    public Transform[] buildingSpawnPoints;

    private void Start()
    {
        placementTracker.position = transform.position;
    }

    public void GenerateModule()
    {
        int groundBlocksCt = Random.Range(moduleConfig.minTerrainHeight, moduleConfig.maxTerrainHeight + 1);
        for (int i = 0; i < groundBlocksCt - 1; i++)
        {
            GameObject paddingBlock = Instantiate(padding, placementTracker.position, Quaternion.identity, transform);
            placementTracker.position = new Vector2(placementTracker.position.x, placementTracker.position.y + moduleConfig.groundBlockHeight);
        }

        GameObject groundBlock = Instantiate(foundation, placementTracker.position, Quaternion.identity, transform);
        placementTracker.position = new Vector2(placementTracker.position.x, placementTracker.position.y + moduleConfig.groundBlockHeight);

        foreach (Transform spawnPoint in buildingSpawnPoints)
        {
            spawnPoint.position = new Vector2(spawnPoint.position.x, placementTracker.position.y);
            StructureBuilder buildingToPlace = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
            GameObject newBuilding = Instantiate(buildingToPlace.gameObject, spawnPoint.position, Quaternion.identity, transform);
            newBuilding.GetComponent<StructureBuilder>().ConstructStructure();
        }
    }
}
