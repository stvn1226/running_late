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
        int floorsCt = 0;
        floorsCt = Random.Range(config.minFloors, config.maxFloors + 1);

        if (groundPrefab != null)
        {
            GameObject ground = Instantiate(groundPrefab, transform.position, Quaternion.identity, transform);
            ground.GetComponentInChildren<SpriteRenderer>().sortingOrder -= floorsCt;
        }
        else return;

        if (floorPrefabs.Length > 0)
        {
            Debug.Log(floorsCt);
            for (int i = 0; i < floorsCt - 2; i++)
            {
                Vector2 floorPosition = new Vector2(transform.position.x, transform.position.y + ((i + 1) * config.floorHeight));
                GameObject floor = Instantiate(floorPrefabs[Random.Range(0, floorPrefabs.Length)], floorPosition, Quaternion.identity, transform);
                floor.GetComponentInChildren<SpriteRenderer>().sortingOrder -= floorsCt;
            }
        }

        if (roofPrefab != null)
        {
            GameObject roof = Instantiate(roofPrefab, new Vector2(transform.position.x, transform.position.y + (((floorsCt > 0 ? floorsCt : 2) - 1) * config.floorHeight)), Quaternion.identity, transform);
            roof.GetComponentInChildren<SpriteRenderer>().sortingOrder -= floorsCt;
        }
        else return;
    }
}
