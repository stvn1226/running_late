using UnityEngine;

public class StructureBuilder : MonoBehaviour
{
    //StructureBuilder will be used to create each building, while StructureGenerator will be used to lay out the buildings on the map
    [Header("Configuration")]
    public FloorPlan config;

    [Header("Component References")]
    public GameObject groundPrefab;
    public GameObject[] floorPrefabs;
    public GameObject roofPrefab;

    public void ConstructStructure()
    {
        Instantiate(groundPrefab, transform.position, Quaternion.identity, transform);

        int floors = Random.Range(config.minFloors - 2, config.maxFloors - 2);
        for (int i = 0; i < floors; i++)
        {
            Vector2 floorPosition = new Vector2(transform.position.x, transform.position.y + (i * config.floorHeight));
            Instantiate(floorPrefabs[Random.Range(0, floorPrefabs.Length)], floorPosition, Quaternion.identity, transform);
        }

        Instantiate(roofPrefab, new Vector2(transform.position.x, transform.position.y + (floors * config.floorHeight)), Quaternion.identity, transform);
    }
}
