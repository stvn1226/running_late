using UnityEngine;
using UnityEngine.Tilemaps;

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
            if (ground.GetComponentInChildren<SpriteRenderer>() != null && ground.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(ground, floorsCt);
            else SortTilemap(ground, floorsCt);
        }
        else return;

        if (floorPrefabs.Length > 0)
        {
            Debug.Log(floorsCt);
            for (int i = 0; i < floorsCt - 2; i++)
            {
                Vector2 floorPosition = new Vector2(transform.position.x, transform.position.y + ((i + 1) * config.floorHeight));
                GameObject floor = Instantiate(floorPrefabs[Random.Range(0, floorPrefabs.Length)], floorPosition, Quaternion.identity, transform);
                if (floor.GetComponentInChildren<SpriteRenderer>() != null && floor.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(floor, floorsCt);
                else SortTilemap(floor, floorsCt);
            }
        }

        if (roofPrefab != null)
        {
            GameObject roof = Instantiate(roofPrefab, new Vector2(transform.position.x, transform.position.y + (((floorsCt > 0 ? floorsCt : 2) - 1) * config.floorHeight)), Quaternion.identity, transform);
            if (roof.GetComponentInChildren<SpriteRenderer>() != null && roof.GetComponentInChildren<SpriteRenderer>().enabled == true) SortSprite(roof, floorsCt);
            else SortTilemap(roof, floorsCt);
        }
        else return;
    }

    private void SortTilemap(GameObject toUse, int floorsCt)
    {
        toUse.GetComponentInChildren<TilemapRenderer>().sortingOrder -= floorsCt;

    }

    private void SortSprite(GameObject toUse, int floorsCt)
    {
        toUse.GetComponentInChildren<SpriteRenderer>().sortingOrder -= floorsCt;
    }
}
