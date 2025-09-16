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
        if (groundPrefab != null) Instantiate(groundPrefab, transform.position, Quaternion.identity, transform);
        else return;

        int floors = 0;
        if (floorPrefabs.Length > 0)
        {
            floors = Random.Range(config.minFloors, config.maxFloors + 1);
            Debug.Log(floors);
            for (int i = 0; i < floors - 2; i++)
            {
                Vector2 floorPosition = new Vector2(transform.position.x, transform.position.y + ((i + 1) * config.floorHeight));
                Instantiate(floorPrefabs[Random.Range(0, floorPrefabs.Length)], floorPosition, Quaternion.identity, transform);
            }
        }

        if (roofPrefab != null) Instantiate(roofPrefab, new Vector2(transform.position.x, transform.position.y + (((floors > 0? floors : 2) - 1) * config.floorHeight)), Quaternion.identity, transform);
        else return;
    }
}
